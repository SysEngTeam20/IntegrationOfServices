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
    public PortaltServerConfig serverConfig;
    
    [Header("Activity ID")]
    [Tooltip("ID of the activity to automatically load on start")]
    public string activityIdToLoad;
    
    [Header("Optional Scene ID")]
    [Tooltip("If set, this specific scene will be loaded instead of the first scene in the activity")]
    public string specificSceneIdToLoad;
    
    [Header("Load Automatically")]
    public bool loadOnStart = true;
    
    // Component references
    private PortaltAPIClient apiClient;
    private PortaltSceneLoader sceneLoader;
    
    // Start is called before the first frame update
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
        
        // Auto-load the content
        if (loadOnStart)
        {
            StartCoroutine(LoadActivityWithDelay());
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
            apiClient.serverConfig = serverConfig;
            apiClientObj.transform.SetParent(transform);
            DontDestroyOnLoad(apiClientObj);
        }
        
        // Set up scene loader
        sceneLoader = FindObjectOfType<PortaltSceneLoader>();
        if (sceneLoader == null)
        {
            GameObject loaderObj = new GameObject("PortaltSceneLoader");
            sceneLoader = loaderObj.AddComponent<PortaltSceneLoader>();
            sceneLoader.serverConfig = serverConfig;
            loaderObj.transform.SetParent(transform);
            DontDestroyOnLoad(loaderObj);
        }
        
        // Ensure components have the server config
        if (serverConfig != null)
        {
            apiClient.serverConfig = serverConfig;
            sceneLoader.serverConfig = serverConfig;
            
            Debug.Log($"Components set up with server IP: {serverConfig.serverIp}");
        }
        else
        {
            Debug.LogError("No server config assigned! Please create and assign a PortaltServerConfig asset.");
        }
    }
    
    /// <summary>
    /// Wait a short time before loading to ensure all systems are initialized
    /// </summary>
    private IEnumerator LoadActivityWithDelay()
    {
        // Wait for a brief moment to ensure everything is initialized
        yield return new WaitForSeconds(0.5f);
        
        // Begin loading specified activity
        LoadActivity();
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