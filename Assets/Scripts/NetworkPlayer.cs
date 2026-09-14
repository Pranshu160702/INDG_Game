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
    public void TakeDamage(float damage, string killerName = "")
    {
        if (isDead) return;
        health = Mathf.Max(0f, health - damage);
        if (health <= 0f)
        {
            isDead = true;
            lastKillerName = killerName;

            // Find killer and increment their kill count
            foreach (var identity in NetworkServer.spawned.Values)
            {
                NetworkPlayer np = identity.GetComponent<NetworkPlayer>();
                if (np != null && identity.name == killerName)
                {
                    np.kills++;
                    if (GameManager.instance != null)
                        GameManager.instance.RegisterKill(killerName, np.kills);
                    break;
                }
            }

            RpcOnDied(killerName, netIdentity.name);
        }
    }

    [ClientRpc]
    void RpcOnDied(string killer, string victim)
    {
        GameHUD.AddKillFeed(killer, victim);
        if (!isLocalPlayer) return;
        if (controller != null) controller.enabled = false;
        if (GameHUD.instance != null) GameHUD.instance.ShowRespawnScreen(5f);
        StartCoroutine(RespawnRoutine(5f));
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
        transform.position = spawnPos;
        if (!isLocalPlayer) return;
        if (controller != null) controller.enabled = true;
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

    private Transform cameraTarget;
    private PlayerController controller;
    private Animator animator;
    private Camera fpsCamera;
    private Camera tpsCamera;
    private bool isFPS = true;

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

    void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
        Debug.Log($"[NetworkPlayer] Start — animator={(animator != null ? animator.gameObject.name : "NULL")}, isLocalPlayer={isLocalPlayer}");
        if (animator == null)
            Debug.LogError("[NetworkPlayer] No Animator found in children!");
        else
            Debug.Log($"[NetworkPlayer] Animator found: controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}, avatar={(animator.avatar != null ? animator.avatar.name : "NULL")}, enabled={animator.enabled}, isHuman={animator.isHuman}");
    }

    [Header("Debug")]
    public bool hideLocalBody = false;

    private GunScript gunScript;

    public override void OnStartLocalPlayer()
    {
        if (controller != null) controller.enabled = true;

        if (hideLocalBody)
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

        cameraTarget = transform.Find("TPSCamera");
        tpsCamera = cameraTarget?.GetComponent<Camera>();
        fpsCamera = transform.Find("FPSCamera")?.GetComponent<Camera>();

        if (tpsCamera != null) tpsCamera.enabled = false;
        if (fpsCamera != null) fpsCamera.enabled = true;

        // Re-enable the audio listener on the local player's FPS camera
        var al = fpsCamera?.GetComponent<AudioListener>();
        if (al != null) al.enabled = true;

        gunScript = GetComponentInChildren<GunScript>(true);
        if (gunScript != null)
        {
            gunScript.shootCamera = fpsCamera;
            gunScript.isOwnedLocally = true;
        }

        if (GameHUD.instance != null)
        {
            GameHUD.instance.networkPlayer = this;
            GameHUD.instance.gunScript = gunScript;
        }
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
        if (gunScript != null)
            gunScript.shootCamera = isFPS ? fpsCamera : tpsCamera;
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
            controller.animIsCrouching != syncCrouching   ||
            controller.animIsCrouchWalking != syncCrouchWalking;

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
                controller.animIsCrouchWalking
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

        if (cameraTarget == null) cameraTarget = transform.Find("TPSCamera");
        if (cameraTarget != null)
            cameraTarget.localRotation = Quaternion.Lerp(
                cameraTarget.localRotation,
                Quaternion.Euler(syncHeadXRot, 0f, 0f),
                Time.deltaTime * 15f
            );

        if (animator != null)
        {
            animator.SetFloat("Horizontal",    syncAnimH);
            animator.SetFloat("Vertical",      syncAnimV);
            animator.SetBool("isRunning",      syncRunning);
            animator.SetBool("isWalking",      syncWalking);
            animator.SetBool("isIdle",         syncIdle);
            animator.SetBool("isJumping",      syncJumping);
            animator.SetBool("isCrouching",    syncCrouching);
            animator.SetBool("isCrouchWalking",syncCrouchWalking);
        }
    }

    [Command]
    void CmdSyncState(
        Vector3 pos, float yRot, float headXRot,
        float animH, float animV,
        bool running, bool walking, bool idle,
        bool jumping, bool crouching, bool crouchWalking)
    {
        syncPos          = pos;
        syncYRot         = yRot;
        syncHeadXRot     = headXRot;
        syncAnimH        = animH;
        syncAnimV        = animV;
        syncRunning      = running;
        syncWalking      = walking;
        syncIdle         = idle;
        syncJumping      = jumping;
        syncCrouching    = crouching;
        syncCrouchWalking= crouchWalking;
    }
}
