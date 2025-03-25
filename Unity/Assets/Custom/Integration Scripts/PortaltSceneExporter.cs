using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class PortaltSceneExporter : MonoBehaviour
{
    [Header("Server Configuration")]
    public PortaltServerConfig serverConfig;
    
    [Header("Scene Reference")]
    public PortaltSceneLoader sceneLoader;
    
    /// <summary>
    /// Save the current scene back to the Portalt API
    /// </summary>
    public async Task<bool> SaveSceneToApi()
    {
        try
        {
            // Get the current scene configuration
            SceneConfiguration config = sceneLoader.CurrentSceneConfig;
            if (config == null)
            {
                Debug.LogError("No scene configuration available to save");
                return false;
            }
            
            // Update configuration with current transforms
            UpdateSceneObjectTransforms(ref config);
            
            // Save to server
            bool success = await UploadSceneConfigurationAsync(config);
            
            if (success)
            {
                Debug.Log($"Scene {config.scene_id} saved successfully");
            }
            else
            {
                Debug.LogError("Failed to save scene to server");
            }
            
            return success;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving scene: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Update a scene configuration with the current transforms of objects in the scene
    /// </summary>
    public void UpdateSceneObjectTransforms(ref SceneConfiguration config)
    {
        if (config == null || config.objects == null)
        {
            Debug.LogError("Cannot update null scene configuration");
            return;
        }
        
        // Find all objects with metadata in the scene
        SceneObjectMetadata[] sceneObjects = FindObjectsByType<SceneObjectMetadata>(
            FindObjectsInactive.Exclude, 
            FindObjectsSortMode.None
        );
        
        Debug.Log($"Found {sceneObjects.Length} objects with metadata in scene");
        
        // Update each object in the configuration
        foreach (var configObj in config.objects)
        {
            foreach (SceneObjectMetadata meta in sceneObjects)
            {
                if (meta.objectId == configObj.object_id)
                {
                    // Update transform data
                    configObj.position = Vector3Serializable.FromVector3(meta.transform.position);
                    configObj.rotation = Vector3Serializable.FromVector3(meta.transform.eulerAngles);
                    configObj.scale = Vector3Serializable.FromVector3(meta.transform.localScale);
                    
                    Debug.Log($"Updated object {configObj.object_id} transform data");
                    break;
                }
            }
        }
        
        // Update the timestamp
        config.updatedAt = DateTime.UtcNow.ToString("o");
    }
    
    /// <summary>
    /// Creates a new scene configuration from scratch based on objects in the scene
    /// </summary>
    public SceneConfiguration CreateSceneConfiguration(string sceneId)
    {
        // Create new configuration
        SceneConfiguration config = new SceneConfiguration
        {
            scene_id = sceneId,
            objects = new List<SceneObject>(),
            createdAt = DateTime.UtcNow.ToString("o"),
            updatedAt = DateTime.UtcNow.ToString("o")
        };
        
        // Find all objects with metadata
        SceneObjectMetadata[] sceneObjects = FindObjectsByType<SceneObjectMetadata>(
            FindObjectsInactive.Exclude, 
            FindObjectsSortMode.None
        );
        
        // Add each object to the configuration
        foreach (SceneObjectMetadata meta in sceneObjects)
        {
            SceneObject obj = new SceneObject
            {
                object_id = meta.objectId,
                modelUrl = meta.modelUrl,
                position = Vector3Serializable.FromVector3(meta.transform.position),
                rotation = Vector3Serializable.FromVector3(meta.transform.eulerAngles),
                scale = Vector3Serializable.FromVector3(meta.transform.localScale)
            };
            
            config.objects.Add(obj);
        }
        
        return config;
    }
    
    /// <summary>
    /// Upload a scene configuration to the server
    /// </summary>
    private async Task<bool> UploadSceneConfigurationAsync(SceneConfiguration config)
    {
        try
        {
            string url = serverConfig.GetSceneConfigUrl(config.scene_id);
            Debug.Log($"Uploading scene configuration to: {url}");
            
            // Serialize to JSON
            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            
            // Create web request
            using (UnityWebRequest request = UnityWebRequest.Put(url, jsonData))
            {
                // Set up request headers
                request.SetRequestHeader("Content-Type", "application/json");
                
                // Send the request
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Scene {config.scene_id} uploaded successfully");
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
    
    /// <summary>
    /// Unity Update method to handle keyboard shortcuts
    /// </summary>
    private void Update()
    {
        // Press S to save scene (for convenience)
        if (Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            // Use the non-async public method to handle the async operation
            _ = SaveSceneToApi();
        }
    }
} 