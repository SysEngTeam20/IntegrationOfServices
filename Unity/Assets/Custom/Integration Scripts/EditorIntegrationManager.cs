using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EditorIntegrationManager : MonoBehaviour
{
    [Header("Server Configuration")]
    public PortaltServerConfig serverConfig;
    
    [Header("Components")]
    public PortaltAPIClient apiClient;
    public PortaltSceneLoader sceneLoader;
    public PortaltSceneExporter sceneExporter;
    public AdminController adminController;
    
    [Header("UI Elements")]
    public TMP_Dropdown activitySelector;
    public TMP_Dropdown sceneSelector;
    public Button saveSceneButton;
    
    // For UI selection
    private List<ActivityData> availableActivities;
    private List<SceneInfo> availableScenes;
    private ActivityData currentActivity;
    private string currentSceneId;
    
    void Start()
    {
        // Ensure all components reference the same server config
        if (apiClient && !apiClient.serverConfig) {
            apiClient.serverConfig = serverConfig;
        }
        
        if (sceneLoader && !sceneLoader.serverConfig) {
            sceneLoader.serverConfig = serverConfig;
        }
        
        if (sceneExporter && !sceneExporter.serverConfig) {
            sceneExporter.serverConfig = serverConfig;
        }
        
        // Set up UI button listeners
        if (saveSceneButton) {
            saveSceneButton.onClick.AddListener(OnSaveSceneClicked);
        }
        
        if (sceneSelector) {
            sceneSelector.onValueChanged.AddListener(OnSceneSelected);
        }
        
        if (activitySelector) {
            activitySelector.onValueChanged.AddListener(OnActivitySelected);
        }
        
        // Load initial activities list
        StartCoroutine(LoadActivitiesList());
    }
    
    // Load activities from API
    private IEnumerator LoadActivitiesList()
    {
        yield return apiClient.GetActivities((activities) => {
            if (activities != null) {
                availableActivities = activities;
                Debug.Log($"Loaded {activities.Count} activities");
                
                // Populate activity dropdown
                UpdateActivitySelector();
                
                // Automatically load first activity if one exists
                if (activities.Count > 0) {
                    LoadActivity(activities[0]._id);
                }
            }
        });
    }
    
    // Load activity and its scenes
    public void LoadActivity(string activityId)
    {
        Debug.Log($"Loading activity with ID: {activityId}");
        StartCoroutine(apiClient.GetActivity(activityId, (activity) => {
            if (activity != null) {
                currentActivity = activity;
                availableScenes = activity.scenes;
                
                Debug.Log($"Loaded activity: {activity.title} with {activity.scenes.Count} scenes");
                
                // Update scene selector dropdown
                UpdateSceneSelector();
                
                // If we have scenes, select the first one
                if (availableScenes.Count > 0) {
                    // Load the first scene
                    Debug.Log($"Loading first scene: {availableScenes[0].name} (ID: {availableScenes[0].id})");
                    LoadScene(availableScenes[0].id);
                    currentSceneId = availableScenes[0].id;
                } else {
                    Debug.LogWarning("No scenes available in this activity");
                }
            } else {
                Debug.LogError($"Failed to load activity with ID: {activityId}");
            }
        }));
    }
    
    // Update the scene selector UI with available scenes
    private void UpdateSceneSelector()
    {
        if (sceneSelector != null && availableScenes != null) {
            // Clear current options
            sceneSelector.ClearOptions();
            
            // Add new options
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var scene in availableScenes) {
                options.Add(new TMP_Dropdown.OptionData(scene.name));
            }
            
            sceneSelector.AddOptions(options);
            sceneSelector.value = 0;
        }
    }
    
    // Handle scene selection from dropdown
    public void OnSceneSelected(int index)
    {
        if (availableScenes != null && index < availableScenes.Count) {
            string sceneId = availableScenes[index].id;
            LoadScene(sceneId);
        }
    }
    
    // Load a specific scene
    public void LoadScene(string sceneId)
    {
        if (!string.IsNullOrEmpty(sceneId)) {
            Debug.Log($"EditorIntegrationManager: Loading scene {sceneId}");
            LoadSceneAsync(sceneId);
            currentSceneId = sceneId;
        }
    }
    
    // Async wrapper for scene loading
    private async void LoadSceneAsync(string sceneId)
    {
        if (sceneLoader != null) {
            // Toggle to UI mode during loading if in tractor beam mode
            bool wasInTractorBeamMode = false;
            if (adminController != null && !adminController.IsInUIMode()) {
                wasInTractorBeamMode = true;
                adminController.ToggleMode(); // Switch to UI Mode
            }
            
            bool success = await sceneLoader.LoadSceneFromApi(sceneId);
            
            if (success) {
                Debug.Log($"Scene {sceneId} loading complete");
                
                // Switch back to tractor beam mode if we were in it before
                if (wasInTractorBeamMode && adminController != null) {
                    adminController.ToggleMode();
                }
            } else {
                Debug.LogError($"Failed to load scene {sceneId}");
            }
        } else {
            Debug.LogError("Scene loader reference is missing");
        }
    }
    
    // Button click handler to save the current scene
    public void OnSaveSceneClicked()
    {
        SaveCurrentScene();
    }
    
    // Save the current scene
    public void SaveCurrentScene()
    {
        if (!string.IsNullOrEmpty(currentSceneId)) {
            Debug.Log($"Saving scene with ID: {currentSceneId}");
            SaveSceneAsync();
        }
        else {
            Debug.LogError("No current scene to save");
        }
    }
    
    // Async wrapper for scene saving
    private async void SaveSceneAsync()
    {
        if (sceneExporter != null) {
            bool success = await sceneExporter.SaveSceneToApi();
            if (success) {
                Debug.Log("Scene saved successfully");
            } else {
                Debug.LogError("Failed to save scene");
            }
        } else {
            Debug.LogError("Scene exporter reference is missing");
        }
    }
    
    // Clean up event listeners
    private void OnDestroy()
    {
        if (saveSceneButton) {
            saveSceneButton.onClick.RemoveListener(OnSaveSceneClicked);
        }
        
        if (sceneSelector) {
            sceneSelector.onValueChanged.RemoveListener(OnSceneSelected);
        }
        
        if (activitySelector) {
            activitySelector.onValueChanged.RemoveListener(OnActivitySelected);
        }
    }
    
    public void OnActivitySelected(int index)
    {
        if (availableActivities != null && index < availableActivities.Count) {
            ActivityData selectedActivity = availableActivities[index];
            // Automatically load the activity
            LoadActivity(selectedActivity._id);
        }
    }
    
    // Update activity dropdown with available activities
    private void UpdateActivitySelector()
    {
        if (activitySelector != null && availableActivities != null) {
            // Clear current options
            activitySelector.ClearOptions();
            
            // Add new options
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var activity in availableActivities) {
                options.Add(new TMP_Dropdown.OptionData(activity.title));
            }
            
            activitySelector.AddOptions(options);
            activitySelector.value = 0;
        }
    }
} 