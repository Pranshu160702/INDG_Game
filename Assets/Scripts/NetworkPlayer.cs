using UnityEngine;
using Mirror;

public class NetworkPlayer : NetworkBehaviour
{
    // --- Transform sync ---
    [SyncVar] private Vector3 syncPos;
    [SyncVar] private float syncYRot;
    [SyncVar] private float syncHeadXRot;

    // --- Animator sync ---
    [SyncVar] private float syncAnimH;
    [SyncVar] private float syncAnimV;
    [SyncVar] private bool syncRunning;
    [SyncVar] private bool syncWalking;
    [SyncVar] private bool syncIdle;
    [SyncVar] private bool syncJumping;
    [SyncVar] private bool syncCrouching;

    private Transform cameraTarget;
    private PlayerController controller;
    private Camera fpsCamera;

    private Vector3 lastPos;
    private float lastYRot;
    private float lastHeadXRot;
    private const float SyncThreshold = 0.01f;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        foreach (var cam in GetComponentsInChildren<Camera>(true))
            cam.enabled = false;
        foreach (var al in GetComponentsInChildren<AudioListener>(true))
            al.enabled = false;
    }

    [Header("Debug")]
    public bool hideLocalBody = false;

    public override void OnStartLocalPlayer()
    {
        if (controller != null) controller.enabled = true;

        if (hideLocalBody)
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

        cameraTarget = transform.Find("CameraTarget");
        if (cameraTarget == null)
        {
            StartCoroutine(SetupCameraNextFrame());
            return;
        }
        SetupFPSCamera();
    }

    private System.Collections.IEnumerator SetupCameraNextFrame()
    {
        yield return null;
        cameraTarget = transform.Find("CameraTarget");
        SetupFPSCamera();
    }

    private void SetupFPSCamera()
    {
        if (cameraTarget == null) return;

        GameObject camObj = new GameObject("FPSCamera");
        camObj.transform.SetParent(cameraTarget);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;

        fpsCamera = camObj.AddComponent<Camera>();
        fpsCamera.enabled = true;
        camObj.AddComponent<AudioListener>();
    }

    void Update()
    {
        if (isLocalPlayer)
            SyncLocalPlayer();
        else
            ApplyRemoteState();
    }

    void SyncLocalPlayer()
    {
        if (controller == null) return;

        float headX = cameraTarget != null ? cameraTarget.localEulerAngles.x : 0f;
        float yRot  = transform.eulerAngles.y;
        Vector3 pos = transform.position;

        bool transformChanged =
            Vector3.Distance(pos, lastPos) > SyncThreshold ||
            Mathf.Abs(yRot - lastYRot) > SyncThreshold ||
            Mathf.Abs(headX - lastHeadXRot) > SyncThreshold;

        bool animChanged =
            Mathf.Abs(controller.animHorizontal - syncAnimH) > 0.01f ||
            Mathf.Abs(controller.animVertical   - syncAnimV) > 0.01f ||
            controller.animIsRunning   != syncRunning   ||
            controller.animIsWalking   != syncWalking   ||
            controller.animIsIdle      != syncIdle      ||
            controller.animIsJumping   != syncJumping   ||
            controller.animIsCrouching != syncCrouching;

        if ((transformChanged || animChanged) && NetworkClient.active)
        {
            lastPos      = pos;
            lastYRot     = yRot;
            lastHeadXRot = headX;

            CmdSyncState(
                pos, yRot, headX,
                controller.animHorizontal,
                controller.animVertical,
                controller.animIsRunning,
                controller.animIsWalking,
                controller.animIsIdle,
                controller.animIsJumping,
                controller.animIsCrouching
            );
        }
    }

    void ApplyRemoteState()
    {
        transform.position = Vector3.Lerp(transform.position, syncPos, Time.deltaTime * 15f);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, syncYRot, 0f),
            Time.deltaTime * 15f
        );

        if (cameraTarget == null) cameraTarget = transform.Find("CameraTarget");
        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Lerp(
                cameraTarget.localRotation,
                Quaternion.Euler(syncHeadXRot, 0f, 0f),
                Time.deltaTime * 15f
            );

        if (controller != null)
            controller.ApplyRemoteAnimState(
                syncAnimH, syncAnimV,
                syncRunning, syncWalking, syncIdle,
                syncJumping, syncCrouching
            );
    }

    [Command]
    void CmdSyncState(
        Vector3 pos, float yRot, float headXRot,
        float animH, float animV,
        bool running, bool walking, bool idle,
        bool jumping, bool crouching)
    {
        syncPos       = pos;
        syncYRot      = yRot;
        syncHeadXRot  = headXRot;
        syncAnimH     = animH;
        syncAnimV     = animV;
        syncRunning   = running;
        syncWalking   = walking;
        syncIdle      = idle;
        syncJumping   = jumping;
        syncCrouching = crouching;
    }
}
