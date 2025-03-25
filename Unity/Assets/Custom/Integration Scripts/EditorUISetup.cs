using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorUISetup : MonoBehaviour
{
    [Header("References")]
    public EditorIntegrationManager integrationManager;
    
    [Header("Configuration")]
    public PortaltServerConfig serverConfig;
    
    [Header("UI Prefabs")]
    public Canvas canvasPrefab;
    public TMP_InputField inputFieldPrefab;
    public Button buttonPrefab;
    public TMP_Dropdown dropdownPrefab;
    public Image panelPrefab;
    
    [Header("Created UI References")]
    public Canvas mainCanvas;
    public TMP_InputField activityIdInput;
    public Button loadActivityButton;
    public TMP_Dropdown sceneSelector;
    public Button saveSceneButton;
    public TMP_Dropdown activitySelector;
    
    #if UNITY_EDITOR
    [ContextMenu("Create Editor UI")]
    public void CreateEditorUI()
    {
        // Make sure we have all required prefabs
        if (canvasPrefab == null || inputFieldPrefab == null || buttonPrefab == null || 
            dropdownPrefab == null || panelPrefab == null)
        {
            Debug.LogError("Missing UI prefabs. Please assign all prefabs in the inspector.");
            return;
        }

        // Destroy existing UI to prevent duplicates
        if (mainCanvas != null)
        {
            DestroyImmediate(mainCanvas.gameObject);
        }

        // Create a completely new standalone UI canvas
        GameObject canvasObj = new GameObject("PortaltEditorUI");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 10; // Below loading screen but above other UIs
        
        // Add canvas scaler for responsive UI
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f; // Balanced scaling between width and height
        
        // Add required GraphicRaycaster component
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create panel background - now using direct GameObject creation
        GameObject panel = new GameObject("ControlPanel");
        panel.transform.SetParent(mainCanvas.transform, false);
        
        // Add Image component for visual
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Semi-transparent dark panel
        
        // Configure the panel's RectTransform
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(0.5f, 1);
        panelRect.sizeDelta = new Vector2(0, 120);
        panelRect.anchoredPosition = new Vector2(0, 0);
        
        // Create activity selector dropdown
        GameObject activitySelectorObj = Instantiate(dropdownPrefab.gameObject, panel.transform);
        activitySelectorObj.name = "ActivitySelector";
        activitySelector = activitySelectorObj.GetComponent<TMP_Dropdown>();
        
        // Configure activity selector
        RectTransform activityDropdownRect = activitySelector.GetComponent<RectTransform>();
        activityDropdownRect.anchorMin = new Vector2(0, 0.5f);
        activityDropdownRect.anchorMax = new Vector2(0.4f, 0.9f);
        activityDropdownRect.anchoredPosition = new Vector2(10, 0);
        activityDropdownRect.sizeDelta = new Vector2(-20, 0);
        
        // Add default option
        activitySelector.options.Clear();
        activitySelector.options.Add(new TMP_Dropdown.OptionData("Select Activity..."));
        activitySelector.RefreshShownValue();
        
        // Create scene selector dropdown
        GameObject sceneSelectorObj = Instantiate(dropdownPrefab.gameObject, panel.transform);
        sceneSelectorObj.name = "SceneSelector";
        sceneSelector = sceneSelectorObj.GetComponent<TMP_Dropdown>();
        
        // Configure scene selector
        RectTransform dropdownRect = sceneSelector.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.4f, 0.5f);
        dropdownRect.anchorMax = new Vector2(0.8f, 0.9f);
        dropdownRect.anchoredPosition = new Vector2(5, 0);
        dropdownRect.sizeDelta = new Vector2(-10, 0);
        
        // Add default option
        sceneSelector.options.Clear();
        sceneSelector.options.Add(new TMP_Dropdown.OptionData("Select Scene..."));
        sceneSelector.RefreshShownValue();
        
        // Create save scene button
        GameObject saveButtonObj = Instantiate(buttonPrefab.gameObject, panel.transform);
        saveButtonObj.name = "SaveSceneButton";
        saveSceneButton = saveButtonObj.GetComponent<Button>();
        
        // Configure save button
        RectTransform saveButtonRect = saveSceneButton.GetComponent<RectTransform>();
        saveButtonRect.anchorMin = new Vector2(0.8f, 0.5f);
        saveButtonRect.anchorMax = new Vector2(1, 0.9f);
        saveButtonRect.anchoredPosition = new Vector2(-5, 0);
        saveButtonRect.sizeDelta = new Vector2(-10, 0);
        
        TextMeshProUGUI buttonText = saveSceneButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = "Save";
        }
        
        // Create status label
        GameObject statusLabel = new GameObject("StatusLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusLabel.transform.SetParent(panel.transform, false);
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.1f);
        statusRect.anchorMax = new Vector2(1, 0.4f);
        statusRect.anchoredPosition = Vector2.zero;
        statusRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI statusText = statusLabel.GetComponent<TextMeshProUGUI>();
        statusText.text = "Portalt Scene Editor";
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 14;
        
        // Create mode indicator
        GameObject modeIndicator = new GameObject("ModeIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
        modeIndicator.transform.SetParent(panel.transform, false);
        RectTransform modeRect = modeIndicator.GetComponent<RectTransform>();
        modeRect.anchorMin = new Vector2(0, 0);
        modeRect.anchorMax = new Vector2(0.3f, 0.1f);
        modeRect.anchoredPosition = new Vector2(10, 5);
        modeRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI modeText = modeIndicator.GetComponent<TextMeshProUGUI>();
        modeText.text = "Press TAB to toggle UI/Tractor Beam mode";
        modeText.alignment = TextAlignmentOptions.Left;
        modeText.fontSize = 12;
        modeText.color = Color.yellow;
        
        // Connect UI to integration manager
        if (integrationManager != null)
        {
            integrationManager.sceneSelector = sceneSelector;
            integrationManager.saveSceneButton = saveSceneButton;
            integrationManager.activitySelector = activitySelector;
        }
        else
        {
            Debug.LogWarning("Integration Manager reference not set. UI elements will not be connected automatically.");
        }
        
        // Make UI persist between scenes
        DontDestroyOnLoad(canvasObj);
        
        Debug.Log("Editor UI created successfully with proper scaling");
    }
    
    [ContextMenu("Create Full Integration Setup")]
    public void CreateFullIntegrationSetup()
    {
        // Create or find server config
        if (serverConfig == null)
        {
            // Look for existing config asset
            string[] guids = AssetDatabase.FindAssets("t:PortaltServerConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                serverConfig = AssetDatabase.LoadAssetAtPath<PortaltServerConfig>(path);
                Debug.Log($"Found existing PortaltServerConfig at {path}");
            }
            else
            {
                // Create new config asset
                serverConfig = ScriptableObject.CreateInstance<PortaltServerConfig>();
                
                // Save it to the project
                string path = "Assets/Custom/Integration Scripts/PortaltServerConfig.asset";
                AssetDatabase.CreateAsset(serverConfig, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"Created new PortaltServerConfig at {path}");
            }
        }
        
        // Create integration manager if needed
        if (integrationManager == null)
        {
            GameObject managerObj = new GameObject("PortaltIntegrationManager");
            managerObj.transform.SetParent(transform);
            integrationManager = managerObj.AddComponent<EditorIntegrationManager>();
        }
        
        // Create UI first - do this after creating integration manager
        CreateEditorUI();
        
        // Create PortaltAPIClient if needed
        PortaltAPIClient apiClient = FindObjectOfType<PortaltAPIClient>();
        if (apiClient == null)
        {
            GameObject apiObj = new GameObject("PortaltAPIClient");
            apiObj.transform.SetParent(transform);
            apiClient = apiObj.AddComponent<PortaltAPIClient>();
        }
        
        // Create PortaltSceneLoader if needed
        PortaltSceneLoader sceneLoader = FindObjectOfType<PortaltSceneLoader>();
        if (sceneLoader == null)
        {
            GameObject loaderObj = new GameObject("PortaltSceneLoader");
            loaderObj.transform.SetParent(transform);
            sceneLoader = loaderObj.AddComponent<PortaltSceneLoader>();
            sceneLoader.serverConfig = serverConfig;
        }
        
        // Create PortaltSceneExporter if needed
        PortaltSceneExporter sceneExporter = FindObjectOfType<PortaltSceneExporter>();
        if (sceneExporter == null)
        {
            GameObject exporterObj = new GameObject("PortaltSceneExporter");
            exporterObj.transform.SetParent(transform);
            sceneExporter = exporterObj.AddComponent<PortaltSceneExporter>();
            sceneExporter.serverConfig = serverConfig;
            sceneExporter.sceneLoader = sceneLoader;
        }
        
        // Create loading screen if it doesn't exist
        LoadingScreenManager existingLoadingScreen = FindObjectOfType<LoadingScreenManager>();
        if (existingLoadingScreen == null)
        {
            CreateLoadingScreen();
        }
        
        // Find or create AdminController for mode switching
        AdminController adminController = FindObjectOfType<AdminController>();
        if (adminController != null)
        {
            // Find the AdminObjectSelector
            AdminObjectSelector objectSelector = FindObjectOfType<AdminObjectSelector>();
            if (objectSelector != null)
            {
                adminController.objectSelector = objectSelector;
            }
            
            // Connect admin controller to integration manager
            if (integrationManager != null)
            {
                integrationManager.adminController = adminController;
            }
            
            // Find the mode indicator text
            GameObject modeIndicatorObj = GameObject.Find("ModeIndicator");
            if (modeIndicatorObj != null)
            {
                TextMeshProUGUI modeText = modeIndicatorObj.GetComponent<TextMeshProUGUI>();
                if (modeText != null)
                {
                    // Add the mode indicator updater
                    ModeIndicatorUpdater updater = modeIndicatorObj.AddComponent<ModeIndicatorUpdater>();
                    updater.adminController = adminController;
                    updater.modeIndicatorText = modeText;
                }
            }
        }
        
        // Connect everything to the server config
        if (serverConfig != null)
        {
            if (apiClient != null) apiClient.serverConfig = serverConfig;
            if (sceneLoader != null) sceneLoader.serverConfig = serverConfig;
            if (sceneExporter != null) sceneExporter.serverConfig = serverConfig;
            
            // Connect components to integration manager
            if (integrationManager != null)
            {
                integrationManager.serverConfig = serverConfig;
                integrationManager.apiClient = apiClient;
                integrationManager.sceneLoader = sceneLoader;
                integrationManager.sceneExporter = sceneExporter;
                
                // Connect UI to integration manager
                if (activitySelector != null && sceneSelector != null && saveSceneButton != null)
                {
                    integrationManager.activitySelector = activitySelector;
                    integrationManager.sceneSelector = sceneSelector;
                    integrationManager.saveSceneButton = saveSceneButton;
                }
            }
        }
        
        Debug.Log("Full integration setup created successfully");
    }
    
    [ContextMenu("Create Loading Screen")]
    public void CreateLoadingScreen()
    {
        // First, check for any existing loading screen and remove it
        LoadingScreenManager existingManager = FindObjectOfType<LoadingScreenManager>();
        if (existingManager != null)
        {
            DestroyImmediate(existingManager.gameObject);
        }
        
        // Create a completely new loading screen
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
        
        // Add LoadingScreenManager
        LoadingScreenManager loadingManager = loadingScreenObj.AddComponent<LoadingScreenManager>();
        loadingManager.loadingCanvasGroup = canvasGroup;
        loadingManager.loadingBackground = bgImage;
        
        // Make sure it persists between scenes
        DontDestroyOnLoad(loadingScreenObj);
        
        // Initialize in hidden state
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        
        Debug.Log("Loading screen created successfully");
    }
    #endif
    
    private void OnValidate()
    {
        // Auto-connect to integration manager if available
        if (integrationManager != null && 
            sceneSelector != null && 
            saveSceneButton != null && 
            activitySelector != null)
        {
            integrationManager.sceneSelector = sceneSelector;
            integrationManager.saveSceneButton = saveSceneButton;
            integrationManager.activitySelector = activitySelector;
        }
    }
} 