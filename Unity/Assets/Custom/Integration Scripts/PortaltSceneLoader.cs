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
    
    [Header("Selection Visualization")]
    [Tooltip("Material used to visualize colliders for selection. Create a custom material with Standard or URP/Lit shader and set Rendering Mode to Transparent.")]
    public Material colliderVisualizationMaterial;
    [Tooltip("Should collider bounds be shown to help user selection")]
    public bool showColliderBounds = false;
    [Range(0.0f, 1.0f)]
    [Tooltip("Transparency of the collider visualization")]
    public float colliderVisualizationOpacity = 0.3f;
    [Tooltip("Color for highlighting colliders")]
    public Color colliderHighlightColor = new Color(0.2f, 0.6f, 1.0f, 0.3f);
    [Tooltip("Key to toggle collider visualization")]
    public KeyCode toggleVisualizationKey = KeyCode.V;
    
    // References to currently loaded objects
    private List<GameObject> loadedObjects = new List<GameObject>();
    private SceneConfiguration currentSceneConfig;
    private List<GameObject> colliderVisualizers = new List<GameObject>();
    private Material instancedColliderMaterial;
    
    // Public property to access current configuration
    public SceneConfiguration CurrentSceneConfig => currentSceneConfig;
    
    private void Awake()
    {
        // Check if a visualization material was provided
        if (colliderVisualizationMaterial == null)
        {
            Debug.LogWarning("No collider visualization material assigned. Colliders will not be visualized correctly.");
            
            // Create a simple material as fallback, but it might not work in builds
            colliderVisualizationMaterial = new Material(Shader.Find("Unlit/Color"));
            colliderVisualizationMaterial.color = colliderHighlightColor;
        }
        
        // Create an instanced copy so we can modify it without affecting the original
        instancedColliderMaterial = new Material(colliderVisualizationMaterial);
        instancedColliderMaterial.color = colliderHighlightColor;
    }
    
    private void OnValidate()
    {
        // Update the instance material in the editor when properties change
        if (instancedColliderMaterial != null)
        {
            Color newColor = colliderHighlightColor;
            newColor.a = colliderVisualizationOpacity;
            instancedColliderMaterial.color = newColor;
        }
    }
    
    private void Update()
    {
        // Update material opacity if it changed
        if (instancedColliderMaterial.color.a != colliderVisualizationOpacity)
        {
            Color newColor = colliderHighlightColor;
            newColor.a = colliderVisualizationOpacity;
            instancedColliderMaterial.color = newColor;
        }
        
        // Check for key press to toggle collider visualization
        if (Input.GetKeyDown(toggleVisualizationKey))
        {
            ToggleColliderVisualization();
            Debug.Log($"Collider visualization {(showColliderBounds ? "enabled" : "disabled")} via key press");
        }
    }
    
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
    /// Toggle the visualization of colliders on all loaded objects
    /// </summary>
    public void ToggleColliderVisualization()
    {
        showColliderBounds = !showColliderBounds;
        
        if (showColliderBounds)
        {
            CreateColliderVisualizers();
        }
        else
        {
            DestroyColliderVisualizers();
        }
    }
    
    /// <summary>
    /// Create visualizers for all colliders in the scene
    /// </summary>
    private void CreateColliderVisualizers()
    {
        DestroyColliderVisualizers();
        
        if (colliderVisualizationMaterial == null)
        {
            Debug.LogWarning("Collider visualization material is not assigned. Creating a default material.");
            colliderVisualizationMaterial = new Material(Shader.Find("Standard"));
            colliderVisualizationMaterial.color = colliderHighlightColor;
        }
        
        foreach (GameObject obj in loadedObjects)
        {
            if (obj == null) continue;
            
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            
            foreach (Collider collider in colliders)
            {
                if (collider == null) continue;
                
                GameObject visualizer = CreateColliderVisualizer(collider);
                if (visualizer != null)
                {
                    colliderVisualizers.Add(visualizer);
                }
            }
        }
        
        Debug.Log($"Created {colliderVisualizers.Count} collider visualizers");
    }
    
    /// <summary>
    /// Create a visualizer for a specific collider
    /// </summary>
    private GameObject CreateColliderVisualizer(Collider collider)
    {
        GameObject visualizer = null;
        
        // Create different visualizers based on collider type
        if (collider is BoxCollider boxCollider)
        {
            visualizer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Set parent first so transform calculations are correct
            visualizer.transform.SetParent(boxCollider.transform, false);
            
            // Position at the collider's local center
            visualizer.transform.localPosition = boxCollider.center;
            visualizer.transform.localRotation = Quaternion.identity;
            visualizer.transform.localScale = boxCollider.size;
        }
        else if (collider is SphereCollider sphereCollider)
        {
            visualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            // Set parent first so transform calculations are correct
            visualizer.transform.SetParent(sphereCollider.transform, false);
            
            // Position at the collider's local center
            visualizer.transform.localPosition = sphereCollider.center;
            visualizer.transform.localRotation = Quaternion.identity;
            
            // Scale to match the collider's radius
            float diameter = sphereCollider.radius * 2f;
            visualizer.transform.localScale = new Vector3(diameter, diameter, diameter);
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            visualizer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            
            // Set parent first so transform calculations are correct
            visualizer.transform.SetParent(capsuleCollider.transform, false);
            
            // Position at the collider's local center
            visualizer.transform.localPosition = capsuleCollider.center;
            
            // Rotate based on capsule direction
            if (capsuleCollider.direction == 0) // X axis
            {
                visualizer.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            else if (capsuleCollider.direction == 2) // Z axis
            {
                visualizer.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            else // Y axis (default)
            {
                visualizer.transform.localRotation = Quaternion.identity;
            }
            
            // Scale to match the collider's dimensions
            float diameter = capsuleCollider.radius * 2f;
            
            // Capsule primitive's height includes the hemispherical ends, so we need to adjust
            float adjustedHeight = capsuleCollider.height;
            
            if (capsuleCollider.direction == 0) // X axis
            {
                visualizer.transform.localScale = new Vector3(adjustedHeight, diameter, diameter);
            }
            else if (capsuleCollider.direction == 1) // Y axis
            {
                visualizer.transform.localScale = new Vector3(diameter, adjustedHeight, diameter);
            }
            else // Z axis
            {
                visualizer.transform.localScale = new Vector3(diameter, diameter, adjustedHeight);
            }
        }
        else if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
        {
            visualizer = new GameObject("MeshColliderVisualizer");
            
            // Set parent first so transform calculations are correct
            visualizer.transform.SetParent(meshCollider.transform, false);
            visualizer.transform.localPosition = Vector3.zero;
            visualizer.transform.localRotation = Quaternion.identity;
            visualizer.transform.localScale = Vector3.one;
            
            MeshFilter meshFilter = visualizer.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = meshCollider.sharedMesh;
            
            MeshRenderer meshRenderer = visualizer.AddComponent<MeshRenderer>();
        }
        
        if (visualizer != null)
        {
            // Configure the visualizer
            visualizer.name = "ColliderVisualizer_" + collider.gameObject.name;
            
            // Configure the renderer
            Renderer renderer = visualizer.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Use a copy of the properly configured material
                renderer.material = new Material(instancedColliderMaterial);
            }
            
            // Disable collision on the visualizer
            Collider visualizerCollider = visualizer.GetComponent<Collider>();
            if (visualizerCollider != null)
            {
                Destroy(visualizerCollider);
            }
            
            // Set layer to something that won't be interactive
            visualizer.layer = LayerMask.NameToLayer("Ignore Raycast") != -1 ? 
                LayerMask.NameToLayer("Ignore Raycast") : 2; // 2 is the default "Ignore Raycast" layer
        }
        
        return visualizer;
    }
    
    /// <summary>
    /// Destroy all collider visualizers
    /// </summary>
    private void DestroyColliderVisualizers()
    {
        foreach (GameObject viz in colliderVisualizers)
        {
            if (viz != null)
            {
                Destroy(viz);
            }
        }
        colliderVisualizers.Clear();
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
            ShowLoadingScreen(sceneId);
            ClearScene();
            
            string url;
            if (sceneId == "dummy" && serverConfig != null && serverConfig is PortaltServerConfigV viewerConfig)
            {
                url = viewerConfig.GetActivityJoinUrl();
                Debug.Log($"Using join URL approach: {url}");
            }
            else if (serverConfig is PortaltServerConfig standardConfig)
            {
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
                    HideLoadingScreen();
                    return true;
                }
                
                Debug.Log($"Loaded scene configuration with {currentSceneConfig.objects.Count} objects");
                
                List<Task> loadingTasks = new List<Task>();
                foreach (SceneObject obj in currentSceneConfig.objects)
                {
                    loadingTasks.Add(LoadModelAsync(obj));
                }
                
                await Task.WhenAll(loadingTasks);
                
                if (showColliderBounds)
                {
                    CreateColliderVisualizers();
                }
                
                Debug.Log("Scene loading complete");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing scene configuration: {e.Message}");
                throw;
            }
            
            HideLoadingScreen();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
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
        // Destroy all loaded objects
        foreach (GameObject obj in loadedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        loadedObjects.Clear();
        
        // Destroy all collider visualizers
        DestroyColliderVisualizers();
        
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
        
        // Set layer for selection
        model.layer = LayerMask.NameToLayer("Selectable") != -1 ? 
            LayerMask.NameToLayer("Selectable") : 0;
            
        // Set child renderers to same layer
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.gameObject.layer = model.layer;
        }
        
        // Add metadata
        SceneObjectMetadata metadata = model.AddComponent<SceneObjectMetadata>();
        metadata.Initialize(objData.object_id, objData.modelUrl);
        
        // Add colliders if needed
        if (model.GetComponent<Collider>() == null)
        {
            Mesh colliderMesh = null;
            GameObject meshHolder = null;
            
            List<MeshFilter> meshFilters = new List<MeshFilter>(model.GetComponentsInChildren<MeshFilter>());
            
            // Find simple mesh to use for collider
            foreach (var filter in meshFilters)
            {
                if (filter != null && filter.sharedMesh != null && 
                    filter.sharedMesh.triangles.Length / 3 < 200)
                {
                    colliderMesh = filter.sharedMesh;
                    meshHolder = filter.gameObject;
                    break;
                }
            }
            
            // Try first available mesh if no simple one found
            if (colliderMesh == null && meshFilters.Count > 0)
            {
                var firstMesh = meshFilters[0];
                if (firstMesh != null && firstMesh.sharedMesh != null)
                {
                    colliderMesh = firstMesh.sharedMesh;
                    meshHolder = firstMesh.gameObject;
                }
            }
            
            // Try skinned mesh if no mesh filter found
            if (colliderMesh == null)
            {
                SkinnedMeshRenderer firstSkinnedMesh = model.GetComponentInChildren<SkinnedMeshRenderer>();
                if (firstSkinnedMesh != null && firstSkinnedMesh.sharedMesh != null)
                {
                    colliderMesh = firstSkinnedMesh.sharedMesh;
                    meshHolder = firstSkinnedMesh.gameObject;
                }
            }
            
            if (colliderMesh != null)
            {
                int triangleCount = colliderMesh.triangles.Length / 3;
                
                if (triangleCount > 2000)
                {
                    Debug.Log($"Mesh for {objData.object_id} is very complex ({triangleCount} triangles). Creating compound colliders.");
                    
                    bool addedChildColliders = false;
                    foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                    {
                        if (renderer.gameObject.GetComponent<Collider>() == null)
                        {
                            BoxCollider boxCol = renderer.gameObject.AddComponent<BoxCollider>();
                            addedChildColliders = true;
                        }
                    }
                    
                    if (!addedChildColliders || meshFilters.Count <= 1)
                    {
                        BoxCollider rootCollider = model.AddComponent<BoxCollider>();
                        Debug.Log($"Added BoxCollider to {objData.object_id} due to high complexity");
                    }
                    else
                    {
                        Debug.Log($"Added compound colliders to {objData.object_id}");
                    }
                }
                else if (triangleCount > 250)
                {
                    MeshCollider collider = meshHolder.AddComponent<MeshCollider>();
                    collider.sharedMesh = colliderMesh;
                    collider.convex = false;
                    Debug.Log($"Added non-convex MeshCollider to {objData.object_id} ({triangleCount} triangles)");
                }
                else
                {
                    try
                    {
                        MeshCollider collider = meshHolder.AddComponent<MeshCollider>();
                        collider.sharedMesh = colliderMesh;
                        collider.convex = true;
                        Debug.Log($"Added convex MeshCollider to {objData.object_id} ({triangleCount} triangles)");
                    }
                    catch (Exception e)
                    {
                        MeshCollider collider = meshHolder.GetComponent<MeshCollider>();
                        if (collider != null)
                        {
                            collider.convex = false;
                            Debug.Log($"Falling back to non-convex collider for {objData.object_id} due to: {e.Message}");
                        }
                        else
                        {
                            BoxCollider boxCollider = meshHolder.AddComponent<BoxCollider>();
                            Debug.Log($"Added BoxCollider to {objData.object_id} after convex mesh error");
                        }
                    }
                }
            }
            else
            {
                BoxCollider boxCollider = model.AddComponent<BoxCollider>();
                Debug.Log($"Added BoxCollider to {objData.object_id} as fallback (no suitable mesh found)");
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