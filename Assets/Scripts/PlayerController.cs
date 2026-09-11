using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float sprintSpeed = 21f;
    public float walkSpeed = 9f;
    public float crouchSpeed = 4.5f;

    [Header("Jump")]
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Crouch")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;

    [Header("Animation Smoothing")]
    public float animSmoothTime = 0.1f;

    [Header("Direction Change")]
    public float angle90RecoverTime  = 0.25f;
    public float angle180RecoverTime = 0.5f;

    private CharacterController cc;
    private Animator animator;
    private Transform cameraTarget; // TPSCamera

    private float xRotation;
    private float yVelocity;
    private bool isGroundedPrev;
    private float smoothH;
    private float smoothV;
    private float smoothHVel;
    private float smoothVVel;
    private Vector2 lastInputDir;
    private float speedMultiplier = 1f;
    private float speedMultiplierVel;
    private float rawH;
    private float rawV;

    [Header("Model Rotation")]
    public float modelYRotDefault = 45f;
    public float modelYRotSmooth = 0.15f;
    private Transform modelTransform;
    private float currentModelYRot;
    private float modelYRotVel;

    // Exposed for NetworkPlayer sync
    [HideInInspector] public float animHorizontal;
    [HideInInspector] public float animVertical;
    [HideInInspector] public bool animIsRunning;
    [HideInInspector] public bool animIsWalking;
    [HideInInspector] public bool animIsIdle;
    [HideInInspector] public bool animIsJumping;
    [HideInInspector] public bool animIsCrouching;
    [HideInInspector] public bool animIsCrouchWalking;

    // Animator parameter hashes
    private static readonly int H          = Animator.StringToHash("Horizontal");
    private static readonly int V          = Animator.StringToHash("Vertical");
    private static readonly int IsRunning  = Animator.StringToHash("isRunning");
    private static readonly int IsWalking  = Animator.StringToHash("isWalking");
    private static readonly int IsIdle     = Animator.StringToHash("isIdle");
    private static readonly int IsJumping  = Animator.StringToHash("isJumping");
    private static readonly int IsCrouch        = Animator.StringToHash("isCrouching");
    private static readonly int IsCrouchWalking  = Animator.StringToHash("isCrouchWalking");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
            modelTransform = animator.transform;
        if (animator == null)
            Debug.LogError("[PlayerController] No Animator found in children!");
        else
            Debug.Log($"[PlayerController] OnEnable — animator found on {animator.gameObject.name}, controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");

        animIsIdle = true;
        cameraTarget = transform.Find("TPSCamera");
        if (cameraTarget == null)
        {
            GameObject ct = new GameObject("TPSCamera");
            ct.transform.SetParent(transform);
            ct.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            cameraTarget = ct.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        if (cameraTarget == null) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.1f;
        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -60f, 60f);
        cameraTarget.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    void HandleMovement()
    {
        var keyboard = Keyboard.current;

        bool isCrouching = keyboard.leftCtrlKey.isPressed;
        animIsCrouching = isCrouching;

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        cc.height = targetHeight;
        cc.center = new Vector3(0f, targetHeight / 2f, 0f);

        float h = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float v = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        rawH = h;
        rawV = v;

        smoothH = Mathf.SmoothDamp(smoothH, h, ref smoothHVel, animSmoothTime);
        smoothV = Mathf.SmoothDamp(smoothV, v, ref smoothVVel, animSmoothTime);

        animHorizontal = smoothH;
        animVertical   = smoothV;

        float inputMag = Mathf.Abs(h) + Mathf.Abs(v);

        // --- Direction change speed penalty ---
        Vector2 currentInputDir = new Vector2(h, v);
        if (currentInputDir.magnitude > 0.1f && lastInputDir.magnitude > 0.1f)
        {
            float angle = Vector2.Angle(lastInputDir, currentInputDir);
            if (angle > 135f)
            {
                // 180 degree change — start from 0.5
                speedMultiplier = Mathf.Min(speedMultiplier, 0.5f);
            }
            else if (angle > 45f)
            {
                // 90 degree change — start from 0.75
                speedMultiplier = Mathf.Min(speedMultiplier, 0.75f);
            }
        }
        if (currentInputDir.magnitude > 0.1f)
            lastInputDir = currentInputDir;

        // Recover speed multiplier
        float recoverTime = speedMultiplier < 0.6f ? angle180RecoverTime : angle90RecoverTime;
        speedMultiplier = Mathf.SmoothDamp(speedMultiplier, 1f, ref speedMultiplierVel, recoverTime);
        if (inputMag < 0.1f) { speedMultiplier = 1f; speedMultiplierVel = 0f; }

        bool isWalkingKey = keyboard.leftShiftKey.isPressed;

        float currentSpeed = 0f;
        if (isCrouching)
        {
            currentSpeed        = inputMag > 0f ? crouchSpeed : 0f;
            animIsRunning       = false;
            animIsWalking       = false;
            animIsIdle          = false;
            animIsCrouchWalking = inputMag > 0f;
        }
        else
        {
            animIsCrouchWalking = false;
        }

        if (!isCrouching && inputMag > 0f && isWalkingKey)
        {
            currentSpeed   = walkSpeed;
            animIsRunning  = false;
            animIsWalking  = true;
            animIsIdle     = false;
        }
        else if (!isCrouching && inputMag > 0f)
        {
            currentSpeed   = sprintSpeed;
            animIsRunning  = true;
            animIsWalking  = false;
            animIsIdle     = false;
        }
        else if (!isCrouching)
        {
            currentSpeed   = 0f;
            animIsRunning  = false;
            animIsWalking  = false;
            animIsIdle     = true;
        }

        bool grounded = cc.isGrounded;

        if (grounded)
        {
            yVelocity = -2f;

            // Trigger jump only on the exact frame Space is pressed
            if (keyboard.spaceKey.wasPressedThisFrame && !isCrouching)
            {
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animIsJumping = true;
            }
            else
            {
                animIsJumping = false;
            }
        }
        else
        {
            // Once airborne, clear the jump trigger immediately so it fires once
            animIsJumping = false;
        }

        isGroundedPrev = grounded;
        yVelocity += gravity * Time.deltaTime;

        Vector3 moveDir = transform.right * h + transform.forward * v;
        cc.Move(moveDir.normalized * currentSpeed * speedMultiplier * Time.deltaTime + Vector3.up * yVelocity * Time.deltaTime);

        ApplyAnimatorState();
        ApplyModelRotation();
    }

    void ApplyModelRotation()
    {
        if (modelTransform == null) return;
        bool sprintZeroRot = animIsRunning && (Mathf.Abs(rawH) < 0.1f || rawV * rawH < 0f);
        float targetY = (animIsIdle || sprintZeroRot) ? 0f : modelYRotDefault;
        currentModelYRot = Mathf.SmoothDamp(currentModelYRot, targetY, ref modelYRotVel, modelYRotSmooth);
        modelTransform.localEulerAngles = new Vector3(
            modelTransform.localEulerAngles.x,
            currentModelYRot,
            modelTransform.localEulerAngles.z
        );
    }

    void ApplyAnimatorState()
    {
        if (animator == null) return;

        animator.SetFloat(H,         animHorizontal);
        animator.SetFloat(V,         animVertical);
        animator.SetBool(IsRunning,  animIsRunning);
        animator.SetBool(IsWalking,  animIsWalking);
        animator.SetBool(IsIdle,     animIsIdle);
        animator.SetBool(IsJumping,  animIsJumping);
        animator.SetBool(IsCrouch,        animIsCrouching);
        animator.SetBool(IsCrouchWalking,  animIsCrouchWalking);
        animator.speed = speedMultiplier;
    }

    public void ApplyRemoteAnimState(float h, float v, bool running, bool walking, bool idle, bool jumping, bool crouching, bool crouchWalking = false)
    {
        if (animator == null) return;
        animator.SetFloat(H,         h);
        animator.SetFloat(V,         v);
        animator.SetBool(IsRunning,  running);
        animator.SetBool(IsWalking,  walking);
        animator.SetBool(IsIdle,     idle);
        animator.SetBool(IsJumping,  jumping);
        animator.SetBool(IsCrouch,        crouching);
        animator.SetBool(IsCrouchWalking,  crouchWalking);
    }
}
