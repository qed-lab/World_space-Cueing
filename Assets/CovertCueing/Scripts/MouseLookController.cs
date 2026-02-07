using UnityEngine;

/// <summary>
/// Simple first-person controller for the covert cueing demo.
/// Provides WASD movement and mouse look so the player can walk around
/// the scene and observe the covert cueing effect in their peripheral vision.
/// 
/// Controls:
///   WASD        — Move forward/back/left/right
///   Mouse       — Look around (simulated gaze direction)
///   Left Shift  — Sprint
///   Escape      — Unlock cursor
///   Left Click  — Lock cursor
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MouseLookController : MonoBehaviour
{
    [Header("Movement")]
    /// <summary>Walking speed in units per second.</summary>
    public float moveSpeed = 5.0f;
    /// <summary>Speed multiplier when holding Left Shift.</summary>
    public float sprintMultiplier = 2.0f;

    [Header("Mouse Look")]
    /// <summary>Mouse sensitivity for look rotation.</summary>
    public float mouseSensitivity = 2.0f;
    /// <summary>Maximum vertical look angle in degrees (up and down).</summary>
    public float verticalClampAngle = 85f;

    [Header("References")]
    /// <summary>
    /// The camera transform for applying pitch (vertical) rotation.
    /// If not assigned, Camera.main.transform is used.
    /// </summary>
    public Transform cameraTransform;

    private float verticalRotation = 0f;
    private CharacterController controller;
    private float verticalVelocity = 0f;
    private int frameCount = 0;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Lock and hide the cursor for FPS-style control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        frameCount++;

        // Skip mouse input for the first few frames to prevent the initial
        // cursor-lock jerk from rotating the camera on startup
        if (frameCount > 3)
        {
            HandleMouseLook();
        }
        HandleMovement();
        HandleCursorToggle();
    }

    /// <summary>
    /// Handles horizontal (yaw) and vertical (pitch) rotation from mouse input.
    /// Horizontal rotation is applied to the player root transform.
    /// Vertical rotation is applied to the camera child transform.
    /// </summary>
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Horizontal rotation: rotate the player body around Y axis
        transform.Rotate(0f, mouseX, 0f);

        // Vertical rotation: accumulate and clamp, apply to camera
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalClampAngle, verticalClampAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    /// <summary>
    /// Handles WASD movement with optional sprint and gravity.
    /// </summary>
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * h + transform.forward * v;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed *= sprintMultiplier;
        }

        // Apply gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; // Small downward force to keep grounded
        }
        else
        {
            verticalVelocity -= 9.81f * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Toggles cursor lock state for convenience.
    /// Press Escape to unlock and show cursor; click to re-lock.
    /// </summary>
    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
