using UnityEngine;

[CreateAssetMenu(fileName = "PortaltServerConfig", menuName = "Portalt/Server Configuration")]
public class PortaltServerConfig : ScriptableObject {
    public string serverIp = "localhost";
    public int serverPort = 3000;
    [Tooltip("Organization ID required for all API requests")]
    public string organizationId = "org_2u8PCxxyIQpJBiJGeXwVAg1z6hy"; // Default org ID
    public string apiBaseUrl => $"http://{serverIp}:{serverPort}/api";
    
    public string GetActivityUrl(string activityId) => $"{apiBaseUrl}/activities/{activityId}?orgId={organizationId}";
    public string GetSceneConfigUrl(string sceneId) => $"{apiBaseUrl}/scenes-configuration/{sceneId}?orgId={organizationId}";
    public string GetScenesForActivityUrl(string activityId) => $"{apiBaseUrl}/scenes?activityId={activityId}&orgId={organizationId}";
    
    public string GetActivitiesListUrl() => $"{apiBaseUrl}/activities?orgId={organizationId}";
    public string GetAssetsListUrl() => $"{apiBaseUrl}/assets?orgId={organizationId}";
}
