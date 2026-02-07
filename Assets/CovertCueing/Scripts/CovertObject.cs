using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CovertObject drives the covert cueing effect on a single object in the scene.
/// 
/// COVERT CUEING TECHNIQUE:
/// The object subtly pulses brighter when it is in the player's peripheral vision
/// (gaze angle > 15 degrees), drawing attention without the player being consciously 
/// aware of the cue. When the player looks directly at the object (gaze angle &lt;= 15 
/// degrees), the effect is suppressed — making the cue imperceptible during direct fixation.
/// 
/// HOW IT WORKS:
/// 1. Each frame, this script sends the object's world-space center position, effect radius,
///    and a time-varying modulation value to the Covert_S shader via material properties.
/// 2. The shader uses these to add a radial brightness boost to the object's surface.
/// 3. This script also computes the angular distance between the player's gaze direction
///    and the direction toward this object. A running average (window of 5 samples) smooths
///    this measurement.
/// 4. If the smoothed angle falls below 15 degrees (foveal vision), the radius is set to 0,
///    effectively disabling the shader's brightness boost.
/// 5. If the angle exceeds 15 degrees (peripheral vision), the original radius is restored.
/// 
/// DEPENDENCIES:
/// - SimulatedEyeSight (singleton): Provides the gaze direction via Instance.Trans.forward
/// - Covert_S shader (QEDLab/Covert): The shader that renders the brightness effect
/// - A Material using the QEDLab/Covert shader must be assigned or present on the Renderer
/// 
/// ADAPTED FROM: Original CovertObject.cs in the Urban Grid Environment Experiment project.
/// Changes: VR eye-tracking reference (EyeSight) replaced with SimulatedEyeSight.
/// </summary>
public class CovertObject : MonoBehaviour
{
    [Header("Shader Configuration")]
    /// <summary>
    /// The material instance using the QEDLab/Covert shader.
    /// If not assigned, it will be auto-instantiated from this object's Renderer at Start().
    /// IMPORTANT: Each CovertObject must have its own material instance, because shader
    /// parameters (_CenterPosition, _Radius, _Modulation) are set per-object per-frame.
    /// </summary>
    public Material Mat;

    /// <summary>
    /// The world-space center point for the radial brightness effect.
    /// The shader calculates distance from each fragment to this point.
    /// Defaults to this GameObject's transform if not assigned.
    /// </summary>
    public Transform Pivot;

    /// <summary>
    /// The radius of the covert shader effect in world units.
    /// Fragments within this distance from the Pivot receive a brightness boost
    /// that falls off linearly toward the edge.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Cached original radius value, restored when the cue re-activates.
    /// </summary>
    [HideInInspector]
    public float originalRadius;

    [Header("Modulation")]
    /// <summary>
    /// AnimationCurve controlling the temporal modulation of the brightness effect.
    /// Evaluated at (Time.time % 0.2f), creating a repeating 0.2-second cycle.
    /// The curve's output is sent to the shader as _Modulation, scaling the brightness boost.
    /// A value of 0 = no boost; 1 = maximum boost (0.095 added to RGB per the shader).
    /// </summary>
    public AnimationCurve Carve;

    [Header("Cueing Control")]
    /// <summary>
    /// Master toggle for this object's covert cue. Set to false to disable cueing entirely.
    /// </summary>
    public bool NeedCue = true;

    // --- Running average for gaze angle smoothing ---
    // A sliding window of the last N angle measurements, used to prevent
    // flickering when the player's gaze is near the 15-degree threshold.
    private LinkedList<float> eyeSightAngleList = new LinkedList<float>();
    private int capacity = 5;       // Number of samples in the running average window
    private float sum = 0.0f;       // Running sum for efficient average calculation
    private float runningEyeSightAngleAvg = 0.0f;

    void Start()
    {
        if (!Pivot)
        {
            Pivot = transform;
        }

        // Auto-instantiate the material if not explicitly assigned.
        // This ensures each CovertObject has its own material instance,
        // preventing shared-material parameter conflicts.
        if (Mat == null)
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                Mat = r.material; // Unity auto-creates an instance
            }
        }

        originalRadius = Radius;
    }

    void Update()
    {
        if (!NeedCue)
        {
            return;
        }

        #region Deliver the parameters to the shader
        // Send this object's world-space center, radius, and modulation to the shader.
        // The shader uses these to compute a per-fragment radial brightness boost.
        Mat.SetVector("_CenterPosition", new Vector4(Pivot.transform.position.x, Pivot.transform.position.y, Pivot.transform.position.z, 0));
        Mat.SetFloat("_Radius", Radius);
        // Evaluate the AnimationCurve at a repeating 0.2-second cycle.
        // This creates the temporal flicker/pulse pattern of the covert cue.
        Mat.SetFloat("_Modulation", Carve.Evaluate(Time.time % 0.2f));
        #endregion

        #region Check to see if we need to show the cue
        // Update the running average of the gaze-to-object angle.
        UpdateLinkedList(EyeSightAngle());
        runningEyeSightAngleAvg = GetRunningAverage();

        // THRESHOLD: 15 degrees
        // If the player's gaze is within 15 degrees of this object (foveal vision),
        // suppress the cue by setting radius to 0. The brightness effect disappears.
        // If the gaze moves beyond 15 degrees (peripheral vision), restore the cue.
        if (runningEyeSightAngleAvg <= 15f)
        {
            Radius = 0;
        }
        else
        {
            Radius = originalRadius;
        }
        #endregion
    }

    /// <summary>
    /// Calculates the angle (in degrees) between the player's gaze direction
    /// and the direction from the camera to this object.
    /// 
    /// Uses SimulatedEyeSight.Instance.Trans.forward as the gaze direction.
    /// In the original VR implementation, this was EyeSight.Instance.Trans.forward,
    /// which represented the actual eye-tracking gaze vector from the headset.
    /// </summary>
    public float EyeSightAngle()
    {
        Vector3 tmpDir = (transform.position - Camera.main.transform.position).normalized;
        return Vector3.Angle(tmpDir, SimulatedEyeSight.Instance.Trans.forward);
    }

    /// <summary>
    /// Adds a new angle sample to the running average window.
    /// If the window is at capacity, the oldest sample is removed first.
    /// This sliding window approach smooths out frame-to-frame noise in the
    /// gaze angle measurement, preventing the cue from flickering on/off rapidly.
    /// </summary>
    public void UpdateLinkedList(float value)
    {
        if (eyeSightAngleList.Count == capacity)
        {
            sum -= eyeSightAngleList.Last.Value;
            eyeSightAngleList.RemoveLast();
        }

        eyeSightAngleList.AddFirst(value);
        sum += value;
    }

    /// <summary>
    /// Returns the current running average of gaze angle samples.
    /// </summary>
    public float GetRunningAverage()
    {
        if (eyeSightAngleList.Count == 0)
        {
            Debug.LogWarning("CovertObject: No samples in running average window.");
        }
        return sum / eyeSightAngleList.Count;
    }

    #region Editor Gizmos
    /// <summary>
    /// Draws a cyan wireframe sphere in the Scene view showing the current
    /// effect radius centered on the Pivot. Useful for visualizing the cue's
    /// spatial extent during development.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (!Pivot)
        {
            Pivot = transform;
        }
        Gizmos.DrawWireSphere(Pivot.transform.position, Radius);
    }
    #endregion
}
