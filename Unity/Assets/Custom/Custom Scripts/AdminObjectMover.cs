using UnityEngine;

// Handles moving selected objects in admin mode
public class AdminObjectMover : MonoBehaviour
{
    // References
    public AdminObjectSelector selector;
    public Transform holdPoint; // Point where objects will be positioned
    
    // Movement settings
    public float pullSpeed = 5f; // Object movement speed
    public float scrollSpeed = 2f; // Distance adjustment speed
    
    private GameObject grabbedObject;
    private float holdDistance = 1f; // Default distance from admin

    void Update()
    {
        grabbedObject = selector.GetSelectedObject();
        if (grabbedObject != null)
        {
            // Move object toward the hold point at current distance
            Vector3 targetPosition = holdPoint.position + holdPoint.forward * holdDistance;
            grabbedObject.transform.position = Vector3.Lerp(
                grabbedObject.transform.position, 
                targetPosition, 
                Time.deltaTime * pullSpeed
            );

            // Adjust distance with scroll wheel in Move mode
            if (AdminModeController.CurrentMode == AdminModeController.Mode.Move)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                holdDistance += scroll * scrollSpeed;
                holdDistance = Mathf.Clamp(holdDistance, 1f, 100f); // Keep object at reasonable distance
            }
        }
    }
}