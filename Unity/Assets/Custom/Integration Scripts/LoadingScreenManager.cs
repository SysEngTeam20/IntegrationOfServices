using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages a loading screen overlay that appears during scene loading
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup loadingCanvasGroup;   // Canvas group for fading
    public Image loadingBackground;          // Background panel
    [Tooltip("Optional - can be left null")]
    public TextMeshProUGUI loadingText;      // Loading text/status
    public float fadeDuration = 0.5f;        // Fade in/out duration in seconds
    
    [Header("Appearance")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    
    // Singleton instance
    private static LoadingScreenManager _instance;
    public static LoadingScreenManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LoadingScreenManager>();
            }
            return _instance;
        }
    }
    
    // Current loading operation
    private bool isLoading = false;
    
    private Coroutine fadeCoroutine;
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize loading screen in hidden state
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.blocksRaycasts = false;
            loadingCanvasGroup.interactable = false;
        }
        
        // Set background color if provided
        if (loadingBackground != null)
        {
            loadingBackground.color = backgroundColor;
        }
    }
    
    /// <summary>
    /// Show the loading screen with initial message
    /// </summary>
    public void ShowLoadingScreen(string message = "Loading...")
    {
        // Stop any existing fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // Set loading text if available
        if (loadingText != null)
        {
            loadingText.text = message;
        }
        
        // Start fade in coroutine
        fadeCoroutine = StartCoroutine(FadeLoadingScreen(1f));
    }
    
    /// <summary>
    /// Hide the loading screen
    /// </summary>
    public void HideLoadingScreen()
    {
        // Stop any existing fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // Start fade out coroutine
        fadeCoroutine = StartCoroutine(FadeLoadingScreen(0f));
    }
    
    /// <summary>
    /// Update loading message
    /// </summary>
    public void UpdateMessage(string message)
    {
        // Update message if text component exists
        if (message != null && loadingText != null)
        {
            loadingText.text = message;
        }
    }
    
    /// <summary>
    /// Coroutine to fade the loading screen in or out
    /// </summary>
    private IEnumerator FadeLoadingScreen(float targetAlpha)
    {
        if (loadingCanvasGroup == null)
        {
            Debug.LogError("Loading Canvas Group is not assigned!");
            yield break;
        }
        
        // Enable canvas group if fading in
        if (targetAlpha > 0)
        {
            loadingCanvasGroup.blocksRaycasts = true;
            loadingCanvasGroup.interactable = true;
        }
        
        float startAlpha = loadingCanvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration);
            loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            yield return null;
        }
        
        loadingCanvasGroup.alpha = targetAlpha;
        
        // Disable canvas group if faded out
        if (targetAlpha <= 0)
        {
            loadingCanvasGroup.blocksRaycasts = false;
            loadingCanvasGroup.interactable = false;
        }
    }
} 