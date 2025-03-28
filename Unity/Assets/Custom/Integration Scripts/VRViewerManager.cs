using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main manager for the VR Viewer application.
/// Handles loading scenes directly from Portalt server without editing capability.
/// </summary>
public class VRViewerManager : MonoBehaviour
{
    [Header("Configuration")]
    public PortaltServerConfigV serverConfig;
    
    [Header("Activity ID")]
    [Tooltip("ID of the activity to automatically load on start")]
    public string activityIdToLoad;
    
    [Header("Optional Scene ID")]
    [Tooltip("If set, this specific scene will be loaded instead of the first scene in the activity")]
    public string specificSceneIdToLoad;
    
    [Header("Load Automatically")]
    public bool loadOnStart = true;
    
    [Header("Joincode Settings")]
    [Tooltip("Whether to require a joincode before loading content")]
    public bool requireJoincode = true;
    public JoincodeInputManager joincodeManager;
    
    // Component references
    private PortaltAPIClient apiClient;
    private PortaltSceneLoader sceneLoader;
    
    void Start()
    {
        Debug.Log("VRViewerManager starting...");
        
        if (string.IsNullOrEmpty(activityIdToLoad))
        {
            Debug.LogWarning("No activity ID specified! Please set activityIdToLoad in the inspector.");
            return;
        }
        
        // Initialize components
        SetupComponents();
        
        // Check if we need a joincode first
        if (requireJoincode && string.IsNullOrEmpty(serverConfig.joinCode))
        {
            ShowJoincodeUI();
        }
        // Auto-load the content if not requiring joincode or already have one
        else if (loadOnStart)
        {
            StartCoroutine(LoadWithDelay());
        }
    }
    
    /// <summary>
    /// Initialize and connect all required components
    /// </summary>
    private void SetupComponents()
    {
        // Create loading screen if needed
        if (LoadingScreenManager.Instance == null)
        {
            CreateLoadingScreen();
        }
        
        // Set up API client
        apiClient = FindObjectOfType<PortaltAPIClient>();
        if (apiClient == null)
        {
            GameObject apiClientObj = new GameObject("PortaltAPIClient");
            apiClient = apiClientObj.AddComponent<PortaltAPIClient>();
            apiClientObj.transform.SetParent(transform);
            DontDestroyOnLoad(apiClientObj);
        }
        
        // Set up scene loader
        sceneLoader = FindObjectOfType<PortaltSceneLoader>();
        if (sceneLoader == null)
        {
            GameObject loaderObj = new GameObject("PortaltSceneLoader");
            sceneLoader = loaderObj.AddComponent<PortaltSceneLoader>();
            loaderObj.transform.SetParent(transform);
            DontDestroyOnLoad(loaderObj);
        }
        
        // Find joincode manager if not set
        if (joincodeManager == null)
        {
            joincodeManager = FindObjectOfType<JoincodeInputManager>();
        }
        
        // Ensure components have the server config
        if (serverConfig != null)
        {
            // We need to create a temporary standard config for the API client
            // since it expects the standard config format
            var tempConfig = ScriptableObject.CreateInstance<PortaltServerConfig>();
            tempConfig.serverIp = serverConfig.serverIp;
            tempConfig.serverPort = serverConfig.serverPort;
            tempConfig.pairingCode = serverConfig.joinCode; // Map joinCode to pairingCode
            
            apiClient.serverConfig = tempConfig;
            
            // For scene loader, we can directly assign our ConfigV since it now accepts ScriptableObject
            sceneLoader.serverConfig = serverConfig;
            
            Debug.Log($"Components set up with server IP: {serverConfig.serverIp}");
        }
        else
        {
            Debug.LogError("No server config assigned! Please create and assign a PortaltServerConfigV asset.");
        }
    }
    
    /// <summary>
    /// Shows the joincode input UI
    /// </summary>
    private void ShowJoincodeUI()
    {
        if (joincodeManager != null)
        {
            joincodeManager.ShowJoincodeUI();
        }
        else
        {
            Debug.LogError("No joincode manager found!");
        }
    }
    
    /// <summary>
    /// Wait a short time before loading to ensure all systems are initialized
    /// </summary>
    private IEnumerator LoadWithDelay()
    {
        // Wait for a brief moment to ensure everything is initialized
        yield return new WaitForSeconds(0.5f);
        
        // Begin loading from the join URL
        LoadFromJoinCode();
    }
    
    /// <summary>
    /// Load content using the joinCode
    /// </summary>
    public void LoadFromJoinCode()
    {
        if (serverConfig == null)
        {
            Debug.LogError("Cannot load: serverConfig is null!");
            return;
        }
        
        if (string.IsNullOrEmpty(serverConfig.joinCode))
        {
            Debug.LogError("Cannot load: joinCode is empty!");
            return;
        }
        
        // Ensure scene loader is initialized
        if (sceneLoader == null)
        {
            Debug.LogError("Cannot load: scene loader is not initialized!");
            SetupComponents(); // Try to set up components again
            
            if (sceneLoader == null)
            {
                Debug.LogError("Failed to initialize scene loader!");
                return;
            }
        }
        
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreen("Loading content...");
        }
        
        Debug.Log($"Loading content with join code: {serverConfig.joinCode}");
        
        // Use direct fetch from the join URL to get scene configuration
        StartCoroutine(FetchAndLoadFromJoinUrl());
    }
    
    /// <summary>
    /// Fetch scene configuration directly from join URL and load it
    /// </summary>
    private IEnumerator FetchAndLoadFromJoinUrl()
    {
        if (serverConfig == null)
        {
            Debug.LogError("Cannot fetch: serverConfig is null!");
            yield break;
        }
        
        string url = serverConfig.GetActivityJoinUrl();
        Debug.Log($"Fetching content from join URL: {url}");
        
        // Use scene loader to directly load from the join URL
        // We use a dummy scene ID since the actual URL is already formed in the config
        bool success = false;
        
        // Verify scene loader is available
        if (sceneLoader == null)
        {
            Debug.LogError("Cannot load: scene loader is not initialized!");
            
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.HideLoadingScreen();
            }
            
            yield break;
        }
        
        // Wrap the task in a helper coroutine without try-catch
        yield return ExecuteSceneLoading("dummy", result => success = result);
        
        if (success)
        {
            Debug.Log("Successfully loaded content from join URL");
        }
        else
        {
            Debug.LogError("Failed to load content from join URL");
            
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.HideLoadingScreen();
            }
        }
    }
    
    /// <summary>
    /// Helper method to wrap the async scene loading in a coroutine-friendly way
    /// </summary>
    private IEnumerator ExecuteSceneLoading(string sceneId, System.Action<bool> callback)
    {
        if (sceneLoader == null)
        {
            Debug.LogError("Cannot execute scene loading: scene loader is null!");
            callback(false);
            yield break;
        }
        
        bool success = false;
        
        // Create a simple state machine to handle errors without yielding in try/catch
        var task = sceneLoader.LoadSceneFromApi(sceneId);
        if (task == null)
        {
            Debug.LogError("Task from LoadSceneFromApi is null!");
            callback(false);
            yield break;
        }
        
        var loadingOperation = task.AsManualEnumerator(result => success = result);
        if (loadingOperation == null)
        {
            Debug.LogError("Manual enumerator is null!");
            callback(false);
            yield break;
        }
        
        // This is outside of try/catch - yielding is safe here
        while (true)
        {
            try
            {
                if (!loadingOperation.MoveNext())
                    break;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading from URL: {ex.Message}");
                
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.HideLoadingScreen();
                }
                
                callback(false);
                yield break;
            }
            
            yield return null; // Just yield a frame since Current is always null in our adapter
        }
        
        callback(success);
    }
    
    /// <summary>
    /// Load the specified activity
    /// </summary>
    public void LoadActivity()
    {
        if (string.IsNullOrEmpty(activityIdToLoad))
        {
            Debug.LogError("Cannot load activity: activityIdToLoad is empty!");
            return;
        }
        
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreen("Loading activity...");
        }
        
        Debug.Log($"Loading activity with ID: {activityIdToLoad}");
        
        // If a specific scene ID is provided, load that directly
        if (!string.IsNullOrEmpty(specificSceneIdToLoad))
        {
            LoadSpecificScene(activityIdToLoad, specificSceneIdToLoad);
            return;
        }
        
        // Otherwise load the activity and its first scene
        StartCoroutine(apiClient.GetActivity(activityIdToLoad, (activity) => {
            if (activity != null)
            {
                Debug.Log($"Successfully loaded activity: {activity.title}");
                
                if (activity.scenes != null && activity.scenes.Count > 0)
                {
                    string firstSceneId = activity.scenes[0].id;
                    Debug.Log($"Loading first scene (ID: {firstSceneId})");
                    LoadScene(firstSceneId);
                }
                else
                {
                    Debug.LogError("Activity contains no scenes!");
                    if (LoadingScreenManager.Instance != null)
                    {
                        LoadingScreenManager.Instance.HideLoadingScreen();
                    }
                }
            }
            else
            {
                Debug.LogError($"Failed to load activity with ID: {activityIdToLoad}");
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.HideLoadingScreen();
                }
            }
        }));
    }
    
    /// <summary>
    /// Load a specific scene directly using its ID
    /// </summary>
    private void LoadScene(string sceneId)
    {
        if (string.IsNullOrEmpty(sceneId))
        {
            Debug.LogError("Cannot load scene: sceneId is empty!");
            return;
        }
        
        Debug.Log($"Loading scene with ID: {sceneId}");
        LoadSceneAsync(sceneId);
    }
    
    /// <summary>
    /// Load a specific scene with known activity ID
    /// </summary>
    private void LoadSpecificScene(string activityId, string sceneId)
    {
        Debug.Log($"Loading specific scene - Activity: {activityId}, Scene: {sceneId}");
        
        // Store the activity ID in the scene loader
        sceneLoader.currentActivityId = activityId;
        
        // Load the scene directly
        LoadScene(sceneId);
    }
    
    /// <summary>
    /// Asynchronously load a scene
    /// </summary>
    private async void LoadSceneAsync(string sceneId)
    {
        if (sceneLoader != null)
        {
            bool success = await sceneLoader.LoadSceneFromApi(sceneId);
            
            if (success)
            {
                Debug.Log($"Scene {sceneId} loaded successfully");
            }
            else
            {
                Debug.LogError($"Failed to load scene {sceneId}");
            }
        }
        else
        {
            Debug.LogError("Scene loader is missing!");
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.HideLoadingScreen();
            }
        }
    }
    
    /// <summary>
    /// Create a simple loading screen for the VR app
    /// </summary>
    private void CreateLoadingScreen()
    {
        // Create a simple loading screen
        GameObject loadingScreenObj = new GameObject("LoadingScreen");
        
        // Add Canvas component
        Canvas loadingCanvas = loadingScreenObj.AddComponent<Canvas>();
        loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadingCanvas.sortingOrder = 999; // Ensure it's on top of everything
        
        // Add Canvas Scaler
        CanvasScaler scaler = loadingScreenObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add Graphic Raycaster
        loadingScreenObj.AddComponent<GraphicRaycaster>();
        
        // Add CanvasGroup for fading
        CanvasGroup canvasGroup = loadingScreenObj.AddComponent<CanvasGroup>();
        
        // Create background panel
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(loadingScreenObj.transform, false);
        
        // Add Image component
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark semi-transparent background
        
        // Set up RectTransform to cover entire screen
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; // This sets left and bottom to 0
        bgRect.offsetMax = Vector2.zero; // This sets right and top to 0
        
        // Create loading text object
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(bgPanel.transform, false);
        
        // Add TextMeshProUGUI component
        TMPro.TextMeshProUGUI loadingText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        loadingText.text = "Loading...";
        loadingText.fontSize = 36;
        loadingText.alignment = TMPro.TextAlignmentOptions.Center;
        loadingText.color = Color.white;
        
        // Position text in center
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600, 100);
        textRect.anchoredPosition = Vector2.zero;
        
        // Add LoadingScreenManager component
        LoadingScreenManager loadingManager = loadingScreenObj.AddComponent<LoadingScreenManager>();
        loadingManager.loadingCanvasGroup = canvasGroup;
        loadingManager.loadingBackground = bgImage;
        loadingManager.loadingText = loadingText;
        
        // Make loading screen persist between scenes
        DontDestroyOnLoad(loadingScreenObj);
        
        // Initialize in hidden state
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        
        Debug.Log("Loading screen created successfully");
    }
}

// Helper extension method to convert Task<bool> to IEnumerator for coroutines
public static class TaskExtensions
{
    public static IEnumerator AsIEnumerator(this System.Threading.Tasks.Task<bool> task, System.Action<bool> callback)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        if (task.IsFaulted)
        {
            Debug.LogError($"Task faulted: {task.Exception}");
            callback(false);
        }
        else
        {
            callback(task.Result);
        }
    }
    
    // New method that returns an enumerable for manual iteration
    public static System.Collections.IEnumerator AsManualEnumerator(this System.Threading.Tasks.Task<bool> task, System.Action<bool> callback)
    {
        return new TaskToEnumeratorAdapter<bool>(task, callback);
    }
    
    // Helper class for manual enumeration
    private class TaskToEnumeratorAdapter<T> : System.Collections.IEnumerator
    {
        private System.Threading.Tasks.Task<T> _task;
        private System.Action<T> _callback;
        private bool _isComplete = false;
        
        public TaskToEnumeratorAdapter(System.Threading.Tasks.Task<T> task, System.Action<T> callback)
        {
            _task = task;
            _callback = callback;
        }
        
        public object Current => null;
        
        public bool MoveNext()
        {
            if (_isComplete)
                return false;
                
            if (_task.IsCompleted)
            {
                _isComplete = true;
                
                if (_task.IsFaulted)
                {
                    throw _task.Exception;
                }
                else
                {
                    _callback(_task.Result);
                }
                
                return false;
            }
            
            return true;
        }
        
        public void Reset()
        {
            throw new System.NotSupportedException();
        }
    }
} 