using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; // Install Newtonsoft.Json

public class WebClient : MonoBehaviour
{
    public GameObject floorTile;
    public GameObject wallPrefab;
    public GameObject doorPrefab;

    // IEnumerator - yield return
    IEnumerator SendDataStart(string data)
    {
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

                try
                {
                    // Deserialize JSON and create grid
                    GameData gameData = JsonConvert.DeserializeObject<GameData>(jsonResponse);

                    GameObject gridContainer = new GameObject("GameGrid");
                    Transform gridTransform = gridContainer.transform;

                    CreateGrid.CreateGridTiles(floorTile, gridTransform, gameData.width, gameData.height);

                    AddWalls.AddWallsToCells(gameData.walls, wallPrefab, doorPrefab, gridContainer.transform, gameData.doors);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"JSON Deserialization Error: {ex.Message}");
                }
            }
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        string json = "{}"; 
        StartCoroutine(SendDataStart(json));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}