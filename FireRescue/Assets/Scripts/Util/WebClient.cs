using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; 

public class WebClient : MonoBehaviour
{
    [SerializeField] private GameObject floorTile;
    [SerializeField] private AddWallsManager addWallsManager;
    [SerializeField] private AddFiresAndPOI addFiresAndPOIManager;
    [SerializeField] private AddAgents addAgentsManager;

    private bool isInitialData = true;
    private bool requestNewData = true;
    private bool isSendingData = false; 

    // Start is called before the first frame update
    void Start()
    {
        string json = "{}"; 
        StartCoroutine(SendData(json));
    }

    // Update is called once per frame
    void Update()
    {
        if (requestNewData == true && isSendingData == false) {
            string json = "{}";
            StartCoroutine(SendData(json));
        }
    }

    IEnumerator SendData(string data)
    {
        isSendingData = true;

        WWWForm form = new WWWForm();
        form.AddField("bundle", "the data");
        string url = "http://localhost:8585";
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest(); // Talk to Python server

            if(www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"Network error: {www.error}");
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;

                if (isInitialData) {
                    InitialGameData gameData = null;
                    try {
                        // Deserialize JSON and create grid
                        gameData = JsonConvert.DeserializeObject<InitialGameData>(jsonResponse);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"JSON Deserialization Error: {ex.Message}");
                        requestNewData = false;
                    }

                    if (gameData != null)
                    {
                        GameObject gridContainer = new GameObject("GameGrid");
                        Transform gridTransform = gridContainer.transform;

                        CreateGrid.CreateGridTiles(floorTile, gridTransform, gameData.width, gameData.height);

                        addWallsManager.AddWallsToCells(gameData.walls, gridTransform, gameData.doors, gameData.entry_points);

                        addFiresAndPOIManager.AddFiresToCells(gameData.fires, gridTransform);

                        addFiresAndPOIManager.AddPOIToCells(gameData.points_of_interest, gridTransform);

                        yield return StartCoroutine(addAgentsManager.AddAgentsToCells(gameData.agent_positions, gridTransform));

                        isInitialData = false;
                    }
                } else {
                    NewGameData gameData = null;
                    try {
                        gameData = JsonConvert.DeserializeObject<NewGameData>(jsonResponse);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"JSON Deserialization Error: {ex.Message}");
                        requestNewData = false;
                    }

                    if (gameData != null)
                    {
                        GameObject gridContainer = GameObject.Find("GameGrid")?.gameObject;
                        Transform gridTransform = gridContainer.transform;

                        // Process actions in the order they occurred
                        yield return StartCoroutine(ProcessActions(gameData, gridTransform));

                        requestNewData = !gameData.simulation_finished;
                    }
                }
            }
        }
        isSendingData = false;
    }

    private IEnumerator ProcessActions(NewGameData gameData, Transform gridTransform)
    {
        foreach (ActionData action in gameData.actions)
        {
            switch (action.action)
            {
                case "move":
                    yield return StartCoroutine(addAgentsManager.MoveAgent(action.agent_id, action.from, action.to, gridTransform));
                    break;
                case "extinguish_fire":
                    NewStatusDouble fireToExtinguish = new NewStatusDouble
                    {
                        position = action.position,
                        new_value = 0 // 0 represents extinguished fire
                    };
                    List<NewStatusDouble> firesToExtinguish = new List<NewStatusDouble> { fireToExtinguish };
                    yield return StartCoroutine(addFiresAndPOIManager.extinguishFires(firesToExtinguish, gridTransform));
                    break;
                case "extinguish_smoke":
                    NewStatusDouble smokeToExtinguish = new NewStatusDouble
                    {
                        position = action.position,
                        new_value = 0 // 0 represents extinguished fire
                    };
                    List<NewStatusDouble> smokesToExtinguish = new List<NewStatusDouble> { smokeToExtinguish };
                    yield return StartCoroutine(addFiresAndPOIManager.extinguishFires(smokesToExtinguish, gridTransform));
                    break;
                case "open_door":
                    yield return StartCoroutine(addWallsManager.OpenDoor(action.positions, gridTransform));
                    break;
                case "drop_victim":
                    yield return StartCoroutine(addAgentsManager.DropVictim(action.agent_id, action.position, gridTransform));
                    break;
                case "pick_up_victim":
                    yield return StartCoroutine(addFiresAndPOIManager.RevealVictimAtPosition(action.position, gameData.points_of_interest, gridTransform));
                    yield return StartCoroutine(addAgentsManager.PickUpVictim(action.agent_id, action.position, gridTransform));
                    break;
                case "reveal_poi_false_alarm":
                    yield return StartCoroutine(addFiresAndPOIManager.RevealFalseAlarmAtPosition(action.position, gameData.points_of_interest, gridTransform));
                    break;
                // Add other cases as needed
                default:
                    Debug.LogWarning($"Unknown action: {action.action}");
                    break;
            }
            // Optionally add a short delay between actions
            // yield return new WaitForSeconds(0.1f);
        }

        // After processing actions, handle other updates if necessary
        yield return StartCoroutine(addFiresAndPOIManager.AddNewFiresAndSmokes(
            gameData.fires, 
            gameData.width, 
            gameData.height, 
            gridTransform));

        yield return StartCoroutine(addFiresAndPOIManager.Explosion(
            gameData.explosions,
            gameData.fires,
            gameData.walls,
            gameData.damage,
            gameData.doors,
            gameData.width,
            gameData.height,
            gridTransform
        ));

        yield return StartCoroutine(addFiresAndPOIManager.getRidOfPOI(
            gameData.points_of_interest,
            gridTransform
        ));
        
        yield return StartCoroutine(addFiresAndPOIManager.placeNewPOI(
            gameData.points_of_interest,
            gridTransform
        ));

        // Include any other updates using gameData as needed
        yield return null;
    }
}