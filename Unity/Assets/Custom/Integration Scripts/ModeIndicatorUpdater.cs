using UnityEngine;
using TMPro;

/// <summary>
/// Updates the UI mode indicator text based on the current mode
/// </summary>
public class ModeIndicatorUpdater : MonoBehaviour
{
    [Header("References")]
    public AdminController adminController;
    public TextMeshProUGUI modeIndicatorText;
    
    [Header("Settings")]
    public Color tractorBeamColor = Color.green;
    public Color uiModeColor = Color.yellow;
    public string tractorBeamText = "TRACTOR BEAM MODE - Press TAB for UI Mode";
    public string uiModeText = "UI MODE - Press TAB for Tractor Beam";
    
    void Update()
    {
        if (adminController == null || modeIndicatorText == null)
            return;
            
        // Update text and color based on current mode
        bool isUIMode = adminController.IsInUIMode();
        
        if (isUIMode)
        {
            modeIndicatorText.text = uiModeText;
            modeIndicatorText.color = uiModeColor;
        }
        else
        {
            modeIndicatorText.text = tractorBeamText;
            modeIndicatorText.color = tractorBeamColor;
        }
    }
} 