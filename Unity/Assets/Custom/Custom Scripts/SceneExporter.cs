using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Handles exporting scene data to JSON file or server
public class SceneExporter : MonoBehaviour
{
    public string savePath = "/Users/guangqianma/Desktop/M/SceneConfig.json";
    private SceneData loadedSceneData; // Original JSON data

    public SceneLoader sceneLoader;

    /*
    public string apiEndpoint = "Backend API Endpoint";
    */

    void Start()
    {
        LoadSceneData();
    }

    // Load scene data from SceneLoader
    private void LoadSceneData()
    {
        if (sceneLoader != null)
        {
            loadedSceneData = sceneLoader.GetLoadedSceneData();
        }

        if (loadedSceneData == null)
        {
            Debug.LogError("No scene data loaded from SceneLoader.");
        }
    }

    // Save current object transforms to JSON
    public void SaveScene()
    {
        if (loadedSceneData == null)
        {
            Debug.LogError("No scene data available to save.");
            return;
        }

        // Find all objects with metadata
        SceneObjectMetadata[] sceneObjects = FindObjectsByType<SceneObjectMetadata>(
            FindObjectsInactive.Exclude, 
            FindObjectsSortMode.None
        );

        // Update transform data in JSON
        foreach (var obj in loadedSceneData.objects)
        {
            foreach (SceneObjectMetadata meta in sceneObjects)
            {
                if (meta.objectId == obj.id)
                {
                    obj.position = meta.transform.position;
                    obj.rotation = meta.transform.eulerAngles;
                    obj.scale = meta.transform.localScale;
                    break;
                }
            }
        }

        // Save to file
        string updatedJson = JsonUtility.ToJson(loadedSceneData, true);
        try
        {
            File.WriteAllText(savePath, updatedJson);
            Debug.Log($"Scene saved to: {savePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to save scene: {e.Message}");
        }

        /* 
        // BACKEND INTEGRATION (NOT YET IMPLEMENTED)
        // Save to server
        _ = SaveSceneToServerAsync(loadedSceneData);
        */
    }

    void Update()
    {
        // Press P to save scene
        if (Input.GetKeyDown(KeyCode.P))
        {
            SaveScene();
        }
    }

    /* 
    // BACKEND INTEGRATION (NOT YET IMPLEMENTED)
    
    // Upload JSON data to server
    private async Task<bool> UploadJsonToServerAsync(string sceneId, string jsonData)
    {
        try
        {
            string url = $"Backend API Endpoint";
            
            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Scene {sceneId} uploaded to server successfully");
                    return true;
                }
                else
                {
                    Debug.LogError($"Server error: {request.error}");
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error uploading scene to server: {e.Message}");
            return false;
        }
    }
    */
}