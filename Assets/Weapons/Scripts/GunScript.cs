using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(AudioSource))]
public class GunScript : NetworkBehaviour
{
    [Header("Data")]
    public GunData data;

    [Header("References")]
    public Transform    muzzlePoint;
    public GameObject   muzzleFlashPrefab;
    public GameObject   bulletImpactFlesh;
    public GameObject   bulletImpactDefault;
    public GameObject   cartridgeEjectPrefab;

    // ── Public state ─────────────────────────────────────────────
    public int  BulletsLeft => bulletsLeft;
    public int  ReserveAmmo => reserveAmmo;
    public bool IsBlocked   => reloading || meleeing;

    // ── Private state ────────────────────────────────────────────
    private int  bulletsLeft;
    private int  reserveAmmo;
    private bool readyToShoot = true;
    private bool reloading    = false;
    private bool meleeing     = false;

    // Spread — tracked cleanly as elapsed fire time
    private float continuousFireTime = 0f;
    private float currentSpread      = 0f;
    private bool  wasFiringLastFrame = false;

    // Pools
    private readonly Queue<LineRenderer> trailPool        = new Queue<LineRenderer>();
    private readonly Queue<GameObject>   muzzleFlashPool  = new Queue<GameObject>();
    private readonly Queue<GameObject>   cartridgePool    = new Queue<GameObject>();

    private AudioSource   audioSource;
    private NetworkPlayer ownerNetworkPlayer;

    [HideInInspector] public Camera           shootCamera;
    [HideInInspector] public FPSArmsAnimator  fpsArmsAnimator;
    [HideInInspector] public bool             isOwnedLocally = false;
    [HideInInspector] public PlayerController playerController;

    private Coroutine fireBurstCoroutine;
    private Coroutine reloadCoroutine;
    private Coroutine meleeCoroutine;

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        audioSource        = GetComponent<AudioSource>();
        ownerNetworkPlayer = GetComponentInParent<NetworkPlayer>();
    }

    public override void OnStartLocalPlayer()
    {
        // Ammo is initialised by NetworkPlayer after gun data is set via InitAmmo()
    }

    // Called by NetworkPlayer after setting data, on spawn and on every gun switch
    public void InitAmmo()
    {
        if (data == null) return;
        bulletsLeft = data.magazineSize;
        reserveAmmo = data.maxReserveAmmo;
    }

    void Update()
    {
        if (!isOwnedLocally) return;
        if (data == null) return;
        if (ownerNetworkPlayer != null && ownerNetworkPlayer.isDead) return;
        if (Mouse.current == null || Keyboard.current == null) return;

        bool wantsToFire    = Mouse.current.leftButton.isPressed;
        bool canFire        = readyToShoot && !reloading && !meleeing;
        bool isFiringNow    = wantsToFire && canFire && bulletsLeft > 0;

        // ── Spread tracking ──────────────────────────────────────
        if (isFiringNow)
        {
            continuousFireTime += Time.deltaTime;
            currentSpread       = data.GetSpread(continuousFireTime);
            wasFiringLastFrame  = true;
        }
        else
        {
            if (wasFiringLastFrame)
            {
                wasFiringLastFrame = false;
                // Don't reset continuousFireTime immediately — let recovery handle it
            }
            // Recover spread and fire time
            float recovery      = data.spreadRecoverySpeed * Time.deltaTime;
            continuousFireTime  = Mathf.Max(0f, continuousFireTime - recovery * data.maxSpreadTime);
            currentSpread       = data.GetSpread(continuousFireTime);
        }

        // ── Fire input ───────────────────────────────────────────
        if (wantsToFire && canFire)
        {
            // Knife routes left-click through melee combo, not FireBurst
            if (data.altDamage > 0)
            {
                meleeCoroutine = StartCoroutine(Melee());
            }
            else if (bulletsLeft > 0)
            {
                readyToShoot       = false;
                fireBurstCoroutine = StartCoroutine(FireBurst());
            }
            else if (CanReload())
            {
                reloadCoroutine = StartCoroutine(Reload());
            }
            else
            {
                PlayEmptyClick();
            }
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && !reloading && !meleeing && CanReload())
            reloadCoroutine = StartCoroutine(Reload());

        if (Keyboard.current.fKey.wasPressedThisFrame && !meleeing && !reloading)
            meleeCoroutine = StartCoroutine(Melee());

        // Knife right-click alt attack
        if (data.altDamage > 0 && Mouse.current.rightButton.wasPressedThisFrame && !meleeing && !reloading)
            meleeCoroutine = StartCoroutine(KnifeAlt());
    }

    // ── Firing ───────────────────────────────────────────────────

    IEnumerator FireBurst()
    {
        fpsArmsAnimator?.PlayFire();
        if (playerController != null) playerController.animIsFiring = true;

        for (int i = 0; i < data.bulletsPerTap; i++)
        {
            if (bulletsLeft <= 0) break;
            Fire();
            if (data.bulletsPerTap > 1)
                yield return new WaitForSeconds(data.timeBetweenBursts);
        }

        yield return new WaitForSeconds(data.fireRate);
        if (playerController != null) playerController.animIsFiring = false;
        readyToShoot = true;
    }

    void Fire()
    {
        bulletsLeft--;

        PlayLocalEffects(bulletsLeft == 0);

        if (shootCamera == null) return;

        int    ignoreSelfMask = ~LayerMask.GetMask("PlayerBody");
        Vector2 spreadOffset  = Random.insideUnitCircle * currentSpread;
        Vector3 viewportPoint = new Vector3(0.5f + spreadOffset.x, 0.5f + spreadOffset.y, 0f);
        Ray     ray           = shootCamera.ViewportPointToRay(viewportPoint);

        Vector3 hitPoint  = ray.origin + ray.direction * data.trailMissDistance;
        Vector3 hitNormal = -ray.direction;
        bool    hitPlayer = false;
        float   damage    = 0f;
        uint    targetId  = 0;

        if (Physics.Raycast(ray, out RaycastHit hit, data.range, ignoreSelfMask))
        {
            hitPoint  = hit.point;
            hitNormal = hit.normal;

            HitBox hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox != null && hitBox.owner != ownerNetworkPlayer)
            {
                float baseDmg = hitBox.type switch
                {
                    HitBoxType.Head => data.headshotDamage,
                    _               => data.bodyshotDamage
                };
                damage    = baseDmg * data.GetDamageMultiplier(hit.distance);
                targetId  = hitBox.owner.netId;
                hitPlayer = true;
            }
        }

        if (data.showBulletTrail && muzzlePoint != null)
            StartCoroutine(SpawnTrail(muzzlePoint.position, hitPoint));

        CmdRegisterHit(targetId, damage, hitPoint, hitNormal, hitPlayer);
    }

    // ── Effects — pooled ─────────────────────────────────────────

    void PlayLocalEffects(bool isLastBullet)
    {
        if (muzzlePoint != null)
        {
            if (muzzleFlashPrefab != null)
            {
                GameObject fx = GetFromPool(muzzleFlashPool, muzzleFlashPrefab);
                fx.transform.SetPositionAndRotation(muzzlePoint.position, muzzlePoint.rotation);
                fx.SetActive(true);
                ParticleSystem ps = fx.GetComponent<ParticleSystem>() ?? fx.GetComponentInChildren<ParticleSystem>();
                ps?.Play(true);
                StartCoroutine(ReturnToPool(fx, muzzleFlashPool, 0.3f));
            }

            if (cartridgeEjectPrefab != null)
            {
                GameObject c = GetFromPool(cartridgePool, cartridgeEjectPrefab);
                c.transform.SetPositionAndRotation(muzzlePoint.position, muzzlePoint.rotation);
                c.SetActive(true);
                StartCoroutine(ReturnToPool(c, cartridgePool, 2f));
            }
        }

        if (data.fireClips != null && data.fireClips.Length > 0)
        {
            AudioClip clip = (isLastBullet && data.lastBulletClip != null)
                ? data.lastBulletClip
                : data.fireClips[Random.Range(0, data.fireClips.Length)];
            audioSource.PlayOneShot(clip, data.audioVolume);
        }
    }

    void PlayEmptyClick()
    {
        if (data.emptyClip != null)
            audioSource.PlayOneShot(data.emptyClip, data.audioVolume);
    }

    // ── Object pool helpers ──────────────────────────────────────

    GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        while (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            if (obj != null) { obj.SetActive(false); return obj; }
        }
        return Instantiate(prefab);
    }

    IEnumerator ReturnToPool(GameObject obj, Queue<GameObject> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    // ── Trail ────────────────────────────────────────────────────

    IEnumerator SpawnTrail(Vector3 from, Vector3 to)
    {
        LineRenderer lr = GetTrailFromPool();
        lr.SetPosition(0, from);
        lr.SetPosition(1, from);
        lr.gameObject.SetActive(true);

        float distance  = Vector3.Distance(from, to);
        float travelled = 0f;

        while (travelled < distance)
        {
            travelled += data.trailSimulationSpeed * Time.deltaTime;
            lr.SetPosition(1, Vector3.Lerp(from, to, Mathf.Clamp01(travelled / distance)));
            yield return null;
        }

        lr.SetPosition(1, to);
        yield return new WaitForSeconds(data.trailDuration);
        lr.gameObject.SetActive(false);
        trailPool.Enqueue(lr);
    }

    LineRenderer GetTrailFromPool()
    {
        while (trailPool.Count > 0)
        {
            var lr = trailPool.Dequeue();
            if (lr != null) return lr;
        }
        return CreateTrail();
    }

    LineRenderer CreateTrail()
    {
        var go = new GameObject("BulletTrail");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.startWidth        = data != null ? data.trailWidth : 0.03f;
        lr.endWidth          = 0f;
        lr.useWorldSpace     = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        if (data != null && data.trailMaterial != null)
            lr.material = data.trailMaterial;
        else
        {
            lr.material    = new Material(Shader.Find("Sprites/Default"));
            lr.startColor  = new Color(1f, 0.9f, 0.5f, 0.8f);
            lr.endColor    = new Color(1f, 0.9f, 0.5f, 0f);
        }
        go.SetActive(false);
        return lr;
    }

    // ── Reload ───────────────────────────────────────────────────

    bool CanReload() => bulletsLeft < data.magazineSize && reserveAmmo > 0;

    IEnumerator Reload()
    {
        reloading = true;
        bool isEmpty = bulletsLeft == 0;
        fpsArmsAnimator?.PlayReload(isEmpty);
        if (playerController != null)
        {
            if (isEmpty) playerController.animIsReloadingEmpty = true;
            else         playerController.animIsReloading      = true;
        }
        if (data.reloadClip != null)
            audioSource.PlayOneShot(data.reloadClip, data.audioVolume);

        yield return new WaitForSeconds(data.reloadTime);

        int needed   = data.magazineSize - bulletsLeft;
        int pulled   = Mathf.Min(needed, reserveAmmo);
        bulletsLeft += pulled;
        reserveAmmo -= pulled;

        reloading = false;
        if (playerController != null)
        {
            playerController.animIsReloading      = false;
            playerController.animIsReloadingEmpty = false;
        }
    }

    // ── Melee ────────────────────────────────────────────────────

    private int   knifeCombo    = 0;
    private float lastKnifeTime = -999f;
    private const float KnifeComboWindow = 1f;
    // How long after the hit lands before you can chain the next strike
    private const float KnifeChainDelay  = 0.35f;

    IEnumerator Melee()
    {
        bool isKnife = data.altDamage > 0
                    && fpsArmsAnimator?.ActiveEntry?.knife1 != null;

        if (!isKnife)
        {
            // Non-knife melee (butt-stroke etc.)
            meleeing = true;
            if (playerController != null) playerController.animIsMelee1 = true;
            fpsArmsAnimator?.PlayFire();
            yield return new WaitForSeconds(0.6f);
            meleeing = false;
            if (playerController != null) playerController.animIsMelee1 = false;
            yield break;
        }

        // ── Knife combo ──────────────────────────────────────────
        // Interrupt any running melee so the next hit starts immediately.
        // meleeing stays true the whole chain; we only block NEW input
        // for KnifeChainDelay after each strike (not the full anim length).
        float now = Time.time;
        if (now - lastKnifeTime > KnifeComboWindow)
            knifeCombo = 0;                     // combo expired — restart

        knifeCombo    = (knifeCombo % 3) + 1;  // 1 → 2 → 3 → 1 → …
        lastKnifeTime = now;

        meleeing = true;
        if (playerController != null)
        {
            playerController.animIsMelee1 = knifeCombo == 1;
            playerController.animIsMelee2 = knifeCombo == 2;
            playerController.animIsMelee3 = knifeCombo == 3;
        }

        fpsArmsAnimator.PlayKnife(knifeCombo);
        KnifeStrike(data.bodyshotDamage);

        yield return new WaitForSeconds(KnifeChainDelay);

        meleeing = false;
        if (playerController != null)
        {
            playerController.animIsMelee1 = false;
            playerController.animIsMelee2 = false;
            playerController.animIsMelee3 = false;
        }
    }

    IEnumerator KnifeAlt()
    {
        meleeing = true;
        if (playerController != null) playerController.animIsMelee4 = true;
        fpsArmsAnimator?.PlayKnife(4);
        KnifeStrike(data.altDamage);
        yield return new WaitForSeconds(0.5f);
        meleeing = false;
        if (playerController != null) playerController.animIsMelee4 = false;
    }

    void KnifeStrike(float damage)
    {
        if (shootCamera == null) return;
        Ray ray = shootCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int ignoreSelfMask = ~LayerMask.GetMask("PlayerBody");
        if (Physics.Raycast(ray, out RaycastHit hit, data.range, ignoreSelfMask))
        {
            HitBox hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox != null && hitBox.owner != ownerNetworkPlayer)
                CmdRegisterHit(hitBox.owner.netId, damage, hit.point, hit.normal, true);
        }
    }

    // ── Public API ───────────────────────────────────────────────

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, data.maxReserveAmmo);
    }

    public void ResetState()
    {
        if (fireBurstCoroutine != null) { StopCoroutine(fireBurstCoroutine); fireBurstCoroutine = null; }
        if (reloadCoroutine    != null) { StopCoroutine(reloadCoroutine);    reloadCoroutine    = null; }
        if (meleeCoroutine     != null) { StopCoroutine(meleeCoroutine);     meleeCoroutine     = null; }

        reloading          = false;
        meleeing           = false;
        readyToShoot       = true;
        continuousFireTime = 0f;
        currentSpread      = 0f;
        wasFiringLastFrame = false;
        knifeCombo         = 0;
        lastKnifeTime      = -999f;

        if (data != null)
        {
            bulletsLeft = data.magazineSize;
            reserveAmmo = data.maxReserveAmmo;
        }

        if (playerController != null)
        {
            playerController.animIsReloading      = false;
            playerController.animIsReloadingEmpty = false;
            playerController.animIsEquipping      = false;
            playerController.animIsMelee1         = false;
            playerController.animIsMelee2         = false;
            playerController.animIsMelee3         = false;
            playerController.animIsMelee4         = false;
            playerController.animIsFiring         = false;
        }
    }

    // ── Networking ───────────────────────────────────────────────

    [Command]
    void CmdRegisterHit(uint targetNetId, float damage, Vector3 point, Vector3 normal, bool isPlayer)
    {
        RpcSpawnImpact(point, normal, isPlayer);
        if (!isPlayer || damage <= 0f) return;
        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        {
            NetworkPlayer target = identity.GetComponent<NetworkPlayer>();
            target?.TakeDamage(damage, netIdentity.name, netIdentity.netId);
        }
    }

    [ClientRpc]
    void RpcSpawnImpact(Vector3 point, Vector3 normal, bool isPlayer)
    {
        if (SurfaceManager.Instance != null)
            SurfaceManager.Instance.HandleImpact(null, point, normal, isPlayer);
        else
        {
            GameObject prefab = isPlayer ? bulletImpactFlesh : bulletImpactDefault;
            if (prefab == null) return;
            GameObject fx = Instantiate(prefab, point + normal * 0.01f, Quaternion.LookRotation(normal));
            Destroy(fx, 2f);
        }
    }
}
