using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

/// Attach to the gun GameObject (child of the player's hand bone).
/// Drag a GunData asset into the slot for each gun type.
[RequireComponent(typeof(AudioSource))]
public class GunScript : NetworkBehaviour
{
    [Header("Data")]
    public GunData data;

    [Header("References")]
    public Transform muzzlePoint;           // empty child at barrel tip
    public GameObject muzzleFlashPrefab;    // MuzzleFlashEffect from EffectExamples
    public GameObject bulletImpactFlesh;    // BulletImpactFleshBigEffect
    public GameObject bulletImpactDefault;  // BulletImpactSandEffect or Wood
    public GameObject cartridgeEjectPrefab; // CartridgeEjectEffect

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;

    // Runtime state — bullets tracked locally, server authoritative via Cmd
    private int bulletsLeft;
    public int BulletsLeft => bulletsLeft;
    private bool readyToShoot = true;
    private bool reloading = false;
    private AudioSource audioSource;
    private NetworkPlayer ownerNetworkPlayer;

    // Set explicitly by NetworkPlayer whenever camera toggles
    [HideInInspector] public Camera shootCamera;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ownerNetworkPlayer = GetComponentInParent<NetworkPlayer>();
    }

    public override void OnStartLocalPlayer()
    {
        bulletsLeft = data != null ? data.magazineSize : 0;
    }

    // Called by NetworkPlayer to tell this gun it belongs to the local player
    [HideInInspector] public bool isOwnedLocally = false;

    void Update()
    {
        if (!isOwnedLocally) return;
        if (data == null) return;
        if (ownerNetworkPlayer != null && ownerNetworkPlayer.isDead) return;

        if (Mouse.current.leftButton.isPressed && readyToShoot && !reloading && bulletsLeft > 0)
        {
            readyToShoot = false;
            StartCoroutine(FireBurst());
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && !reloading && bulletsLeft < data.magazineSize)
            StartCoroutine(Reload());
    }

    IEnumerator FireBurst()
    {
        for (int i = 0; i < data.bulletsPerTap; i++)
        {
            if (bulletsLeft <= 0) break;
            Fire();
            if (data.bulletsPerTap > 1)
                yield return new WaitForSeconds(data.timeBetweenBursts);
        }
        yield return new WaitForSeconds(data.fireRate);
        readyToShoot = true;
    }

    void Fire()
    {
        bulletsLeft--;

        // Local effects (muzzle flash, sound, cartridge)
        PlayLocalEffects();

        // Build spread direction from screen center
        if (shootCamera == null) return;

        Vector2 spreadOffset = Random.insideUnitCircle * data.spread;
        Vector3 viewportPoint = new Vector3(0.5f + spreadOffset.x, 0.5f + spreadOffset.y, 0f);
        Ray ray = shootCamera.ViewportPointToRay(viewportPoint);

        if (Physics.Raycast(ray, out RaycastHit hit, data.range))
        {
            HitBox hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox != null)
            {
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
        reloading = true;
        if (reloadSound != null) audioSource.PlayOneShot(reloadSound);
        yield return new WaitForSeconds(data.reloadTime);
        bulletsLeft = data.magazineSize;
        reloading = false;
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
                target.TakeDamage(damage, netIdentity.name);
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
