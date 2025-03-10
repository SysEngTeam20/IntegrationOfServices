using UnityEngine;

// Controls the current interaction mode for admin tools
public class AdminModeController : MonoBehaviour
{
    // Available interaction modes
    public enum Mode { Move, Scale, Rotate }
    public static Mode CurrentMode = Mode.Move;

    // Rotation axis options
    public enum RotationAxis { X, Y, Z }
    public static RotationAxis CurrentRotationAxis = RotationAxis.Y;

    void Update()
    {
        // Cycle modes: Move -> Scale -> Rotate
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            if (CurrentMode == Mode.Move)
                CurrentMode = Mode.Scale;
            else if (CurrentMode == Mode.Scale)
                CurrentMode = Mode.Rotate;
            else
                CurrentMode = Mode.Move;

            Debug.Log($"Mode Switched to: {CurrentMode}");
        }

        // Cycle rotation axis: X -> Y -> Z
        if (CurrentMode == Mode.Rotate && Input.GetKeyDown(KeyCode.R))
        {
            if (CurrentRotationAxis == RotationAxis.X)
                CurrentRotationAxis = RotationAxis.Y;
            else if (CurrentRotationAxis == RotationAxis.Y)
                CurrentRotationAxis = RotationAxis.Z;
            else
                CurrentRotationAxis = RotationAxis.X;

            Debug.Log($"Rotation Axis Switched to: {CurrentRotationAxis}");
        }
    }
}