using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class PortaltAPIClient : MonoBehaviour
{
    public PortaltServerConfig serverConfig;

    // Async method to get activities list
    public async Task<ActivityListResponse> GetActivities()
    {
        string url = serverConfig.GetActivitiesListUrl();
        Debug.Log($"Fetching activities from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
                await Task.Delay(10);
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully fetched activities");
                try {
                    ActivityListResponse response = JsonConvert.DeserializeObject<ActivityListResponse>(request.downloadHandler.text);
                    return response;
                }
                catch (System.Exception e) {
                    Debug.LogError($"Failed to parse activities: {e.Message}");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch activities: {request.error} - {request.downloadHandler.text}");
                return null;
            }
        }
    }
    
    // Async method to get scenes for an activity
    public async Task<SceneListResponse> GetScenes(string activityId)
    {
        string url = serverConfig.GetScenesForActivityUrl(activityId);
        Debug.Log($"Fetching scenes for activity {activityId} from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
                await Task.Delay(10);
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try {
                    SceneListResponse response = JsonConvert.DeserializeObject<SceneListResponse>(request.downloadHandler.text);
                    return response;
                }
                catch (System.Exception e) {
                    Debug.LogError($"Failed to parse scenes: {e.Message}");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch scenes: {request.error} - {request.downloadHandler.text}");
                return null;
            }
        }
    }

    // Get list of all activities
    public IEnumerator GetActivities(System.Action<List<ActivityData>> callback)
    {
        string url = serverConfig.GetActivitiesListUrl();
        Debug.Log($"Fetching activities from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully fetched activities");
                Debug.Log(request.downloadHandler.text);
                try {
                    List<ActivityData> activities = JsonConvert.DeserializeObject<List<ActivityData>>(request.downloadHandler.text);
                    callback(activities);
                }
                catch (System.Exception e) {
                    Debug.LogError($"Failed to parse activities: {e.Message}");
                    callback(null);
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch activities: {request.error} - {request.downloadHandler.text}");
                callback(null);
            }
        }
    }
    
    // Get a specific activity by ID
    public IEnumerator GetActivity(string activityId, System.Action<ActivityData> callback)
    {
        string url = serverConfig.GetActivityUrl(activityId);
        Debug.Log($"Fetching activity from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try {
                    ActivityData activity = JsonConvert.DeserializeObject<ActivityData>(request.downloadHandler.text);
                    callback(activity);
                }
                catch (System.Exception e) {
                    Debug.LogError($"Failed to parse activity: {e.Message}");
                    callback(null);
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch activity: {request.error} - {request.downloadHandler.text}");
                callback(null);
            }
        }
    }
    
    // Get scene configuration
    public IEnumerator GetSceneConfiguration(string sceneId, System.Action<SceneConfiguration> callback)
    {
        string url = serverConfig.GetSceneConfigUrl(sceneId);
        Debug.Log($"Fetching scene config from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try {
                    SceneConfiguration config = JsonConvert.DeserializeObject<SceneConfiguration>(request.downloadHandler.text);
                    callback(config);
                }
                catch (System.Exception e) {
                    Debug.LogError($"Failed to parse scene config: {e.Message}");
                    callback(null);
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch scene config: {request.error} - {request.downloadHandler.text}");
                callback(null);
            }
        }
    }
    
    // Save scene configuration
    public IEnumerator SaveSceneConfiguration(SceneConfiguration config, System.Action<bool> callback)
    {
        string url = serverConfig.GetSceneConfigUrl(config.scene_id);
        Debug.Log($"Saving scene config to: {url}");
        
        string jsonData = JsonConvert.SerializeObject(config);
        
        using (UnityWebRequest request = UnityWebRequest.Put(url, jsonData))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Scene saved successfully");
                callback(true);
            }
            else
            {
                Debug.LogError($"Failed to save scene: {request.error} - {request.downloadHandler.text}");
                callback(false);
            }
        }
    }
    
    // Get assets list
    public IEnumerator GetAssets(System.Action<List<AssetData>> callback)
    {
        string url = serverConfig.GetAssetsListUrl();
        Debug.Log($"Fetching assets from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try {
                    List<AssetData> assets = JsonConvert.DeserializeObject<List<AssetData>>(request.downloadHandler.text);
                    callback(assets);
                }
                catch (System.Exception e) {
                    Debug.LogError($"Failed to parse assets: {e.Message}");
                    callback(null);
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch assets: {request.error} - {request.downloadHandler.text}");
                callback(null);
            }
        }
    }
} 