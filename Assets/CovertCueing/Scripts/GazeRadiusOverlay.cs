using UnityEngine;

/// <summary>
/// GazeRadiusOverlay draws a circular outline on screen representing the
/// 15-degree gaze threshold used by the covert cueing system.
/// 
/// Objects INSIDE this circle are considered to be in foveal vision — the covert
/// cue is suppressed (no brightness boost). Objects OUTSIDE this circle are in
/// peripheral vision — the covert cue is active (objects pulse brighter).
/// 
/// This overlay helps demonstrate and understand the covert cueing technique
/// by making the invisible gaze threshold visible on screen.
/// </summary>
public class GazeRadiusOverlay : MonoBehaviour
{
    [Header("Appearance")]
    /// <summary>Color of the gaze threshold circle.</summary>
    public Color circleColor = new Color(1f, 1f, 1f, 0.5f);
    /// <summary>Thickness of the circle outline in pixels.</summary>
    public float lineThickness = 3f;
    /// <summary>Number of line segments used to draw the circle (higher = smoother).</summary>
    public int segments = 64;

    [Header("Threshold")]
    /// <summary>
    /// The gaze angle threshold in degrees. Must match the threshold
    /// used in CovertObject.cs (default: 15 degrees).
    /// </summary>
    public float gazeThresholdDegrees = 15f;

    [Header("Labels")]
    /// <summary>Show text labels explaining the zones.</summary>
    public bool showLabels = true;

    // Internal
    private Material lineMaterial;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        CreateLineMaterial();
    }

    /// <summary>
    /// Creates a simple unlit material for GL line drawing.
    /// </summary>
    void CreateLineMaterial()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    /// <summary>
    /// Computes the screen-space pixel radius corresponding to the gaze threshold angle.
    /// 
    /// The camera's vertical FOV defines how many degrees map to the screen height.
    /// A 15-degree cone from the camera center translates to a specific pixel radius
    /// based on: radius = (Screen.height / 2) * tan(threshold) / tan(halfFOV)
    /// </summary>
    float GetScreenRadius()
    {
        if (cam == null) return 100f;
        float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float thresholdRad = gazeThresholdDegrees * Mathf.Deg2Rad;
        return (Screen.height * 0.5f) * Mathf.Tan(thresholdRad) / Mathf.Tan(halfFovRad);
    }

    /// <summary>
    /// Draws the circle using GL immediate-mode rendering after all cameras have rendered.
    /// This ensures it appears on top of the 3D scene as a screen-space overlay.
    /// </summary>
    void OnPostRender()
    {
        if (lineMaterial == null) CreateLineMaterial();

        float radius = GetScreenRadius();
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        GL.PushMatrix();
        GL.LoadPixelMatrix();
        lineMaterial.SetPass(0);

        // Draw the circle outline
        // We draw multiple concentric rings to simulate line thickness
        for (float t = 0; t < lineThickness; t += 1f)
        {
            float r = radius + t - lineThickness * 0.5f;
            GL.Begin(GL.LINE_STRIP);
            GL.Color(circleColor);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = center.x + Mathf.Cos(angle) * r;
                float y = center.y + Mathf.Sin(angle) * r;
                GL.Vertex3(x, y, 0);
            }
            GL.End();
        }

        // Draw crosshair at center
        float crossSize = 14f;
        GL.Begin(GL.LINES);
        GL.Color(circleColor);
        // Horizontal
        GL.Vertex3(center.x - crossSize, center.y, 0);
        GL.Vertex3(center.x + crossSize, center.y, 0);
        // Vertical
        GL.Vertex3(center.x, center.y - crossSize, 0);
        GL.Vertex3(center.x, center.y + crossSize, 0);
        GL.End();

        GL.PopMatrix();
    }

    /// <summary>
    /// Draws text labels using OnGUI to explain the zones.
    /// </summary>
    void OnGUI()
    {
        if (!showLabels) return;

        float radius = GetScreenRadius();
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // Style for labels
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = circleColor;
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // Label at the top of the circle
        // OnGUI uses top-left origin, so we flip Y
        float guiCenterY = Screen.height * 0.5f;
        float guiLabelY = guiCenterY - radius - 30f;
        Rect labelRect = new Rect(center.x - 160f, guiLabelY, 320f, 25f);
        GUI.Label(labelRect, "— Foveal threshold (15°) —", style);

        // "Cue suppressed" label inside the circle
        style.fontSize = 14;
        style.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
        Rect innerRect = new Rect(center.x - 100f, guiCenterY + radius * 0.3f, 200f, 25f);
        GUI.Label(innerRect, "Cue suppressed", style);

        // "Cue active" label outside the circle (top-right)
        style.fontSize = 15;
        style.normal.textColor = new Color(0.5f, 1f, 0.5f, 0.5f);
        Rect outerRect = new Rect(center.x + radius + 15f, guiCenterY - radius * 0.5f, 150f, 25f);
        style.alignment = TextAnchor.MiddleLeft;
        GUI.Label(outerRect, "← Cue active", style);

        // Instructions at the bottom of the screen
        style.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        style.fontSize = 15;
        style.alignment = TextAnchor.LowerCenter;
        Rect instructRect = new Rect(0, Screen.height - 40f, Screen.width, 35f);
        GUI.Label(instructRect, "WASD = Move  |  Mouse = Look  |  Objects pulse brighter outside the circle", style);
    }
}
