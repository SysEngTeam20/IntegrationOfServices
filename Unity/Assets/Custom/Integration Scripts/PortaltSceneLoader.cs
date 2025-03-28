using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Siccity.GLTFUtility;

public class PortaltSceneLoader : MonoBehaviour
{
    [Header("Server Configuration")]
    public ScriptableObject serverConfig; // Accepts both PortaltServerConfig and PortaltServerConfigV
    
    [Header("Current Scene")]
    public string currentSceneId;
    public string currentActivityId;
    
    // References to currently loaded objects
    private List<GameObject> loadedObjects = new List<GameObject>();
    private SceneConfiguration currentSceneConfig;
    
    // Public property to access current configuration
    public SceneConfiguration CurrentSceneConfig => currentSceneConfig;
    
    // Show loading screen with appropriate scene message
    private void ShowLoadingScreen(string sceneId)
    {
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreen($"Loading scene...");
        }
    }
    
    // Hide loading screen
    private void HideLoadingScreen()
    {
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingScreen();
        }
    }
    
    /// <summary>
    /// Load a scene by ID directly from the Portalt API
    /// </summary>
    public async Task<bool> LoadSceneFromApi(string sceneId)
    {
        if (string.IsNullOrEmpty(sceneId))
        {
            Debug.LogError("Scene ID cannot be empty");
            return false;
        }
        
        try
        {
            // Show loading screen
            ShowLoadingScreen(sceneId);
            
            // Clear any existing scene
            ClearScene();
            
            // Load configuration from API - check if we're using a join URL
            string url;
            if (sceneId == "dummy" && serverConfig != null && serverConfig is PortaltServerConfigV viewerConfig)
            {
                // We're using the join URL approach
                url = viewerConfig.GetActivityJoinUrl();
                Debug.Log($"Using join URL approach: {url}");
            }
            else if (serverConfig is PortaltServerConfig standardConfig)
            {
                // Normal scene URL with scene ID
                url = standardConfig.GetSceneConfigUrl(sceneId);
                Debug.Log($"Loading scene configuration from normal URL: {url}");
            }
            else
            {
                Debug.LogError("No compatible server configuration found");
                HideLoadingScreen();
                return false;
            }
            
            string json = await DownloadJsonFromServerAsync(url);
            try {
                currentSceneConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<SceneConfiguration>(json);
                currentSceneId = sceneId;
                
                if (currentSceneConfig == null || currentSceneConfig.objects == null || currentSceneConfig.objects.Count == 0)
                {
                    Debug.Log("Scene configuration contains no objects");
                    
                    // Hide loading screen
                    HideLoadingScreen();
                    
                    return true;
                }
                
                Debug.Log($"Loaded scene configuration with {currentSceneConfig.objects.Count} objects");
                
                // Load all objects in parallel
                List<Task> loadingTasks = new List<Task>();
                foreach (SceneObject obj in currentSceneConfig.objects)
                {
                    loadingTasks.Add(LoadModelAsync(obj));
                }
                
                // Wait for all tasks to complete
                await Task.WhenAll(loadingTasks);
                
                Debug.Log("Scene loading complete");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing scene configuration: {e.Message}");
                throw;
            }
            
            // Hide loading screen
            HideLoadingScreen();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
            
            // Hide loading screen
            HideLoadingScreen();
            
            return false;
        }
    }
    
    /// <summary>
    /// Load a specific activity and its first scene
    /// </summary>
    public async Task<bool> LoadActivityFromApi(string activityId)
    {
        if (string.IsNullOrEmpty(activityId))
        {
            Debug.LogError("Activity ID cannot be empty");
            return false;
        }
        
        try
        {
            // Show loading screen
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowLoadingScreen("Loading activity...");
            }
            
            string url;
            if (serverConfig is PortaltServerConfig standardConfig)
            {
                url = standardConfig.GetActivityUrl(activityId);
                Debug.Log($"Loading activity from: {url}");
            }
            else
            {
                Debug.LogError("Cannot load activity: No compatible server configuration found");
                HideLoadingScreen();
                return false;
            }
            
            string json = await DownloadJsonFromServerAsync(url);
            ActivityData activity = Newtonsoft.Json.JsonConvert.DeserializeObject<ActivityData>(json);
            currentActivityId = activityId;
            
            if (activity == null || activity.scenes == null || activity.scenes.Count == 0)
            {
                Debug.LogError("Activity contains no scenes");
                
                // Hide loading screen
                HideLoadingScreen();
                
                return false;
            }
            
            Debug.Log($"Loaded activity {activity.title} with {activity.scenes.Count} scenes");
            
            // Load the first scene
            return await LoadSceneFromApi(activity.scenes[0].id);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load activity: {e.Message}");
            
            // Hide loading screen
            HideLoadingScreen();
            
            return false;
        }
    }
    
    /// <summary>
    /// Clear all currently loaded objects
    /// </summary>
    public void ClearScene()
    {
        foreach (GameObject obj in loadedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        loadedObjects.Clear();
        Debug.Log("Scene cleared");
    }
    
    /// <summary>
    /// Load a scene by activity ID and scene ID
    /// </summary>
    public async Task<bool> LoadScene(string activityId, string sceneId)
    {
        if (string.IsNullOrEmpty(sceneId) || string.IsNullOrEmpty(activityId))
        {
            Debug.LogError("Activity ID and Scene ID cannot be empty");
            return false;
        }
        
        try
        {
            // Set the current activity ID
            currentActivityId = activityId;
            
            // Load the scene by ID
            return await LoadSceneFromApi(sceneId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
            
            // Hide loading screen
            HideLoadingScreen();
            
            return false;
        }
    }
    
    /// <summary>
    /// Load a 3D model from a SceneObject
    /// </summary>
    private async Task LoadModelAsync(SceneObject objData)
    {
        try
        {
            Debug.Log($"Loading model: {objData.object_id} from {objData.modelUrl}");
            
            // Download model data
            byte[] glbData = await DownloadBinaryFromServerAsync(objData.modelUrl);
            
            // Import the model
            GameObject model = Importer.LoadFromBytes(glbData);
            if (model == null)
            {
                Debug.LogError($"Failed to parse model: {objData.modelUrl}");
                return;
            }
            
            // Set up the model
            SetupModelGameObject(model, objData);
            
            // Add to loaded objects
            loadedObjects.Add(model);
            
            Debug.Log($"Loaded model: {objData.object_id}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading model {objData.object_id}: {e.Message}");
        }
    }
    
    /// <summary>
    /// Configure a model GameObject with the correct components and transform
    /// </summary>
    private void SetupModelGameObject(GameObject model, SceneObject objData)
    {
        // Apply transform data
        model.transform.position = objData.position.ToVector3();
        model.transform.eulerAngles = objData.rotation.ToVector3();
        model.transform.localScale = objData.scale.ToVector3();
        model.name = objData.object_id;
        
        // Make sure the object is on the correct layer for selection
        model.layer = LayerMask.NameToLayer("Selectable") != -1 ? 
            LayerMask.NameToLayer("Selectable") : 0;
            
        // Ensure all child objects with renderers are also set to the correct layer
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.gameObject.layer = model.layer;
        }
        
        // Add metadata component
        SceneObjectMetadata metadata = model.AddComponent<SceneObjectMetadata>();
        metadata.Initialize(objData.object_id, objData.modelUrl);
        
        // Add colliders if needed
        if (model.GetComponent<Collider>() == null)
        {
            // Try to find a mesh in the hierarchy for the collider
            Mesh colliderMesh = null;
            
            // First check MeshFilters
            MeshFilter firstMeshFilter = model.GetComponentInChildren<MeshFilter>();
            if (firstMeshFilter != null && firstMeshFilter.sharedMesh != null)
            {
                colliderMesh = firstMeshFilter.sharedMesh;
            }
            // If no MeshFilter found, try SkinnedMeshRenderers
            else
            {
                SkinnedMeshRenderer firstSkinnedMesh = model.GetComponentInChildren<SkinnedMeshRenderer>();
                if (firstSkinnedMesh != null && firstSkinnedMesh.sharedMesh != null)
                {
                    colliderMesh = firstSkinnedMesh.sharedMesh;
                }
            }
            
            if (colliderMesh != null)
            {
                MeshCollider collider = model.AddComponent<MeshCollider>();
                collider.sharedMesh = colliderMesh;
                collider.convex = true;
                Debug.Log($"Added MeshCollider to {objData.object_id}");
            }
            else
            {
                // Fallback to a default collider
                BoxCollider boxCollider = model.AddComponent<BoxCollider>();
                Debug.Log($"Added BoxCollider to {objData.object_id} as fallback");
            }
        }
    }
    
    /// <summary>
    /// Download JSON data from server
    /// </summary>
    private async Task<string> DownloadJsonFromServerAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.text;
            }
            else
            {
                throw new Exception(request.error);
            }
        }
    }
    
    /// <summary>
    /// Download binary file (for models)
    /// </summary>
    private async Task<byte[]> DownloadBinaryFromServerAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
                return request.downloadHandler.data;
            else
                throw new Exception(request.error);
        }
    }
} 