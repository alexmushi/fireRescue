using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; // Install Newtonsoft.Json

public class WebClient : MonoBehaviour
{
    [SerializeField] private GameObject floorTile;
    [SerializeField] private AddWallsManager addWallsManager;
    [SerializeField] private AddFiresAndPOI addFiresAndPOIManager;
    [SerializeField] private AddAgents addAgentsManager;

    private bool isInitialData = true;
    private bool requestNewData = true;
    private bool isSendingData = false; 

    // IEnumerator - yield return
    IEnumerator SendData(string data)
    {
        isSendingData = true;

        WWWForm form = new WWWForm();
        form.AddField("bundle", "the data");
        string url = "http://localhost:8585";
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();          // Talk to Python
            if(www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
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

                    GameObject gridContainer = new GameObject("GameGrid");
                    Transform gridTransform = gridContainer.transform;

                    CreateGrid.CreateGridTiles(floorTile, gridTransform, gameData.width, gameData.height);

                    addWallsManager.AddWallsToCells(gameData.walls, gridTransform, gameData.doors, gameData.entry_points);

                    addFiresAndPOIManager.AddFiresToCells(gameData.fires, gridTransform);

                    addFiresAndPOIManager.AddPOIToCells(gameData.points_of_interest, gridTransform);

                    yield return StartCoroutine(addAgentsManager.AddAgentsToCells(gameData.agent_positions, gridTransform));

                    isInitialData = false;

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

                    GameObject gridContainer = GameObject.Find("GameGrid")?.gameObject;
                    Transform gridTransform = gridContainer.transform;

                    yield return StartCoroutine(addFiresAndPOIManager.extinguishFires(gameData.fires, gridTransform));

                    yield return StartCoroutine(addFiresAndPOIManager.Explosion(
                        gameData.explosions, 
                        gameData.fires, 
                        gameData.walls, 
                        gameData.damage, 
                        gameData.doors, 
                        gameData.width, 
                        gameData.height, 
                        gridTransform));

                    yield return StartCoroutine(addFiresAndPOIManager.AddNewFiresAndSmokes(gameData.fires, gameData.width, gameData.height, gridTransform));

                    yield return StartCoroutine(addAgentsManager.UpdateAgentsPositions(gameData.agent_positions, gridTransform));
                    
                    requestNewData = !gameData.simulation_finished;
                }
            }
        }
        isSendingData = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        string json = "{}"; 
        StartCoroutine(SendData(json));
    }

    // Update is called once per frame
    void Update()
    {
        string json = "{}";
        if (requestNewData == true && isSendingData == false) {
            StartCoroutine(SendData(json));
        }
    }
}