using UnityEngine;

// Handles rotating selected objects in admin mode
public class AdminObjectRotator : MonoBehaviour
{
    // References
    public AdminObjectSelector selector;
    public Transform adminCamera;
    
    // Settings
    public float rotationSpeed = 3000f;

    void Update()
    {
        // Only active in Rotate mode
        if (AdminModeController.CurrentMode == AdminModeController.Mode.Rotate)
        {
            GameObject obj = selector.GetSelectedObject();
            if (obj != null)
            {
                float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
                if (scroll != 0)
                {
                    Vector3 rotationAxis = Vector3.zero;

                    // Set rotation axis based on current selection
                    switch (AdminModeController.CurrentRotationAxis)
                    {
                        case AdminModeController.RotationAxis.X:
                            rotationAxis = adminCamera.right;
                            break;
                        case AdminModeController.RotationAxis.Y:
                            rotationAxis = adminCamera.up;
                            break;
                        case AdminModeController.RotationAxis.Z:
                            rotationAxis = adminCamera.forward;
                            break;
                    }

                    // Apply rotation
                    obj.transform.Rotate(rotationAxis, scroll * rotationSpeed * Time.deltaTime, Space.World);
                    Debug.Log($"Rotating {obj.name} on {AdminModeController.CurrentRotationAxis}-axis");
                }
            }
        }
    }
}