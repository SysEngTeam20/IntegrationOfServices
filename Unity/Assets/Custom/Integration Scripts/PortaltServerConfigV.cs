using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PortaltServerConfigV", menuName = "Portalt/Server Config V", order = 2)]
public class PortaltServerConfigV : ScriptableObject {
    [Header("Server Configuration")]
    public string serverIp = "localhost";
    public int serverPort = 3000;
    
    [Header("Access Configuration")]
    [Tooltip("Join code for accessing content from the Portalt server")]
    public string joinCode = "";
    
    public string apiBaseUrl => $"https://portalt.vercel.app/api";
    
    // The primary method for the viewer to access content
    public string GetActivityJoinUrl() => $"{apiBaseUrl}/public/activity-join?joinCode={joinCode}";
    
    // Keep GetSceneConfigUrl for compatibility with existing code
    public string GetSceneConfigUrl(string sceneId) => GetActivityJoinUrl();
} 