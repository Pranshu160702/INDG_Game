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
            if (isFPS) fpsArmsAnimator?.PlayEquip();
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
    [SyncVar] private bool syncReloadingEmpty;
    [SyncVar] private bool syncEquipping;
    [SyncVar] private bool syncMelee1;
    [SyncVar] private bool syncMelee2;
    [SyncVar] private bool syncMelee3;
    [SyncVar] private bool syncMelee4;

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

    private GunScript      gunScript;
    private TPSArmsAnimator tpsArmsAnimator;

    public void SwitchGun(int index)
    {
        if (fpsArmsAnimator == null) return;
        fpsArmsAnimator.SwitchGun(index);
        tpsArmsAnimator?.SwitchGun(index);
        if (gunScript != null && fpsArmsAnimator.ActiveEntry != null)
        {
            gunScript.data = fpsArmsAnimator.ActiveEntry.data;
            gunScript.InitAmmo();
        }
        fpsArmsAnimator.PlayEquip();
        if (controller != null) StartCoroutine(EquipAnimRoutine());
    }

    IEnumerator EquipAnimRoutine()
    {
        controller.animIsEquipping = true;
        // Hold equipping true for a fixed window — equip anims are typically ~0.6s
        yield return new WaitForSeconds(0.6f);
        controller.animIsEquipping = false;
    }

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
            if (fpsArmsAnimator != null)
            {
                Transform t = fpsArmsAnimator.transform;
                while (t.parent != fpsCamTransform && t.parent != null)
                    t = t.parent;
                fpsArmsRoot = t.gameObject;

                fpsArmsRoot.SetActive(true);
                SetLayerRecursive(fpsArmsRoot, fpsArmsLayer >= 0 ? fpsArmsLayer : 0);
                StartCoroutine(PlayReadyNextFrame());
            }
        }
        else
        {
            Debug.LogError("[NetworkPlayer] FPSCamera not found on " + gameObject.name);
        }

        gunScript = GetComponentInChildren<GunScript>(true);
        if (gunScript != null)
        {
            gunScript.shootCamera      = fpsCamera;
            gunScript.isOwnedLocally   = true;
            gunScript.fpsArmsAnimator  = fpsArmsAnimator;
            gunScript.playerController = controller;
            if (fpsArmsAnimator != null && fpsArmsAnimator.ActiveEntry != null)
                gunScript.data = fpsArmsAnimator.ActiveEntry.data;
        }

        // Find TPSArmsAnimator on the Commando child
        var commando = transform.Find("Commando");
        if (commando != null)
            tpsArmsAnimator = commando.GetComponent<TPSArmsAnimator>();

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
        yield return null;
        fpsArmsAnimator?.PlayEquip();
        if (controller != null) StartCoroutine(EquipAnimRoutine());
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
            HandleWeaponScroll();
            if (Keyboard.current != null && Keyboard.current.leftAltKey.wasPressedThisFrame)
                ToggleCamera();
        }
        else
            ApplyRemoteState();
    }

    void HandleWeaponScroll()
    {
        if (fpsArmsAnimator == null || Mouse.current == null) return;
        if (gunScript != null && gunScript.IsBlocked) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0f) return;

        int count = fpsArmsAnimator.guns.Length;
        int next  = (fpsArmsAnimator.ActiveIndex + (scroll > 0f ? -1 : 1) + count) % count;
        SwitchGun(next);
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
            controller.animIsFiring        != syncFiring        ||
            controller.animIsReloading     != syncReloading     ||
            controller.animIsReloadingEmpty!= syncReloadingEmpty||
            controller.animIsEquipping     != syncEquipping     ||
            controller.animIsMelee1        != syncMelee1        ||
            controller.animIsMelee2        != syncMelee2        ||
            controller.animIsMelee3        != syncMelee3        ||
            controller.animIsMelee4        != syncMelee4;

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
                controller.animIsReloadingEmpty,
                controller.animIsEquipping,
                controller.animIsMelee1,
                controller.animIsMelee2,
                controller.animIsMelee3,
                controller.animIsMelee4
            );
        }
    }

    // Pre-hashed animator parameter IDs — no string lookups per frame
    private static readonly int AnimH             = Animator.StringToHash("Horizontal");
    private static readonly int AnimV             = Animator.StringToHash("Vertical");
    private static readonly int AnimRunning       = Animator.StringToHash("isRunning");
    private static readonly int AnimWalking       = Animator.StringToHash("isWalking");
    private static readonly int AnimIdle          = Animator.StringToHash("isIdle");
    private static readonly int AnimJumping       = Animator.StringToHash("isJumping");
    private static readonly int AnimCrouching     = Animator.StringToHash("isCrouching");
    private static readonly int AnimCrouchWalking = Animator.StringToHash("isCrouchWalking");
    private static readonly int AnimFiring        = Animator.StringToHash("isFiring");
    private static readonly int AnimReloading     = Animator.StringToHash("isReloading");
    private static readonly int AnimReloadEmpty   = Animator.StringToHash("isReloadingEmpty");
    private static readonly int AnimEquipping     = Animator.StringToHash("isEquipping");
    private static readonly int AnimMelee1        = Animator.StringToHash("isMelee1");
    private static readonly int AnimMelee2        = Animator.StringToHash("isMelee2");
    private static readonly int AnimMelee3        = Animator.StringToHash("isMelee3");
    private static readonly int AnimMelee4        = Animator.StringToHash("isMelee4");

    private bool hasReceivedFirstSync = false;

    void ApplyRemoteState()
    {
        if (!hasReceivedFirstSync) return;

        float dt = Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, syncPos, dt * 15f);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, syncYRot, 0f), dt * 15f);

        if (cameraTarget == null) cameraTarget = transform.Find("TPSCamera");
        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Lerp(
                cameraTarget.localRotation,
                Quaternion.Euler(syncHeadXRot, 0f, 0f),
                dt * 15f
            );

        if (animator != null)
        {
            animator.SetFloat(AnimH,             syncAnimH);
            animator.SetFloat(AnimV,             syncAnimV);
            animator.SetBool(AnimRunning,        syncRunning);
            animator.SetBool(AnimWalking,        syncWalking);
            animator.SetBool(AnimIdle,           syncIdle);
            animator.SetBool(AnimCrouching,      syncCrouching);
            animator.SetBool(AnimCrouchWalking,  syncCrouchWalking);
            animator.SetBool(AnimFiring,       syncFiring);
            animator.SetBool(AnimReloading,    syncReloading);
            animator.SetBool(AnimReloadEmpty,  syncReloadingEmpty);
            animator.SetBool(AnimEquipping,    syncEquipping);
            animator.SetBool(AnimMelee1,       syncMelee1);
            animator.SetBool(AnimMelee2,       syncMelee2);
            animator.SetBool(AnimMelee3,       syncMelee3);
            animator.SetBool(AnimMelee4,       syncMelee4);
            if (syncJumping && !lastSyncJumping)
                animator.SetTrigger(AnimJumping);
            lastSyncJumping = syncJumping;
        }
    }

    [Command]
    void CmdSyncState(
        Vector3 pos, float yRot, float headXRot,
        float animH, float animV,
        bool running, bool walking, bool idle,
        bool jumping, bool crouching, bool crouchWalking,
        bool firing, bool reloading, bool reloadingEmpty,
        bool equipping, bool melee1, bool melee2, bool melee3, bool melee4)
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
        syncReloadingEmpty= reloadingEmpty;
        syncEquipping     = equipping;
        syncMelee1        = melee1;
        syncMelee2        = melee2;
        syncMelee3        = melee3;
        syncMelee4        = melee4;
    }
}
