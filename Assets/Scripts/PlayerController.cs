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
    public float crouchJumpHeight = 0.8f;
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

    [HideInInspector] public float xRotation;
    private float yVelocity;
    private float idleTimer;
    private bool jumpLock;
    private float jumpLockTimer;
    private bool jumpWasCrouching;
    private bool jumpWasCrouchWalking;
    private bool isAirborneFromJump;
    private float smoothH;
    private float smoothV;
    private float smoothHVel;
    private float smoothVVel;
    private Vector2 lastInputDir;
    private float speedMultiplier = 1f;
    private float speedMultiplierVel;
    private float rawH;
    private float rawV;

    [Header("Body Collider")]
    public CapsuleCollider bodyCollider;
    private float colliderHeight;
    private float colliderRadius;
    private Vector3 colliderCenter;
    private float colliderXRot;
    private float colliderHeightVel;
    private float colliderRadiusVel;
    private Vector3 colliderCenterVel;
    private float colliderXRotVel;
    [SerializeField] private float colliderSmoothTime;

    [Header("Upper Body — Hips")]
    public Transform hipsTransform;
    [Range(0f, 1f)] public float hipsWeight = 0.2f;
    public float hipsAngleOffset = 0f;

    [Header("Upper Body — Spine")]
    public Transform spineTransform;
    [Range(0f, 1f)] public float spineWeight = 0.6f;
    public float spineAngleOffset = 15f;

    [Header("Upper Body — Chest")]
    public Transform chestTransform;
    [Range(0f, 1f)] public float chestWeight = 0.85f;
    public float chestAngleOffset = 0f;

    [Header("Upper Body — Upper Chest")]
    public Transform upperChestTransform;
    [Range(0f, 1f)] public float upperChestWeight = 0.85f;
    public float upperChestAngleOffset = 0f;

    [Header("Upper Body — Neck")]
    public Transform neckTransform;
    [Range(0f, 1f)] public float neckWeight = 0.5f;
    public float neckAngleOffset = 0f;

    [Header("Upper Body — Head")]
    public Transform headTransform;
    [Range(0f, 1f)] public float headWeight = 0.85f;
    public float headAngleOffset = 0f;

    [Header("Upper Body — Left Shoulder")]
    public Transform leftShoulderTransform;
    [Range(0f, 1f)] public float leftShoulderWeight = 0.4f;
    public float leftShoulderAngleOffset = 0f;

    [Header("Upper Body — Right Shoulder")]
    public Transform rightShoulderTransform;
    [Range(0f, 1f)] public float rightShoulderWeight = 0.4f;
    public float rightShoulderAngleOffset = 0f;

    // Exposed for NetworkPlayer sync
    [HideInInspector] public float animHorizontal;
    [HideInInspector] public float animVertical;
    [HideInInspector] public bool animIsRunning;
    [HideInInspector] public bool animIsWalking;
    [HideInInspector] public bool animIsIdle;
    [HideInInspector] public bool animIsJumping;
    [HideInInspector] public bool animIsCrouching;
    [HideInInspector] public bool animIsAirborne;
    [HideInInspector] public bool animIsCrouchWalking;
    [HideInInspector] public bool animIsAiming;

    // Animator parameter hashes
    private static readonly int H          = Animator.StringToHash("Horizontal");
    private static readonly int V          = Animator.StringToHash("Vertical");
    private static readonly int IsRunning  = Animator.StringToHash("isRunning");
    private static readonly int IsWalking  = Animator.StringToHash("isWalking");
    private static readonly int IsIdle     = Animator.StringToHash("isIdle");
    private static readonly int IsJumping        = Animator.StringToHash("isJumping");
    private static readonly int IsAirborne       = Animator.StringToHash("isAirborne");
    private static readonly int IsCrouch          = Animator.StringToHash("isCrouching");
    private static readonly int IsCrouchWalking   = Animator.StringToHash("isCrouchWalking");
    private static readonly int IsAiming          = Animator.StringToHash("isAiming");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
            Debug.LogError("[PlayerController] No Animator found in children!");
        else
            Debug.Log($"[PlayerController] OnEnable — animator found on {animator.gameObject.name}, controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");

        animIsIdle = true;
        yVelocity = -2f;
        if (bodyCollider != null)
        {
            colliderHeight  = bodyCollider.height;
            colliderRadius  = bodyCollider.radius;
            colliderCenter  = bodyCollider.center;
            colliderXRot    = bodyCollider.transform.localEulerAngles.x;
        }
        cameraTarget = transform.Find("TPSCamera");
        fpsCamera = transform.Find("FPSCamera");
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

    private Transform fpsCamera;

    void HandleLook()
    {
        if (cameraTarget == null) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.1f;
        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -70f, 70f);
        cameraTarget.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (fpsCamera != null)
            fpsCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    void HandleMovement()
    {
        var keyboard = Keyboard.current;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            animIsAiming = !animIsAiming;

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
        animIsAirborne = !grounded;

        if (grounded) isAirborneFromJump = false;

        // while airborne from a jump, freeze crouch state to jump-time snapshot
        if (isAirborneFromJump)
        {
            animIsCrouching     = jumpWasCrouching;
            animIsCrouchWalking = jumpWasCrouchWalking;
        }

        // jumpLock: suppress movement bools for 0.15s after jumping to prevent mid-air state flicker
        if (jumpLock)
        {
            jumpLockTimer -= Time.deltaTime;
            if (jumpLockTimer <= 0f) jumpLock = false;
            animIsRunning = false;
            animIsWalking = false;
            animIsIdle    = false;
        }

        // --- Fix 1: only go idle after 0.2s of no input ---
        if (inputMag < 0.1f && !isCrouching && grounded)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= 0.2f)
            {
                animIsRunning = false;
                animIsWalking = false;
                animIsIdle    = true;
            }
            else
            {
                animIsIdle = false;
            }
        }
        else
        {
            idleTimer = 0f;
        }

        if (grounded)
        {
            yVelocity = -2f;

            if (keyboard.spaceKey.wasPressedThisFrame && !isCrouching)
            {
                yVelocity             = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animIsJumping         = true;
                jumpLock              = true;
                jumpLockTimer         = 0.15f;
                jumpWasCrouching      = false;
                jumpWasCrouchWalking  = false;
                isAirborneFromJump    = true;
                if (animator != null) animator.SetTrigger(IsJumping);
            }
            else if (keyboard.spaceKey.wasPressedThisFrame && isCrouching)
            {
                yVelocity             = Mathf.Sqrt(crouchJumpHeight * -2f * gravity);
                animIsJumping         = true;
                jumpLock              = true;
                jumpLockTimer         = 0.15f;
                jumpWasCrouching      = true;
                jumpWasCrouchWalking  = animIsCrouchWalking;
                isAirborneFromJump    = true;
                if (animator != null) animator.SetTrigger(IsJumping);
            }
        }

        yVelocity += gravity * Time.deltaTime;

        if (bodyCollider != null)
        {
            bool isAirborne = !grounded;
            float targetColliderHeight   = isCrouching ? (animIsCrouchWalking ? 1.3f : 1.1f) : (isAirborne ? 1.3f : 1.6f);
            float targetColliderRadius   = isCrouching ? (animIsCrouchWalking ? 0.3f : 0.4f) : (isAirborne ? 0.3f : 0.25f);
            Vector3 targetColliderCenter = isCrouching ? (animIsCrouchWalking ? new Vector3(0f, -0.2f, 0f) : new Vector3(0f, -0.3f, 0f)) : (isAirborne ? new Vector3(0f, -0.1f, 0f) : new Vector3(0f, -0.05f, 0f));
            float targetColliderXRot     = 0f;

            colliderHeight = Mathf.SmoothDamp(colliderHeight, targetColliderHeight, ref colliderHeightVel, colliderSmoothTime);
            colliderRadius = Mathf.SmoothDamp(colliderRadius, targetColliderRadius, ref colliderRadiusVel, colliderSmoothTime);
            colliderCenter = new Vector3(
                Mathf.SmoothDamp(colliderCenter.x, targetColliderCenter.x, ref colliderCenterVel.x, colliderSmoothTime),
                Mathf.SmoothDamp(colliderCenter.y, targetColliderCenter.y, ref colliderCenterVel.y, colliderSmoothTime),
                Mathf.SmoothDamp(colliderCenter.z, targetColliderCenter.z, ref colliderCenterVel.z, colliderSmoothTime)
            );
            colliderXRot = Mathf.SmoothDamp(colliderXRot, targetColliderXRot, ref colliderXRotVel, colliderSmoothTime);

            bodyCollider.height = colliderHeight;
            bodyCollider.radius = colliderRadius;
            bodyCollider.center = colliderCenter;
            bodyCollider.transform.localEulerAngles = new Vector3(colliderXRot, bodyCollider.transform.localEulerAngles.y, bodyCollider.transform.localEulerAngles.z);
        }

        Vector3 moveDir = transform.right * h + transform.forward * v;
        cc.Move(moveDir.normalized * currentSpeed * speedMultiplier * Time.deltaTime + Vector3.up * yVelocity * Time.deltaTime);

        ApplyAnimatorState();
    }

    void ApplyAnimatorState()
    {
        if (animator == null) return;

        animator.SetFloat(H,         animHorizontal);
        animator.SetFloat(V,         animVertical);
        animator.SetBool(IsRunning,  animIsRunning);
        animator.SetBool(IsWalking,  animIsWalking);
        animator.SetBool(IsIdle,     animIsIdle);
        animator.SetBool(IsAirborne,      animIsAirborne);
        animator.SetBool(IsCrouch,        animIsCrouching);
        animator.SetBool(IsCrouchWalking, animIsCrouchWalking);
        animator.SetBool(IsAiming,        animIsAiming);
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
