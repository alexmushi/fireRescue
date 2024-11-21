using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; // Install Newtonsoft.Json

public class WebClient : MonoBehaviour
{
    // IEnumerator - yield return
    IEnumerator SendData(string data)
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
                Debug.Log("Response: " + jsonResponse);

                try
                {
                    GameData gameData = JsonConvert.DeserializeObject<GameData>(jsonResponse);
                    Debug.Log("Damage Points: " + gameData.damage_points);
                    Debug.Log("People Lost: " + gameData.people_lost);
                    Debug.Log("People Rescued: " + gameData.people_rescued);
                    foreach (List<double> row in gameData.walls)
                    {
                        string rowString = string.Join(", ", row);
                        
                        Debug.Log(rowString);
                    }
                    foreach (List<double> row in gameData.fires)
                    {
                        string rowString = string.Join(", ", row);
                        
                        Debug.Log(rowString);
                    }
                    foreach (List<string> row in gameData.points_of_interest)
                    {
                        string rowString = string.Join(", ", row);
                        
                        Debug.Log(rowString);
                    }
                    foreach (Door door in gameData.doors)
                    {
                        string coord1 = $"{door.coord1[0]}, {door.coord1[1]}";
                        string coord2 = $"{door.coord2[0]}, {door.coord2[1]}";
                        Debug.Log($"Door between ({coord1}) and ({coord2}): {door.status}");
                    }
                    foreach (int[] entry in gameData.entry_points)
                    {
                        Debug.Log($"Entry Point: {entry[0]}, {entry[1]}");
                    }
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
        StartCoroutine(SendData(json));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}