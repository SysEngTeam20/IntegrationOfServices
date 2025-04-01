using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
    
    // Config UI References
    [Header("Config UI References")]
    public TMP_InputField pairingCodeInput;
    public Button saveConfigButton;
    public Button toggleConfigButton;
    public GameObject configPanel;
    
    [Header("Instructions UI")]
    public Button toggleInstructionsButton;
    public GameObject instructionsPanel;
    
    private bool configPanelVisible = false;
    private bool instructionsPanelVisible = false;
    
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
        dropdownRect.anchorMax = new Vector2(0.7f, 0.9f);
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
        saveButtonRect.anchorMin = new Vector2(0.7f, 0.5f);
        saveButtonRect.anchorMax = new Vector2(0.85f, 0.9f);
        saveButtonRect.anchoredPosition = new Vector2(-5, 0);
        saveButtonRect.sizeDelta = new Vector2(-10, 0);
        
        TextMeshProUGUI buttonText = saveSceneButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = "Save";
        }
        
        // Create config toggle button
        GameObject configToggleObj = Instantiate(buttonPrefab.gameObject, panel.transform);
        configToggleObj.name = "ToggleConfigButton";
        toggleConfigButton = configToggleObj.GetComponent<Button>();
        
        // Configure config toggle button
        RectTransform configToggleRect = toggleConfigButton.GetComponent<RectTransform>();
        configToggleRect.anchorMin = new Vector2(0.85f, 0.5f);
        configToggleRect.anchorMax = new Vector2(0.92f, 0.9f);
        configToggleRect.anchoredPosition = new Vector2(-5, 0);
        configToggleRect.sizeDelta = new Vector2(-5, 0);
        
        TextMeshProUGUI configButtonText = toggleConfigButton.GetComponentInChildren<TextMeshProUGUI>();
        if (configButtonText != null)
        {
            configButtonText.text = "Config";
        }
        
        // Create instructions toggle button
        GameObject instructionsToggleObj = Instantiate(buttonPrefab.gameObject, panel.transform);
        instructionsToggleObj.name = "ToggleInstructionsButton";
        toggleInstructionsButton = instructionsToggleObj.GetComponent<Button>();
        
        // Configure instructions toggle button
        RectTransform instructionsToggleRect = toggleInstructionsButton.GetComponent<RectTransform>();
        instructionsToggleRect.anchorMin = new Vector2(0.92f, 0.5f);
        instructionsToggleRect.anchorMax = new Vector2(1f, 0.9f);
        instructionsToggleRect.anchoredPosition = new Vector2(-5, 0);
        instructionsToggleRect.sizeDelta = new Vector2(-5, 0);
        
        TextMeshProUGUI instructionsButtonText = toggleInstructionsButton.GetComponentInChildren<TextMeshProUGUI>();
        if (instructionsButtonText != null)
        {
            instructionsButtonText.text = "Help";
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
        
        // Create configuration panel
        CreateConfigPanel();
        
        // Create instructions panel
        CreateInstructionsPanel();
        
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
    
    private void CreateConfigPanel()
    {
        // Destroy any existing config panel
        if (configPanel != null)
        {
            DestroyImmediate(configPanel);
            configPanel = null;
        }

        // Create a panel for server configuration
        GameObject configPanelObj = new GameObject("ConfigPanel");
        configPanelObj.transform.SetParent(mainCanvas.transform, false);
        
        // Add Image component for visual
        Image configPanelImage = configPanelObj.AddComponent<Image>();
        configPanelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        
        // Configure the panel's RectTransform - make it smaller now that we only have one field
        RectTransform configPanelRect = configPanelObj.GetComponent<RectTransform>();
        configPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        configPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        configPanelRect.pivot = new Vector2(0.5f, 0.5f);
        configPanelRect.sizeDelta = new Vector2(400, 160); // Reduced height since we now only have one input field
        configPanelRect.anchoredPosition = Vector2.zero;
        
        configPanel = configPanelObj;
        Debug.Log($"Created config panel: {configPanel.name}");
        
        // Create title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(configPanelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 40);
        titleRect.anchoredPosition = new Vector2(0, 0);
        
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Cloud Connection";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        
        // Create Pairing Code Input (only input field now)
        CreateConfigInput("PairingCode", "Pairing Code:", 0, out pairingCodeInput);
        
        // Create Save Config Button
        GameObject saveConfigObj = Instantiate(buttonPrefab.gameObject, configPanelObj.transform);
        saveConfigObj.name = "SaveConfigButton";
        saveConfigButton = saveConfigObj.GetComponent<Button>();
        
        RectTransform saveConfigRect = saveConfigButton.GetComponent<RectTransform>();
        saveConfigRect.anchorMin = new Vector2(0.5f, 0);
        saveConfigRect.anchorMax = new Vector2(0.5f, 0);
        saveConfigRect.pivot = new Vector2(0.5f, 0);
        saveConfigRect.sizeDelta = new Vector2(150, 40);
        saveConfigRect.anchoredPosition = new Vector2(0, 20);
        
        TextMeshProUGUI saveConfigText = saveConfigButton.GetComponentInChildren<TextMeshProUGUI>();
        if (saveConfigText != null)
        {
            saveConfigText.text = "Connect";
        }
        
        // Add onClick listener for save button
        saveConfigButton.onClick.RemoveAllListeners();
        #if UNITY_EDITOR
        saveConfigButton.onClick.AddListener(() => {
            SaveConfiguration();
        });
        #else
        saveConfigButton.onClick.AddListener(() => {
            SaveConfigurationRuntime();
        });
        #endif
        Debug.Log("Added save button listener");
        
        // Load current values
        if (serverConfig != null)
        {
            pairingCodeInput.text = serverConfig.pairingCode;
            Debug.Log("Loaded initial pairing code from server config");
        }
        else
        {
            Debug.LogError("ServerConfig is null when creating config panel!");
        }
        
        // Hide panel by default
        configPanel.SetActive(false);
        configPanelVisible = false;
    }
    
    private void CreateConfigInput(string name, string label, int position, out TMP_InputField inputField)
    {
        // Create container
        GameObject container = new GameObject(name + "Container", typeof(RectTransform));
        container.transform.SetParent(configPanel.transform, false);
        
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.sizeDelta = new Vector2(0, 50);
        containerRect.anchoredPosition = new Vector2(0, -50 - (position * 50));
        
        // Create label
        GameObject labelObj = new GameObject(name + "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(container.transform, false);
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0.3f, 0.5f);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.sizeDelta = new Vector2(0, 30);
        labelRect.anchoredPosition = new Vector2(10, 0);
        
        TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.fontSize = 14;
        
        // Create input field
        GameObject inputObj = Instantiate(inputFieldPrefab.gameObject, container.transform);
        inputObj.name = name + "Input";
        inputField = inputObj.GetComponent<TMP_InputField>();
        
        RectTransform inputRect = inputField.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.3f, 0.5f);
        inputRect.anchorMax = new Vector2(1f, 0.5f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(-20, 30);
        inputRect.anchoredPosition = new Vector2(0, 0);
    }
    
    public void ToggleConfigPanel()
    {
        Debug.Log("Config panel toggle button clicked");
        
        // If panel is visible, hide it
        if (configPanelVisible && configPanel != null)
        {
            configPanel.SetActive(false);
            configPanelVisible = false;
            Debug.Log("Config panel hidden");
        }
        // If panel is not visible, show it using our comprehensive method
        else
        {
            ShowConfigurationPanel();
        }
    }
    
    public void SaveConfiguration()
    {
        Debug.Log("Attempting to save configuration");
        
        if (serverConfig == null)
        {
            Debug.LogError("No server config reference assigned!");
            return;
        }
        
        // Update the config - only pairing code is needed now
        serverConfig.pairingCode = pairingCodeInput.text;
        Debug.Log($"Set pairing code to: {pairingCodeInput.text}");
        
        // Save changes to asset
        #if UNITY_EDITOR
        EditorUtility.SetDirty(serverConfig);
        AssetDatabase.SaveAssets();
        #endif
        
        Debug.Log("Server configuration saved and applied!");
        
        // Hide panel
        configPanel.SetActive(false);
        configPanelVisible = false;
        
        // Refresh activities list from new server config
        RefreshActivitiesList();
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
    
    // Runtime method to save configuration changes in builds
    public void SaveConfigurationRuntime()
    {
        Debug.Log("Saving configuration at runtime");
        
        if (serverConfig == null)
        {
            Debug.LogError("No server config reference assigned!");
            return;
        }
        
        if (pairingCodeInput == null)
        {
            Debug.LogError("Pairing code input field reference is missing!");
            return;
        }
        
        // Update config - only pairing code is needed now
        serverConfig.pairingCode = pairingCodeInput.text;
        Debug.Log($"Set pairing code to: {pairingCodeInput.text}");
        
        // Hide panel
        configPanel.SetActive(false);
        configPanelVisible = false;
        
        Debug.Log("Configuration saved at runtime!");
        
        // Refresh activities list from new server config
        RefreshActivitiesList();
    }
    
    // Helper method to refresh activities after configuration changes
    private void RefreshActivitiesList()
    {
        if (integrationManager != null)
        {
            Debug.Log("Refreshing activities list with new server configuration");
            
            try
            {
                // First ensure all components have the updated server config
                if (integrationManager.apiClient != null)
                {
                    integrationManager.apiClient.serverConfig = serverConfig;
                    Debug.Log("Updated API client server config");
                }
                
                if (integrationManager.sceneLoader != null)
                {
                    integrationManager.sceneLoader.serverConfig = serverConfig;
                    Debug.Log("Updated scene loader server config");
                }
                
                if (integrationManager.sceneExporter != null)
                {
                    integrationManager.sceneExporter.serverConfig = serverConfig;
                    Debug.Log("Updated scene exporter server config");
                }
                
                // Clear the activity dropdown to show we're refreshing
                if (activitySelector != null)
                {
                    activitySelector.ClearOptions();
                    activitySelector.options.Add(new TMP_Dropdown.OptionData("Loading..."));
                    activitySelector.RefreshShownValue();
                    Debug.Log("Reset activity selector dropdown to Loading state");
                }
                
                // Start a coroutine to directly fetch activities from the API
                StartCoroutine(FetchActivitiesDirectly());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error when refreshing activities: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Integration manager is null, cannot refresh activities");
        }
    }
    
    // Update the activity dropdown with fetched activities
    private void UpdateActivityDropdown(List<ActivityData> activities)
    {
        if (activitySelector != null)
        {
            activitySelector.ClearOptions();
            
            if (activities != null && activities.Count > 0)
            {
                Debug.Log($"Updating activity dropdown with {activities.Count} activities");
                
                // Add options for each activity
                List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
                foreach (var activity in activities)
                {
                    options.Add(new TMP_Dropdown.OptionData(activity.title));
                    Debug.Log($"Added activity to dropdown: {activity.title} (ID: {activity._id})");
                }
                
                activitySelector.AddOptions(options);
                activitySelector.value = 0;
                activitySelector.RefreshShownValue();
                Debug.Log("Updated activity dropdown with new options");
                
                // Store the activities in the integration manager (this is critical)
                var fieldInfo = integrationManager.GetType().GetField(
                    "availableActivities", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public
                );
                
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(integrationManager, activities);
                    Debug.Log("Updated availableActivities in the integration manager");
                }
                
                // Update the integration manager's dropdown too
                if (integrationManager != null)
                {
                    // Force refresh UI
                    integrationManager.activitySelector = null;
                    integrationManager.activitySelector = activitySelector;
                    Debug.Log("Reassigned activity selector to integration manager");
                    
                    // Directly trigger the OnActivitySelected method to load the first activity
                    var method = integrationManager.GetType().GetMethod(
                        "OnActivitySelected",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public
                    );
                    
                    if (method != null)
                    {
                        Debug.Log("Calling OnActivitySelected to load first activity");
                        TriggerActivitySelection(0);
                    }
                    else
                    {
                        // Use the public method directly
                        Debug.Log("Using LoadActivity method directly");
                        
                        // Clear dropdown first to ensure UI updates
                        if (sceneSelector != null)
                        {
                            sceneSelector.ClearOptions();
                            sceneSelector.options.Add(new TMP_Dropdown.OptionData("Loading scenes..."));
                            sceneSelector.RefreshShownValue();
                        }
                        
                        // Load first activity
                        integrationManager.LoadActivity(activities[0]._id);
                    }
                }
            }
            else
            {
                // No activities found
                activitySelector.options.Add(new TMP_Dropdown.OptionData("No activities found"));
                activitySelector.RefreshShownValue();
                
                // Also clear scene selector
                if (sceneSelector != null)
                {
                    sceneSelector.ClearOptions();
                    sceneSelector.options.Add(new TMP_Dropdown.OptionData("No scenes available"));
                    sceneSelector.RefreshShownValue();
                }
                
                Debug.Log("No activities found on server");
                
                // Show message to user
                StartCoroutine(ShowTemporaryMessage("No activities found on server"));
            }
        }
        else
        {
            Debug.LogError("Activity selector is null, cannot update dropdown");
        }
    }
    
    // Show a temporary message to the user
    private IEnumerator ShowTemporaryMessage(string message)
    {
        // Find status text component
        TextMeshProUGUI statusText = null;
        Transform statusLabel = mainCanvas?.transform.Find("ControlPanel/StatusLabel");
        
        if (statusLabel != null)
        {
            statusText = statusLabel.GetComponent<TextMeshProUGUI>();
        }
        
        if (statusText != null)
        {
            // Store original message
            string originalText = statusText.text;
            Color originalColor = statusText.color;
            
            // Show warning
            statusText.text = message;
            statusText.color = Color.yellow;
            
            // Wait 3 seconds
            yield return new WaitForSeconds(3f);
            
            // Restore original
            statusText.text = originalText;
            statusText.color = originalColor;
        }
    }
    
    // Directly fetch activities from the API and update the dropdown
    private IEnumerator FetchActivitiesDirectly()
    {
        Debug.Log("Directly fetching activities from the API");
        
        if (activitySelector != null)
        {
            activitySelector.interactable = false;
        }
        
        if (sceneSelector != null)
        {
            sceneSelector.interactable = false;
        }
        
        // Clear dropdowns and show loading state
        if (activitySelector != null)
        {
            activitySelector.ClearOptions();
            activitySelector.options.Add(new TMP_Dropdown.OptionData("Loading..."));
            activitySelector.RefreshShownValue();
        }
        
        if (sceneSelector != null)
        {
            sceneSelector.ClearOptions();
            sceneSelector.options.Add(new TMP_Dropdown.OptionData("Waiting..."));
            sceneSelector.RefreshShownValue();
        }
        
        // Find status text component and update it
        TextMeshProUGUI statusText = null;
        Transform statusLabel = mainCanvas?.transform.Find("ControlPanel/StatusLabel");
        
        if (statusLabel != null)
        {
            statusText = statusLabel.GetComponent<TextMeshProUGUI>();
            if (statusText != null)
            {
                statusText.text = "Connecting to server...";
                statusText.color = Color.yellow;
            }
        }
        
        // Use the API client directly to get activities
        if (integrationManager?.apiClient != null)
        {
            // Show loading screen if available
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowLoadingScreen("Connecting to server...");
            }
            
            Debug.Log($"Fetching activities from {serverConfig.apiBaseUrl}");
            
            // Use the GetActivities coroutine directly
            yield return integrationManager.apiClient.GetActivities((activities) => {
                Debug.Log($"Fetched {(activities != null ? activities.Count : 0)} activities from server");
                
                // Hide loading screen
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.HideLoadingScreen();
                }
                
                // Update activity dropdown directly
                UpdateActivityDropdown(activities);
                
                // Reset status text
                if (statusText != null)
                {
                    statusText.text = "Portalt Scene Editor";
                    statusText.color = Color.white;
                }
                
                // Re-enable dropdowns
                if (activitySelector != null)
                {
                    activitySelector.interactable = true;
                }
                
                if (sceneSelector != null)
                {
                    sceneSelector.interactable = true;
                }
            });
        }
        else
        {
            Debug.LogError("API Client is null, cannot fetch activities");
            
            // Hide loading screen
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.HideLoadingScreen();
            }
            
            // Update dropdown to show error
            if (activitySelector != null)
            {
                activitySelector.ClearOptions();
                activitySelector.options.Add(new TMP_Dropdown.OptionData("Error connecting to server"));
                activitySelector.RefreshShownValue();
                activitySelector.interactable = true;
            }
            
            if (sceneSelector != null)
            {
                sceneSelector.ClearOptions();
                sceneSelector.options.Add(new TMP_Dropdown.OptionData("No scenes available"));
                sceneSelector.RefreshShownValue();
                sceneSelector.interactable = true;
            }
            
            // Update status text
            if (statusText != null)
            {
                statusText.text = "Connection failed";
                statusText.color = Color.red;
                
                // Reset after a delay
                StartCoroutine(ShowTemporaryMessage("Connection failed"));
            }
        }
    }
    
    // This needs to be outside any #if UNITY_EDITOR blocks so it's included in builds
    private void Start()
    {
        Debug.Log("EditorUISetup Start method called");
        
        // Make sure we have the config panel reference
        if (configPanel == null)
        {
            configPanel = mainCanvas?.transform.Find("ConfigPanel")?.gameObject;
            if (configPanel == null)
            {
                Debug.LogError("Could not find ConfigPanel in hierarchy!");
            }
            else
            {
                Debug.Log("Found ConfigPanel in hierarchy");
                // Make sure it's initially hidden
                configPanel.SetActive(false);
                configPanelVisible = false;
            }
        }
        
        // Make sure toggle button has onClick listener
        if (toggleConfigButton != null)
        {
            // Remove any existing listeners to avoid duplicates
            toggleConfigButton.onClick.RemoveAllListeners();
            
            // Connect using an anonymous method to avoid direct method reference
            // This is more reliable in builds
            toggleConfigButton.onClick.AddListener(() => {
                if (configPanelVisible && configPanel != null)
                {
                    configPanel.SetActive(false);
                    configPanelVisible = false;
                    Debug.Log("Config panel hidden");
                }
                else
                {
                    ShowConfigurationPanel();
                }
            });
            
            Debug.Log("Set up config toggle button onClick listener");
        }
        else
        {
            Debug.LogError("No reference to toggle config button!");
        }
        
        // Make sure save button has onClick listener
        if (saveConfigButton != null)
        {
            // Remove any existing listeners to avoid duplicates
            saveConfigButton.onClick.RemoveAllListeners();
            
            #if UNITY_EDITOR
            // Connect using an anonymous method instead of direct reference
            saveConfigButton.onClick.AddListener(() => {
                SaveConfiguration();
            });
            #else
            // Connect using an anonymous method instead of direct reference
            saveConfigButton.onClick.AddListener(() => {
                SaveConfigurationRuntime();
            });
            #endif
            
            Debug.Log("Set up save config button onClick listener");
        }
        else
        {
            Debug.LogError("No reference to save config button!");
        }
        
        // Set up instructions toggle button
        if (toggleInstructionsButton != null)
        {
            // Remove any existing listeners to avoid duplicates
            toggleInstructionsButton.onClick.RemoveAllListeners();
            
            // Connect the listener
            toggleInstructionsButton.onClick.AddListener(() => {
                ToggleInstructionsPanel();
            });
            
            Debug.Log("Set up instructions toggle button onClick listener");
        }
        else
        {
            Debug.LogWarning("No reference to toggle instructions button!");
            
            // Try to find it
            toggleInstructionsButton = mainCanvas?.transform.Find("ControlPanel/ToggleInstructionsButton")?.GetComponent<Button>();
            
            if (toggleInstructionsButton != null)
            {
                toggleInstructionsButton.onClick.RemoveAllListeners();
                toggleInstructionsButton.onClick.AddListener(() => {
                    ToggleInstructionsPanel();
                });
                Debug.Log("Found and connected instructions toggle button");
            }
        }
    }
    
    // Public method that can be called from the inspector to show configuration panel
    public void ShowConfigurationPanel()
    {
        Debug.Log("Manually showing configuration panel");
        
        // Try to find config panel if it's missing
        if (configPanel == null)
        {
            // Look for it in children of mainCanvas
            if (mainCanvas != null)
            {
                configPanel = mainCanvas.transform.Find("ConfigPanel")?.gameObject;
                
                if (configPanel == null)
                {
                    Debug.LogError("Could not find ConfigPanel in hierarchy - attempting to recreate it");
                    
                    // Try to recreate the panel
                    #if UNITY_EDITOR
                    // In editor, use the editor-specific method if available
                    if (buttonPrefab != null && inputFieldPrefab != null)
                    {
                        CreateConfigPanel();
                    }
                    #else
                    // In a build, use the runtime-compatible method
                    CreateConfigPanelRuntime();
                    #endif
                    
                    if (configPanel == null)
                    {
                        Debug.LogError("Failed to create config panel");
                        return;
                    }
                }
            }
            else
            {
                Debug.LogError("Main canvas is null, cannot find config panel");
                return;
            }
        }
        
        // Validate input fields
        ValidateConfigPanelInputs();
        
        // Make sure we have a valid reference for save button
        if (saveConfigButton == null)
        {
            saveConfigButton = configPanel.transform.Find("SaveConfigButton")?.GetComponent<Button>();
            
            if (saveConfigButton != null)
            {
                // Set up click handler
                saveConfigButton.onClick.RemoveAllListeners();
                #if UNITY_EDITOR
                saveConfigButton.onClick.AddListener(() => {
                    SaveConfiguration();
                });
                #else
                saveConfigButton.onClick.AddListener(() => {
                    SaveConfigurationRuntime();
                });
                #endif
            }
            else
            {
                Debug.LogError("Could not find save config button");
            }
        }
        
        // Update values from config
        if (serverConfig != null && pairingCodeInput != null)
        {
            pairingCodeInput.text = serverConfig.pairingCode;
            Debug.Log("Updated input field with current config value");
        }
        else
        {
            Debug.LogError("Server config or input field is null");
        }
        
        // Show the panel
        configPanel.SetActive(true);
        configPanelVisible = true;
        Debug.Log("Configuration panel is now visible");
    }
    
    // Public method to manually refresh activities from inspector or other scripts
    [ContextMenu("Refresh Activities List")]
    public void ManualRefreshActivities()
    {
        Debug.Log("Manual refresh of activities list requested");
        
        if (serverConfig == null)
        {
            Debug.LogError("Server config is null, cannot refresh activities");
            return;
        }
        
        RefreshActivitiesList();
    }
    
    // Helper method for OnActivitySelected callback
    private void TriggerActivitySelection(int index)
    {
        if (integrationManager != null && index >= 0)
        {
            var method = integrationManager.GetType().GetMethod(
                "OnActivitySelected",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public
            );
            
            if (method != null)
            {
                Debug.Log("Calling OnActivitySelected to load first activity");
                method.Invoke(integrationManager, new object[] { index });
            }
            else if (integrationManager != null && activitySelector != null)
            {
                // Try to find the availableActivities field to get the ID
                var fieldInfo = integrationManager.GetType().GetField(
                    "availableActivities", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public
                );
                
                if (fieldInfo != null)
                {
                    var activities = fieldInfo.GetValue(integrationManager) as List<ActivityData>;
                    if (activities != null && activities.Count > index)
                    {
                        // Load the activity directly
                        Debug.Log($"Loading activity with ID: {activities[index]._id}");
                        integrationManager.LoadActivity(activities[index]._id);
                    }
                }
            }
        }
    }
    
    // Coroutine to handle refreshing with a short delay
    private IEnumerator DelayedRefresh()
    {
        // Wait a short time for everything to settle
        yield return new WaitForSeconds(0.2f);
        
        try
        {
            // Use reflection to call the private LoadActivitiesList method
            System.Reflection.MethodInfo method = integrationManager.GetType().GetMethod(
                "LoadActivitiesList", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public
            );
            
            if (method != null)
            {
                Debug.Log("Calling LoadActivitiesList method");
                method.Invoke(integrationManager, null);
                Debug.Log("Activities refresh initiated via reflection");
            }
            else
            {
                // Fallback approach if we can't find the method
                Debug.Log("Using fallback approach to refresh activities");
                
                // Clear the dropdown and force the integration manager to reload
                if (activitySelector != null)
                {
                    activitySelector.ClearOptions();
                    activitySelector.options.Add(new TMP_Dropdown.OptionData("Loading..."));
                    activitySelector.RefreshShownValue();
                    Debug.Log("Reset activity selector dropdown");
                }
                
                // Check if the integration manager has a public method we can call
                System.Reflection.MethodInfo publicMethod = integrationManager.GetType().GetMethod("LoadActivity");
                if (publicMethod != null)
                {
                    Debug.Log("Found LoadActivity public method");
                    // This method would likely need an activityId parameter, so we can't easily call it directly
                }
                
                // Force a re-assignment of the dropdown which can trigger a refresh
                if (integrationManager.activitySelector != null)
                {
                    // Store references before exiting try block
                    var currentSelector = integrationManager.activitySelector;
                    
                    // Temporarily null out the selector
                    integrationManager.activitySelector = null;
                    
                    // Schedule the reassignment with proper error handling
                    StartCoroutine(ReassignSelector(currentSelector));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in delayed refresh: {e.Message}");
        }
    }
    
    // Separate coroutine to handle the selector reassignment
    private IEnumerator ReassignSelector(TMP_Dropdown selector)
    {
        yield return null; // Wait a frame
        
        try
        {
            if (integrationManager != null && selector != null)
            {
                integrationManager.activitySelector = selector;
                Debug.Log("Reassigned activity selector to force refresh");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reassigning selector: {e.Message}");
        }
    }
    
    // Runtime-compatible CreateConfigPanel method
    private void CreateConfigPanelRuntime()
    {
        // Destroy any existing config panel
        if (configPanel != null)
        {
            Destroy(configPanel);
            configPanel = null;
        }

        // Create a panel for server configuration
        GameObject configPanelObj = new GameObject("ConfigPanel");
        configPanelObj.transform.SetParent(mainCanvas.transform, false);
        
        // Add Image component for visual
        Image configPanelImage = configPanelObj.AddComponent<Image>();
        configPanelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        
        // Configure the panel's RectTransform - make it smaller now that we only have one field
        RectTransform configPanelRect = configPanelObj.GetComponent<RectTransform>();
        configPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        configPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        configPanelRect.pivot = new Vector2(0.5f, 0.5f);
        configPanelRect.sizeDelta = new Vector2(400, 160); // Reduced height since we now only have one input field
        configPanelRect.anchoredPosition = Vector2.zero;
        
        configPanel = configPanelObj;
        Debug.Log($"Created config panel: {configPanel.name}");
        
        // Create title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(configPanelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 40);
        titleRect.anchoredPosition = new Vector2(0, 0);
        
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Cloud Connection";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        
        // Create Pairing Code Input (only input field now)
        CreateConfigInputRuntime("PairingCode", "Pairing Code:", 0, out pairingCodeInput);
        
        // Create Save Config Button
        GameObject saveConfigObj = new GameObject("SaveConfigButton");
        saveConfigObj.AddComponent<RectTransform>();
        saveConfigObj.transform.SetParent(configPanelObj.transform, false);
        
        // Try to use the button prefab if available
        if (buttonPrefab != null)
        {
            // Destroy the GameObject we just created
            Destroy(saveConfigObj);
            
            // Instantiate the prefab instead
            saveConfigObj = Instantiate(buttonPrefab.gameObject, configPanelObj.transform);
        }
        else
        {
            // Add required components for a basic button
            Image saveConfigImage = saveConfigObj.AddComponent<Image>();
            saveConfigImage.color = new Color(0.2f, 0.6f, 0.2f, 1.0f); // Green button
            saveConfigButton = saveConfigObj.AddComponent<Button>();
            saveConfigButton.targetGraphic = saveConfigImage;
            
            // Add text
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(saveConfigObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI buttonText = textObj.GetComponent<TextMeshProUGUI>();
            buttonText.text = "Connect";
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 14;
        }
        
        saveConfigObj.name = "SaveConfigButton";
        saveConfigButton = saveConfigObj.GetComponent<Button>();
        
        RectTransform saveConfigRect = saveConfigObj.GetComponent<RectTransform>();
        saveConfigRect.anchorMin = new Vector2(0.5f, 0);
        saveConfigRect.anchorMax = new Vector2(0.5f, 0);
        saveConfigRect.pivot = new Vector2(0.5f, 0);
        saveConfigRect.sizeDelta = new Vector2(150, 40);
        saveConfigRect.anchoredPosition = new Vector2(0, 20);
        
        TextMeshProUGUI saveConfigText = saveConfigObj.GetComponentInChildren<TextMeshProUGUI>();
        if (saveConfigText != null)
        {
            saveConfigText.text = "Connect";
        }
        
        // Add onClick listener for save button
        saveConfigButton.onClick.RemoveAllListeners();
        saveConfigButton.onClick.AddListener(() => {
            SaveConfigurationRuntime();
        });
        Debug.Log("Added save button listener");
        
        // Load current values
        if (serverConfig != null)
        {
            pairingCodeInput.text = serverConfig.pairingCode;
            Debug.Log("Loaded initial pairing code from server config");
        }
        else
        {
            Debug.LogError("ServerConfig is null when creating config panel!");
        }
        
        // Hide panel by default
        configPanel.SetActive(false);
        configPanelVisible = false;
    }
    
    // Runtime-compatible version of CreateConfigInput
    private void CreateConfigInputRuntime(string name, string label, int position, out TMP_InputField inputField)
    {
        // Create container
        GameObject container = new GameObject(name + "Container", typeof(RectTransform));
        container.transform.SetParent(configPanel.transform, false);
        
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.sizeDelta = new Vector2(0, 50);
        containerRect.anchoredPosition = new Vector2(0, -50 - (position * 50));
        
        // Create label
        GameObject labelObj = new GameObject(name + "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(container.transform, false);
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0.3f, 0.5f);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.sizeDelta = new Vector2(0, 30);
        labelRect.anchoredPosition = new Vector2(10, 0);
        
        TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.fontSize = 14;
        
        // Create input field - the tricky part
        GameObject inputObj = null;
        
        if (inputFieldPrefab != null)
        {
            // If we have a prefab, use it
            inputObj = Instantiate(inputFieldPrefab.gameObject, container.transform);
        }
        else
        {
            // Otherwise create a basic input field from scratch
            inputObj = new GameObject(name + "Input", typeof(RectTransform));
            inputObj.transform.SetParent(container.transform, false);
            
            // Add required components
            Image backgroundImage = inputObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.9f, 0.9f, 0.9f, 1.0f);
            
            // Create text area
            GameObject textArea = new GameObject("TextArea", typeof(RectTransform));
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(5, 2);
            textAreaRect.offsetMax = new Vector2(-5, -2);
            
            // Create text component for input
            GameObject textComponent = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textComponent.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textComponent.GetComponent<TextMeshProUGUI>();
            text.color = Color.black;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            
            // Create placeholder
            GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholder.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI placeholderText = placeholder.GetComponent<TextMeshProUGUI>();
            placeholderText.text = "Enter " + label.TrimEnd(':');
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderText.fontSize = 14;
            placeholderText.alignment = TextAlignmentOptions.Left;
            
            // Add the input field component and configure it
            TMP_InputField inputFieldComponent = inputObj.AddComponent<TMP_InputField>();
            inputFieldComponent.textComponent = text;
            inputFieldComponent.placeholder = placeholderText;
            inputFieldComponent.targetGraphic = backgroundImage;
        }
        
        inputObj.name = name + "Input";
        inputField = inputObj.GetComponent<TMP_InputField>();
        
        RectTransform inputRect = inputField.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.3f, 0.5f);
        inputRect.anchorMax = new Vector2(1f, 0.5f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(-20, 30);
        inputRect.anchoredPosition = new Vector2(0, 0);
    }
    
    // Make sure we have valid references for input fields
    private void ValidateConfigPanelInputs()
    {
        if (pairingCodeInput == null)
        {
            // Try to find it
            pairingCodeInput = configPanel.transform.Find("PairingCodeContainer/PairingCodeInput")?.GetComponent<TMP_InputField>();
            
            if (pairingCodeInput == null)
            {
                Debug.LogError("Could not find pairing code input field in the config panel");
            }
        }
    }
    
    private void CreateInstructionsPanel()
    {
        // Destroy any existing instructions panel
        if (instructionsPanel != null)
        {
            DestroyImmediate(instructionsPanel);
            instructionsPanel = null;
        }
        
        // Create a panel for instructions
        GameObject instructionsPanelObj = new GameObject("InstructionsPanel");
        instructionsPanelObj.transform.SetParent(mainCanvas.transform, false);
        
        // Add Image component for visual
        Image instructionsPanelImage = instructionsPanelObj.AddComponent<Image>();
        instructionsPanelImage.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        
        // Configure the panel's RectTransform
        RectTransform instructionsPanelRect = instructionsPanelObj.GetComponent<RectTransform>();
        instructionsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        instructionsPanelRect.pivot = new Vector2(0.5f, 0.5f);
        instructionsPanelRect.sizeDelta = new Vector2(600, 500);
        instructionsPanelRect.anchoredPosition = Vector2.zero;
        
        instructionsPanel = instructionsPanelObj;
        
        // Create title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(instructionsPanelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 40); // Slightly smaller title
        titleRect.anchoredPosition = new Vector2(0, 0);
        
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Instructions & Controls";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        
        // Create scrollable content area with improved margins
        GameObject scrollViewObj = new GameObject("ScrollView", typeof(RectTransform));
        scrollViewObj.transform.SetParent(instructionsPanelObj.transform, false);
        RectTransform scrollViewRect = scrollViewObj.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0, 0);
        scrollViewRect.anchorMax = new Vector2(1, 1);
        scrollViewRect.offsetMin = new Vector2(10, 10); // Bottom margin
        scrollViewRect.offsetMax = new Vector2(-10, -45); // Top margin (below title)
        
        // Add the scroll rect component
        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.name = "InstructionsScrollRect"; // Named for easier finding
        
        // Create the content container with increased height and proper padding
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(-20, 1500); // More height to ensure all content fits
        contentRect.anchoredPosition = new Vector2(0, 0);
        
        // Set scroll rect properties
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 10;
        scrollRect.viewport = scrollViewRect;
        scrollRect.movementType = ScrollRect.MovementType.Clamped; // Prevent overscrolling
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalNormalizedPosition = 1.0f; // Start at the top
        
        // Add mask with proper size
        Image scrollViewImage = scrollViewObj.AddComponent<Image>();
        scrollViewImage.color = new Color(0.1f, 0.1f, 0.1f, 0.1f);
        Mask mask = scrollViewObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Add vertical scrollbar
        GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(15, 0);
        scrollbarRect.anchoredPosition = new Vector2(15, 0);
        
        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollRect.verticalScrollbar = scrollbar;
        
        // Add scrollbar handle
        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(scrollbarObj.transform, false);
        Image handleImage = handleObj.GetComponent<Image>();
        handleImage.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(1, 1);
        handleRect.sizeDelta = Vector2.zero;
        
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        
        // Add the instructions text with improved padding
        GameObject instructionsTextObj = new GameObject("InstructionsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        instructionsTextObj.transform.SetParent(contentObj.transform, false);
        RectTransform instructionsTextRect = instructionsTextObj.GetComponent<RectTransform>();
        instructionsTextRect.anchorMin = Vector2.zero;
        instructionsTextRect.anchorMax = Vector2.one;
        instructionsTextRect.offsetMin = new Vector2(10, 40); // More bottom padding
        instructionsTextRect.offsetMax = new Vector2(-10, -70); // Much more top padding to prevent cutoff
        
        TextMeshProUGUI instructionsText = instructionsTextObj.GetComponent<TextMeshProUGUI>();
        instructionsText.fontSize = 17;
        instructionsText.alignment = TextAlignmentOptions.Left;
        instructionsText.color = Color.white;
        instructionsText.enableWordWrapping = true;
        instructionsText.margin = new Vector4(0, 20, 0, 10); // More top margin in the text
        
        // Set the instructions content with updated key information
        instructionsText.text = 
@"<b><size=20>Movement Controls</size></b>

• <b>W</b> - Move forward
• <b>S</b> - Move backward
• <b>A</b> - Strafe left
• <b>D</b> - Strafe right
• <b>Q</b> - Move down
• <b>E</b> - Move up
• <b>Mouse</b> - Look around

<b><size=20>Selection & Interaction</size></b>

• <b>Left Click</b> - Select/deselect objects
• <b>Tab</b> - Toggle between UI mode and Tractor Beam mode
• <b>V</b> - Toggle collider visualization (helps to see clickable areas)
• <b>Shift</b> - When object is selected, cycle between modification modes (position, scale, rotation)
• <b>R</b> - When rotating an object, cycle between rotation axes
• <b>Mouse Scroll Wheel</b> - Move objects closer/farther in position mode, scale up/down in scale mode, or rotate clockwise/counterclockwise in rotation mode

<b><size=20>UI Elements</size></b>

• <b>Activity Selector</b> - Choose which activity to load
• <b>Scene Selector</b> - Choose which scene from the current activity to load
• <b>Save Button</b> - Save changes to the current scene
• <b>Config Button</b> - Configure the pairing code
• <b>Help Button</b> - Show/hide this help screen

<b><size=20>Configuration</size></b>

• <b>Pairing Code</b> - Connect to the cloud admin dashboard
• <b>Connect Button</b> - Apply configuration settings and connect

<b><size=20>Tips & Tricks</size></b>

• When in Tractor Beam mode, selected objects can be moved using the mouse.
• Use the Shift key to cycle between Position, Scale, and Rotation modes for selected objects.
• When in Rotation mode, use the R key to switch between X, Y, and Z rotation axes.
• Use the V key to toggle visualization of colliders for easier selection.
• For complex 3D models, colliders are automatically simplified to improve performance.
• UI mode lets you interact with dropdowns and buttons, while Tractor Beam mode allows object manipulation.
• All scenes and objects are stored in the cloud and can be accessed by multiple users.
• Changes are synchronized between users when saved.

<b><size=20>Troubleshooting</size></b>

• If objects aren't selectable, check that you're in Tractor Beam mode (press Tab).
• If you can't see any activities in the dropdown, verify your connection settings.
• For very large models, collider visualization might take a moment to generate.
• If you encounter any errors loading a scene, check your connection and try again.
• The collider visualization feature was added because colliders are required for object selection, and some objects don't come with built-in colliders.
• Colliders are added manually during the import process, and there's no way to ensure they perfectly fit the entire object.
• Highlighting these colliders helps you know exactly where to click for object selection, which is especially useful for large objects with complex shapes.






";
        
        // Add a helper spacer at the top to prevent cutoff
        GameObject topSpacer = new GameObject("TopSpacer", typeof(RectTransform));
        topSpacer.transform.SetParent(contentObj.transform, false);
        RectTransform topSpacerRect = topSpacer.GetComponent<RectTransform>();
        topSpacerRect.anchorMin = new Vector2(0, 1);
        topSpacerRect.anchorMax = new Vector2(1, 1);
        topSpacerRect.pivot = new Vector2(0.5f, 1);
        topSpacerRect.sizeDelta = new Vector2(0, 40);
        topSpacerRect.anchoredPosition = new Vector2(0, 0);
        
        // Hide panel by default
        instructionsPanel.SetActive(false);
        instructionsPanelVisible = false;
    }
    
    public void ToggleInstructionsPanel()
    {
        if (instructionsPanel == null)
        {
            // Try to find it or create it
            instructionsPanel = mainCanvas?.transform.Find("InstructionsPanel")?.gameObject;
            
            if (instructionsPanel == null)
            {
                #if UNITY_EDITOR
                CreateInstructionsPanel();
                #else
                CreateInstructionsPanelRuntime();
                #endif
            }
        }
        
        // Toggle visibility
        instructionsPanelVisible = !instructionsPanelVisible;
        instructionsPanel.SetActive(instructionsPanelVisible);
        
        // If we're showing the panel, make sure we reset the scroll position to top
        if (instructionsPanelVisible)
        {
            ResetInstructionsScrollPosition();
        }
        
        Debug.Log($"Instructions panel is now {(instructionsPanelVisible ? "visible" : "hidden")}");
    }
    
    // Reset scroll position to top when showing instructions
    private void ResetInstructionsScrollPosition()
    {
        if (instructionsPanel != null)
        {
            // Find the ScrollRect component
            ScrollRect scrollRect = instructionsPanel.GetComponentInChildren<ScrollRect>();
            if (scrollRect != null)
            {
                // Set scroll position to top (1.0f is top, 0.0f is bottom)
                scrollRect.normalizedPosition = new Vector2(0, 1);
                Debug.Log("Reset instructions scroll position to top");
            }
        }
    }
    
    // Runtime-compatible create instructions panel
    private void CreateInstructionsPanelRuntime()
    {
        // Implementation similar to CreateInstructionsPanel but runtime compatible
        
        // Destroy any existing instructions panel
        if (instructionsPanel != null)
        {
            Destroy(instructionsPanel);
            instructionsPanel = null;
        }
        
        // Create a panel for instructions
        GameObject instructionsPanelObj = new GameObject("InstructionsPanel");
        instructionsPanelObj.transform.SetParent(mainCanvas.transform, false);
        
        // Add Image component for visual
        Image instructionsPanelImage = instructionsPanelObj.AddComponent<Image>();
        instructionsPanelImage.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        
        // Configure the panel's RectTransform
        RectTransform instructionsPanelRect = instructionsPanelObj.GetComponent<RectTransform>();
        instructionsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        instructionsPanelRect.pivot = new Vector2(0.5f, 0.5f);
        instructionsPanelRect.sizeDelta = new Vector2(600, 500);
        instructionsPanelRect.anchoredPosition = Vector2.zero;
        
        instructionsPanel = instructionsPanelObj;
        
        // Create title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(instructionsPanelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 40); // Slightly smaller title
        titleRect.anchoredPosition = new Vector2(0, 0);
        
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Instructions & Controls";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        
        // Create scrollable content area with improved margins
        GameObject scrollViewObj = new GameObject("ScrollView", typeof(RectTransform));
        scrollViewObj.transform.SetParent(instructionsPanelObj.transform, false);
        RectTransform scrollViewRect = scrollViewObj.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0, 0);
        scrollViewRect.anchorMax = new Vector2(1, 1);
        scrollViewRect.offsetMin = new Vector2(10, 10); // Bottom margin
        scrollViewRect.offsetMax = new Vector2(-10, -45); // Top margin (below title)
        
        // Add the scroll rect component
        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.name = "InstructionsScrollRect"; // Named for easier finding
        
        // Create the content container with increased height and proper padding
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(-20, 1500); // More height to ensure all content fits
        contentRect.anchoredPosition = new Vector2(0, 0);
        
        // Set scroll rect properties
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 10;
        scrollRect.viewport = scrollViewRect;
        scrollRect.movementType = ScrollRect.MovementType.Clamped; // Prevent overscrolling
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalNormalizedPosition = 1.0f; // Start at the top
        
        // Add mask with proper size
        Image scrollViewImage = scrollViewObj.AddComponent<Image>();
        scrollViewImage.color = new Color(0.1f, 0.1f, 0.1f, 0.1f);
        Mask mask = scrollViewObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Add vertical scrollbar
        GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(15, 0);
        scrollbarRect.anchoredPosition = new Vector2(15, 0);
        
        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollRect.verticalScrollbar = scrollbar;
        
        // Add scrollbar handle
        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(scrollbarObj.transform, false);
        Image handleImage = handleObj.GetComponent<Image>();
        handleImage.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(1, 1);
        handleRect.sizeDelta = Vector2.zero;
        
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        
        // Add the instructions text with improved padding
        GameObject instructionsTextObj = new GameObject("InstructionsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        instructionsTextObj.transform.SetParent(contentObj.transform, false);
        RectTransform instructionsTextRect = instructionsTextObj.GetComponent<RectTransform>();
        instructionsTextRect.anchorMin = Vector2.zero;
        instructionsTextRect.anchorMax = Vector2.one;
        instructionsTextRect.offsetMin = new Vector2(10, 40); // More bottom padding
        instructionsTextRect.offsetMax = new Vector2(-10, -70); // Much more top padding to prevent cutoff
        
        TextMeshProUGUI instructionsText = instructionsTextObj.GetComponent<TextMeshProUGUI>();
        instructionsText.fontSize = 17;
        instructionsText.alignment = TextAlignmentOptions.Left;
        instructionsText.color = Color.white;
        instructionsText.enableWordWrapping = true;
        instructionsText.margin = new Vector4(0, 20, 0, 10); // More top margin in the text
        
        // Add a helper spacer at the top to prevent cutoff
        GameObject topSpacer = new GameObject("TopSpacer", typeof(RectTransform));
        topSpacer.transform.SetParent(contentObj.transform, false);
        RectTransform topSpacerRect = topSpacer.GetComponent<RectTransform>();
        topSpacerRect.anchorMin = new Vector2(0, 1);
        topSpacerRect.anchorMax = new Vector2(1, 1);
        topSpacerRect.pivot = new Vector2(0.5f, 1);
        topSpacerRect.sizeDelta = new Vector2(0, 40);
        topSpacerRect.anchoredPosition = new Vector2(0, 0);
        
        // Set the instructions content with updated key information
        instructionsText.text = 
@"<b><size=20>Movement Controls</size></b>

• <b>W</b> - Move forward
• <b>S</b> - Move backward
• <b>A</b> - Strafe left
• <b>D</b> - Strafe right
• <b>Q</b> - Move down
• <b>E</b> - Move up
• <b>Mouse</b> - Look around

<b><size=20>Selection & Interaction</size></b>

• <b>Left Click</b> - Select/deselect objects
• <b>Tab</b> - Toggle between UI mode and Tractor Beam mode
• <b>V</b> - Toggle collider visualization (helps to see clickable areas)
• <b>Shift</b> - When object is selected, cycle between modification modes (position, scale, rotation)
• <b>R</b> - When rotating an object, cycle between rotation axes
• <b>Mouse Scroll Wheel</b> - Move objects closer/farther in position mode, scale up/down in scale mode, or rotate clockwise/counterclockwise in rotation mode

<b><size=20>UI Elements</size></b>

• <b>Activity Selector</b> - Choose which activity to load
• <b>Scene Selector</b> - Choose which scene from the current activity to load
• <b>Save Button</b> - Save changes to the current scene
• <b>Config Button</b> - Configure the pairing code
• <b>Help Button</b> - Show/hide this help screen

<b><size=20>Configuration</size></b>

• <b>Pairing Code</b> - Connect to the cloud admin dashboard
• <b>Connect Button</b> - Apply configuration settings and connect

<b><size=20>Tips & Tricks</size></b>

• When in Tractor Beam mode, selected objects can be moved using the mouse.
• Use the Shift key to cycle between Position, Scale, and Rotation modes for selected objects.
• When in Rotation mode, use the R key to switch between X, Y, and Z rotation axes.
• Use the V key to toggle visualization of colliders for easier selection.
• For complex 3D models, colliders are automatically simplified to improve performance.
• UI mode lets you interact with dropdowns and buttons, while Tractor Beam mode allows object manipulation.
• All scenes and objects are stored in the cloud and can be accessed by multiple users.
• Changes are synchronized between users when saved.

<b><size=20>Troubleshooting</size></b>

• If objects aren't selectable, check that you're in Tractor Beam mode (press Tab).
• If you can't see any activities in the dropdown, verify your connection settings.
• For very large models, collider visualization might take a moment to generate.
• If you encounter any errors loading a scene, check your connection and try again.
• The collider visualization feature was added because colliders are required for object selection, and some objects don't come with built-in colliders.
• Colliders are added manually during the import process, and there's no way to ensure they perfectly fit the entire object.
• Highlighting these colliders helps you know exactly where to click for object selection, which is especially useful for large objects with complex shapes.






";
        
        // Hide panel by default
        instructionsPanel.SetActive(false);
        instructionsPanelVisible = false;
    }
} 