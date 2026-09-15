using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class NetworkPlayer : NetworkBehaviour
{
    // --- Health & Death ---
    [SyncVar(hook = nameof(OnHealthChanged))] private float health = 100f;
    private const float MaxHealth = 100f;
    [SyncVar] public bool isDead = false;
    [SyncVar] public int kills = 0;
    private string lastKillerName = "";
    public float HealthPct => health / MaxHealth;

    [Server]
    public void TakeDamage(float damage, string killerName = "", uint killerNetId = 0)
    {
        if (isDead) return;
        health = Mathf.Max(0f, health - damage);
        if (health <= 0f)
        {
            isDead = true;
            lastKillerName = killerName;

            if (killerNetId != 0 && NetworkServer.spawned.TryGetValue(killerNetId, out NetworkIdentity killerIdentity))
            {
                NetworkPlayer np = killerIdentity.GetComponent<NetworkPlayer>();
                if (np != null)
                {
                    np.kills++;
                    if (GameManager.instance != null)
                        GameManager.instance.RegisterKill(killerName, np.kills);
                }
            }

            RpcOnDied(killerName, netIdentity.name);
        }
    }

    private Coroutine respawnCoroutine;

    [ClientRpc]
    void RpcOnDied(string killer, string victim)
    {
        GameHUD.AddKillFeed(killer, victim);
        if (!isLocalPlayer) return;
        if (controller != null) controller.enabled = false;
        if (fpsArmsRoot != null) fpsArmsRoot.SetActive(false);
        if (GameHUD.instance != null) GameHUD.instance.ShowRespawnScreen(5f);
        if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
        respawnCoroutine = StartCoroutine(RespawnRoutine(5f));
    }

    IEnumerator RespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isLocalPlayer)
        {
            CmdRespawn();
        }
    }

    [Command]
    void CmdRespawn()
    {
        Transform spawn = SpawnManager.instance != null
            ? SpawnManager.instance.GetBestSpawn()
            : null;
        Vector3 spawnPos = spawn != null ? spawn.position : Vector3.up;
        ServerRespawn(spawnPos);
    }

    [ClientRpc]
    void RpcRespawn(Vector3 spawnPos)
    {
        // Disable CC before teleporting — it blocks position changes
        if (characterController != null) characterController.enabled = false;
        transform.position = spawnPos;
        if (characterController != null) characterController.enabled = true;
        lastSyncJumping = false;

        if (!isLocalPlayer) return;
        if (controller != null) controller.enabled = true;
        if (gunScript != null) gunScript.ResetState();
        if (fpsArmsRoot != null)
        {
            fpsArmsRoot.SetActive(isFPS);
            if (isFPS) fpsArmsAnimator?.PlayReady();
        }
        if (GameHUD.instance != null) GameHUD.instance.HideRespawnScreen();
    }

    [Server]
    void ServerRespawn(Vector3 spawnPos)
    {
        health = MaxHealth;
        isDead = false;
        RpcRespawn(spawnPos);
    }

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (!isLocalPlayer || GameHUD.instance == null) return;
        if (newHealth < oldHealth) GameHUD.instance.ShowBloodHit();
    }

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
    [SyncVar] private bool syncCrouchWalking;
    [SyncVar] private bool syncFiring;
    [SyncVar] private bool syncReloading;
    [SyncVar] private bool syncMelee;

    private Transform cameraTarget;
    private PlayerController controller;
    private Animator animator;
    private Camera fpsCamera;
    private Camera tpsCamera;
    private bool isFPS = true;

    // FPS Arms
    private GameObject fpsArmsRoot;
    private FPSArmsAnimator fpsArmsAnimator;
    private SpineAimOffsetSync spineAimSync;

    private bool lastSyncJumping = false;

    // Layer indices — set from layer names at runtime
    private int fpsArmsLayer;

    private Vector3 lastPos;
    private float lastYRot;
    private float lastHeadXRot;
    private const float SyncThreshold = 0.01f;

    private CharacterController characterController;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;
        characterController = GetComponent<CharacterController>();

        foreach (var cam in GetComponentsInChildren<Camera>(true))
            cam.enabled = false;
        foreach (var al in GetComponentsInChildren<AudioListener>(true))
            al.enabled = false;
    }

    void Start()
    {
        var commando = transform.Find("Commando");
        if (commando != null)
            animator = commando.GetComponentInChildren<Animator>(true);

        if (animator == null)
            Debug.LogError("[NetworkPlayer] No Animator found on Commando child!");

        // For remote players, initial SyncVar values are set before Start
        if (!isLocalPlayer)
            hasReceivedFirstSync = true;
    }

    [Header("Debug")]
    public bool hideLocalBody = false;

    private GunScript gunScript;

    public override void OnStartLocalPlayer()
    {
        if (controller != null) controller.enabled = true;

        fpsArmsLayer  = LayerMask.NameToLayer("FPSArms");
        int playerBodyLayer = LayerMask.NameToLayer("PlayerBody");

        cameraTarget = transform.Find("TPSCamera");
        tpsCamera = cameraTarget?.GetComponent<Camera>();
        fpsCamera = transform.Find("FPSCamera")?.GetComponent<Camera>();

        if (tpsCamera != null) tpsCamera.enabled = false;
        if (fpsCamera != null)
        {
            fpsCamera.enabled = true;
            // FPS cam: hide PlayerBody layer only (map/world stays visible)
            if (playerBodyLayer >= 0)
                fpsCamera.cullingMask &= ~(1 << playerBodyLayer);
        }

        // TPS cam: see everything EXCEPT FPS arms
        if (tpsCamera != null && fpsArmsLayer >= 0)
            tpsCamera.cullingMask &= ~(1 << fpsArmsLayer);

        // Move body renderers to a layer the FPS cam won't see
        SetBodyLayerForLocalPlayer();

        // Re-enable the audio listener on the local player's FPS camera
        var al = fpsCamera?.GetComponent<AudioListener>();
        if (al != null) al.enabled = true;

        // Find FPS arms — search all children of FPSCamera, not just direct child
        var fpsCamTransform = transform.Find("FPSCamera");
        if (fpsCamTransform != null)
        {
            fpsArmsAnimator = fpsCamTransform.GetComponentInChildren<FPSArmsAnimator>(true);
            Debug.Log("[NetworkPlayer] FPSCamera found. FPSArmsAnimator found: " + (fpsArmsAnimator != null));
            if (fpsArmsAnimator != null)
            {
                Transform t = fpsArmsAnimator.transform;
                while (t.parent != fpsCamTransform && t.parent != null)
                    t = t.parent;
                fpsArmsRoot = t.gameObject;

                fpsArmsRoot.SetActive(true);
                SetLayerRecursive(fpsArmsRoot, fpsArmsLayer >= 0 ? fpsArmsLayer : 0);
                Debug.Log("[NetworkPlayer] FPSArms root: " + fpsArmsRoot.name + " | calling PlayReady");
                StartCoroutine(PlayReadyNextFrame());
            }
            else
            {
                Debug.LogWarning("[NetworkPlayer] FPSArmsAnimator not found under FPSCamera.");
            }
        }
        else
        {
            Debug.LogError("[NetworkPlayer] FPSCamera not found on " + gameObject.name);
        }

        gunScript = GetComponentInChildren<GunScript>(true);
        Debug.Log("[NetworkPlayer] GunScript found: " + (gunScript != null) + " | fpsArmsAnimator: " + (fpsArmsAnimator != null) + " | playerController: " + (controller != null));
        if (gunScript != null)
        {
            gunScript.shootCamera    = fpsCamera;
            gunScript.isOwnedLocally = true;
            gunScript.fpsArmsAnimator  = fpsArmsAnimator;
            gunScript.playerController = controller;
        }

        // Wire spine aim sync to FPS camera
        spineAimSync = GetComponentInChildren<SpineAimOffsetSync>(true);
        if (spineAimSync != null)
            spineAimSync.SetAimCamera(fpsCamera);

        if (GameHUD.instance != null)
        {
            GameHUD.instance.networkPlayer = this;
            GameHUD.instance.gunScript = gunScript;
        }
    }

    IEnumerator PlayReadyNextFrame()
    {
        yield return null; // wait one frame for animator to initialize
        fpsArmsAnimator?.PlayReady();
    }

    void SetBodyLayerForLocalPlayer()
    {
        int playerBodyLayer = LayerMask.NameToLayer("PlayerBody");
        if (playerBodyLayer < 0) { Debug.LogWarning("[NetworkPlayer] PlayerBody layer not found. Run INDG/Setup/Add FPSArms Layer."); return; }

        // Move the Commando child (body + TPS gun) to PlayerBody so FPS cam won't render it
        var commando = transform.Find("Commando");
        if (commando != null)
            SetLayerRecursive(commando.gameObject, playerBodyLayer);

        if (hideLocalBody)
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            SyncLocalPlayer();
            if (Keyboard.current != null && Keyboard.current.leftAltKey.wasPressedThisFrame)
                ToggleCamera();
        }
        else
            ApplyRemoteState();
    }

    void ToggleCamera()
    {
        if (tpsCamera == null || fpsCamera == null) return;
        isFPS = !isFPS;
        tpsCamera.enabled = !isFPS;
        fpsCamera.enabled = isFPS;
        if (fpsArmsRoot != null) fpsArmsRoot.SetActive(isFPS);
        if (spineAimSync != null) spineAimSync.SetAimCamera(isFPS ? fpsCamera : tpsCamera);
        if (gunScript != null)
            gunScript.shootCamera = isFPS ? fpsCamera : tpsCamera;
    }

    void SyncLocalPlayer()
    {
        if (controller == null || isDead) return;

        float headX = cameraTarget != null ? cameraTarget.localEulerAngles.x : 0f;
        float yRot  = transform.eulerAngles.y;
        Vector3 pos = transform.position;

        bool transformChanged =
            Vector3.Distance(pos, lastPos) > SyncThreshold ||
            Mathf.Abs(Mathf.DeltaAngle(yRot, lastYRot)) > SyncThreshold ||
            Mathf.Abs(Mathf.DeltaAngle(headX, lastHeadXRot)) > SyncThreshold;

        bool animChanged =
            Mathf.Abs(controller.animHorizontal - syncAnimH) > 0.01f ||
            Mathf.Abs(controller.animVertical   - syncAnimV) > 0.01f ||
            controller.animIsRunning       != syncRunning    ||
            controller.animIsWalking       != syncWalking    ||
            controller.animIsIdle          != syncIdle       ||
            controller.animIsJumping       != syncJumping    ||
            controller.animIsCrouching     != syncCrouching  ||
            controller.animIsCrouchWalking != syncCrouchWalking ||
            controller.animIsFiring        != syncFiring     ||
            controller.animIsReloading     != syncReloading  ||
            controller.animIsMelee         != syncMelee;

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
                controller.animIsCrouching,
                controller.animIsCrouchWalking,
                controller.animIsFiring,
                controller.animIsReloading,
                controller.animIsMelee
            );
        }
    }

    private bool hasReceivedFirstSync = false;

    void ApplyRemoteState()
    {
        if (!hasReceivedFirstSync) return;

        transform.position = Vector3.Lerp(transform.position, syncPos, Time.deltaTime * 15f);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, syncYRot, 0f),
            Time.deltaTime * 15f
        );

        if (cameraTarget == null) cameraTarget = transform.Find("TPSCamera");
        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Lerp(
                cameraTarget.localRotation,
                Quaternion.Euler(syncHeadXRot, 0f, 0f),
                Time.deltaTime * 15f
            );

        if (animator != null)
        {
            animator.SetFloat("Horizontal",     syncAnimH);
            animator.SetFloat("Vertical",       syncAnimV);
            animator.SetBool("isRunning",       syncRunning);
            animator.SetBool("isWalking",       syncWalking);
            animator.SetBool("isIdle",          syncIdle);
            animator.SetBool("isCrouching",     syncCrouching);
            animator.SetBool("isCrouchWalking", syncCrouchWalking);
            animator.SetBool("isFiring",        syncFiring);
            animator.SetBool("isReloading",     syncReloading);
            animator.SetBool("isMelee",         syncMelee);
            // isJumping is a Trigger in the controller — only fire on rising edge
            if (syncJumping && !lastSyncJumping)
                animator.SetTrigger("isJumping");
            lastSyncJumping = syncJumping;
        }
    }

    [Command]
    void CmdSyncState(
        Vector3 pos, float yRot, float headXRot,
        float animH, float animV,
        bool running, bool walking, bool idle,
        bool jumping, bool crouching, bool crouchWalking,
        bool firing, bool reloading, bool melee)
    {
        syncPos           = pos;
        syncYRot          = yRot;
        syncHeadXRot      = headXRot;
        syncAnimH         = animH;
        syncAnimV         = animV;
        syncRunning       = running;
        syncWalking       = walking;
        syncIdle          = idle;
        syncJumping       = jumping;
        syncCrouching     = crouching;
        syncCrouchWalking = crouchWalking;
        syncFiring        = firing;
        syncReloading     = reloading;
        syncMelee         = melee;
    }
}
