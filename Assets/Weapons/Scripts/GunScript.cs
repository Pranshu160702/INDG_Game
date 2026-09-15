using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(AudioSource))]
public class GunScript : NetworkBehaviour
{
    [Header("Data")]
    public GunData data;

    [Header("References")]
    public Transform muzzlePoint;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletImpactFlesh;
    public GameObject bulletImpactDefault;
    public GameObject cartridgeEjectPrefab;

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;

    private int bulletsLeft;
    public int BulletsLeft => bulletsLeft;
    private bool readyToShoot = true;
    private bool reloading = false;
    private bool meleeing = false;
    private AudioSource audioSource;
    private NetworkPlayer ownerNetworkPlayer;

    [HideInInspector] public Camera shootCamera;
    [HideInInspector] public FPSArmsAnimator fpsArmsAnimator;
    [HideInInspector] public bool isOwnedLocally = false;
    [HideInInspector] public PlayerController playerController;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ownerNetworkPlayer = GetComponentInParent<NetworkPlayer>();
    }

    public override void OnStartLocalPlayer()
    {
        bulletsLeft = data != null ? data.magazineSize : 0;
    }

    private Coroutine fireBurstCoroutine;
    private Coroutine reloadCoroutine;
    private Coroutine meleeCoroutine;

    void Update()
    {
        if (!isOwnedLocally) return;
        if (data == null) return;
        if (ownerNetworkPlayer != null && ownerNetworkPlayer.isDead) return;
        if (Mouse.current == null || Keyboard.current == null) return;

        if (Mouse.current.leftButton.isPressed && readyToShoot && !reloading && !meleeing && bulletsLeft > 0)
        {
            readyToShoot = false;
            fireBurstCoroutine = StartCoroutine(FireBurst());
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && !reloading && !meleeing && bulletsLeft < data.magazineSize)
            reloadCoroutine = StartCoroutine(Reload());

        if (Keyboard.current.fKey.wasPressedThisFrame && !meleeing && !reloading)
            meleeCoroutine = StartCoroutine(Melee());
    }

    IEnumerator FireBurst()
    {
        Debug.Log("[GunScript] FireBurst — fpsArmsAnimator=" + (fpsArmsAnimator != null) + " playerController=" + (playerController != null));
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
        PlayLocalEffects();

        if (shootCamera == null) return;

        // Exclude PlayerBody layer so local player can't shoot themselves
        int ignoreSelfMask = ~LayerMask.GetMask("PlayerBody");
        Vector2 spreadOffset = Random.insideUnitCircle * data.spread;
        Vector3 viewportPoint = new Vector3(0.5f + spreadOffset.x, 0.5f + spreadOffset.y, 0f);
        Ray ray = shootCamera.ViewportPointToRay(viewportPoint);

        if (Physics.Raycast(ray, out RaycastHit hit, data.range, ignoreSelfMask))
        {
            HitBox hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox != null)
            {
                // Don't register hits on own hitboxes
                if (hitBox.owner == ownerNetworkPlayer) return;
                float dmg = hitBox.type switch
                {
                    HitBoxType.Head => data.headshotDamage,
                    HitBoxType.Body => data.bodyshotDamage,
                    HitBoxType.Legs => data.legsshotDamage,
                    _ => data.bodyshotDamage
                };
                CmdRegisterHit(hitBox.owner.netId, dmg, hit.point, hit.normal, true);
            }
            else
            {
                CmdRegisterHit(0, 0f, hit.point, hit.normal, false);
            }
        }
    }

    void PlayLocalEffects()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject fx = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            fx.transform.SetParent(muzzlePoint);
            ParticleSystem ps = fx.GetComponent<ParticleSystem>();
            if (ps == null) ps = fx.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play(true);
            Destroy(fx, 0.3f);
        }
        if (cartridgeEjectPrefab != null && muzzlePoint != null)
        {
            GameObject c = Instantiate(cartridgeEjectPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(c, 2f);
        }
        if (fireSound != null)
            audioSource.PlayOneShot(fireSound);
    }

    IEnumerator Reload()
    {
        Debug.Log("[GunScript] Reload — fpsArmsAnimator=" + (fpsArmsAnimator != null) + " playerController=" + (playerController != null));
        reloading = true;
        fpsArmsAnimator?.PlayReload();
        if (playerController != null) playerController.animIsReloading = true;
        if (reloadSound != null) audioSource.PlayOneShot(reloadSound);
        yield return new WaitForSeconds(data.reloadTime);
        bulletsLeft = data.magazineSize;
        reloading = false;
        if (playerController != null) playerController.animIsReloading = false;
    }

    IEnumerator Melee()
    {
        meleeing = true;
        fpsArmsAnimator?.PlayMelee();
        if (playerController != null) playerController.animIsMelee = true;
        yield return new WaitForSeconds(0.83f);
        meleeing = false;
        if (playerController != null) playerController.animIsMelee = false;
    }

    public void ResetState()
    {
        if (fireBurstCoroutine != null) { StopCoroutine(fireBurstCoroutine); fireBurstCoroutine = null; }
        if (reloadCoroutine != null)    { StopCoroutine(reloadCoroutine);    reloadCoroutine = null; }
        if (meleeCoroutine != null)     { StopCoroutine(meleeCoroutine);     meleeCoroutine = null; }
        reloading = false;
        meleeing = false;
        readyToShoot = true;
        bulletsLeft = data != null ? data.magazineSize : 0;
        if (playerController != null)
        {
            playerController.animIsReloading = false;
            playerController.animIsMelee = false;
            playerController.animIsFiring = false;
        }
    }

    // ── Networking ──────────────────────────────────────────────

    [Command]
    void CmdRegisterHit(uint targetNetId, float damage, Vector3 point, Vector3 normal, bool isPlayer)
    {
        RpcSpawnImpact(point, normal, isPlayer);
        if (!isPlayer || damage <= 0f) return;
        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        {
            NetworkPlayer target = identity.GetComponent<NetworkPlayer>();
            if (target != null)
                target.TakeDamage(damage, netIdentity.name, netIdentity.netId);
        }
    }

    [ClientRpc]
    void RpcSpawnImpact(Vector3 point, Vector3 normal, bool isPlayer)
    {
        GameObject prefab = isPlayer ? bulletImpactFlesh : bulletImpactDefault;
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, point + normal * 0.01f, Quaternion.LookRotation(normal));
        Destroy(fx, 2f);
    }
}