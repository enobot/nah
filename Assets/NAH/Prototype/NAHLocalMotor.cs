using UnityEngine;
using UnityEngine.InputSystem;

// LOCAL PROTOTYPE ONLY. This does not implement network authority/prediction.
// Requires a CharacterController, PlayerInput, and a Camera Pivot reference.
// Input Actions: Move (Vector2), Look (Vector2), Jump (Button), Sprint (Button).
[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public sealed class NAHLocalMotor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Horizontal Movement")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0.1f)] private float sprintSpeed = 3.4f;
    [SerializeField, Min(0.1f)] private float groundAcceleration = 6f;
    [SerializeField, Min(0.1f)] private float groundDeceleration = 14f;
    [SerializeField, Min(0.1f)] private float airAcceleration = 1.5f;

    [Header("Jump and Gravity")]
    [SerializeField, Min(0.1f)] private float jumpHeight = 0.65f;
    [SerializeField] private float gravity = -28f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

    [Header("Camera")]
    [SerializeField, Min(0.01f)] private float mouseDegreesPerPixel = 0.12f;
    [SerializeField, Min(1f)] private float gamepadDegreesPerSecond = 160f;
    [SerializeField, Range(1f, 89f)] private float pitchLimit = 85f;

    private CharacterController controller;
    private InputAction moveAction, lookAction, jumpAction, sprintAction;
    private Vector3 horizontalVelocity;
    private float verticalVelocity, pitch, coyoteTimer, jumpBufferTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        var playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move", true);
        lookAction = playerInput.actions.FindAction("Look", true);
        jumpAction = playerInput.actions.FindAction("Jump", true);
        sprintAction = playerInput.actions.FindAction("Sprint", true);

        if (cameraPivot == null)
        {
            Debug.LogError("Assign the local camera pivot to NAHLocalMotor.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Mouse delta is already per-frame; gamepad stick is a continuous rate.
        Vector2 look = lookAction.ReadValue<Vector2>();
        bool mouseLook = lookAction.activeControl?.device is Mouse;
        Vector2 angles = mouseLook
            ? look * mouseDegreesPerPixel
            : look * gamepadDegreesPerSecond * dt;
        transform.Rotate(0f, angles.x, 0f);
        pitch = Mathf.Clamp(pitch - angles.y, -pitchLimit, pitchLimit);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        bool grounded = controller.isGrounded;
        coyoteTimer = grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - dt);
        jumpBufferTimer = jumpAction.WasPressedThisFrame()
            ? jumpBufferTime : Mathf.Max(0f, jumpBufferTimer - dt);

        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && gravity < 0f)
        {
            verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
        verticalVelocity += gravity * dt;

        Vector2 axis = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        Vector3 direction = transform.right * axis.x + transform.forward * axis.y;
        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 desiredVelocity = direction * speed;
        float acceleration = grounded
            ? (axis.sqrMagnitude > 0.001f ? groundAcceleration : groundDeceleration)
            : airAcceleration;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, desiredVelocity, acceleration * dt);

        // CharacterController.Move performs collision-constrained movement,
        // but DOES NOT apply gravity for you.
        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
    }
}
