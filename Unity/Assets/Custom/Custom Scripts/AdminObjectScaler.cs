using UnityEngine;

// Handles scaling selected objects in admin mode
public class AdminObjectScaler : MonoBehaviour
{
    // References
    public AdminObjectSelector selector;
    
    // Settings
    public float scaleSpeed = 0.1f;

    void Update()
    {
        // Only active in Scale mode
        if (AdminModeController.CurrentMode == AdminModeController.Mode.Scale)
        {
            GameObject obj = selector.GetSelectedObject();
            if (obj != null)
            {
                float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
                if (scroll != 0)
                {
                    // Apply uniform scaling
                    Vector3 newScale = obj.transform.localScale + Vector3.one * scroll * scaleSpeed;
                    
                    // Prevent objects from becoming too small
                    newScale = Vector3.Max(newScale, new Vector3(0.1f, 0.1f, 0.1f));
                    obj.transform.localScale = newScale;

                    Debug.Log($"Scaling {obj.name}: {obj.transform.localScale}");
                }
            }
        }
    }
}