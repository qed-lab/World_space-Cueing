using UnityEngine;

/// <summary>
/// SimulatedEyeSight replaces the VR-dependent EyeSight class from the original project.
/// 
/// In the original implementation, EyeSight used HTC Vive's Wave.Essence.Eye.EyeManager
/// to obtain the user's real eye-tracking gaze direction. This class simulates that behavior
/// by treating the main camera's forward direction as the gaze direction.
/// 
/// This means: wherever the player looks with the mouse (camera forward) = simulated gaze.
/// 
/// Interface contract (preserved from original EyeSight):
///   - SimulatedEyeSight.Instance.Trans.forward → the current gaze direction
///   - SimulatedEyeSight.Instance.EyePosition   → world-space origin of gaze
///   - SimulatedEyeSight.Instance.EyeDirection   → world-space rotation of gaze
/// 
/// CovertObject.cs depends on Instance.Trans.forward to determine if the player
/// is looking at the cued object.
/// </summary>
public class SimulatedEyeSight : MonoBehaviour
{
    /// <summary>
    /// Singleton instance, accessed by CovertObject to get gaze direction.
    /// </summary>
    public static SimulatedEyeSight Instance;

    /// <summary>
    /// The transform whose .forward vector represents the current gaze direction.
    /// In VR, this would be a separate transform driven by eye-tracking hardware.
    /// In this simulation, it is simply the main camera's transform.
    /// </summary>
    public Transform Trans;

    /// <summary>
    /// The world-space rotation of the simulated gaze.
    /// </summary>
    public Quaternion EyeDirection = Quaternion.identity;

    /// <summary>
    /// The world-space position of the simulated gaze origin (camera position).
    /// </summary>
    public Vector3 EyePosition = Vector3.zero;

    private void OnEnable()
    {
        Instance = this;

        // In the original, Trans was a separate transform driven by VR eye tracking.
        // Here, we use the camera transform directly since gaze = camera forward.
        if (Trans == null)
        {
            Trans = Camera.main.transform;
        }
    }

    void Update()
    {
        // Update gaze data from the main camera each frame.
        // In the original EyeSight.cs, these values came from:
        //   EyeManager.Instance.GetCombindedEyeDirectionNormalized(out EyeSightForward);
        EyePosition = Camera.main.transform.position;
        EyeDirection = Camera.main.transform.rotation;
    }
}
