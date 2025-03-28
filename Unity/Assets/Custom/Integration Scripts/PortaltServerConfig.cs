using UnityEngine;

[CreateAssetMenu(fileName = "PortaltServerConfig", menuName = "Portalt/Server Configuration")]
public class PortaltServerConfig : ScriptableObject {
    public string serverIp = "localhost";
    public int serverPort = 3000;
    [Tooltip("Pairing code for authentication with the Portalt server")]
    public string pairingCode = "";
    public string apiBaseUrl => $"http://{serverIp}:{serverPort}/api";
    
    public string GetActivityUrl(string activityId) => $"{apiBaseUrl}/activities/{activityId}?pairingCode={pairingCode}";
    public string GetSceneConfigUrl(string sceneId) => $"{apiBaseUrl}/scenes-configuration/{sceneId}?pairingCode={pairingCode}";
    public string GetScenesForActivityUrl(string activityId) => $"{apiBaseUrl}/scenes?activityId={activityId}&pairingCode={pairingCode}";
    
    public string GetActivitiesListUrl() => $"{apiBaseUrl}/activities?pairingCode={pairingCode}";
    public string GetAssetsListUrl() => $"{apiBaseUrl}/assets?pairingCode={pairingCode}";
}
