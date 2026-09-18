using UnityEngine;

/// Place on a world GameObject with a Collider (set Is Trigger = true).
/// Assign gunIndex matching the index in FPSArmsAnimator.guns[].
public class GunPickup : MonoBehaviour
{
    [Tooltip("Index into FPSArmsAnimator.guns[] that this pickup switches to")]
    public int gunIndex = 0;

    [Tooltip("Ammo added to reserve on pickup (0 = no ammo added)")]
    public int bonusReserveAmmo = 0;

    [Tooltip("Spin axis for the pickup prop")]
    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 45f;

    [Tooltip("Optional visual mesh shown in the world")]
    public GameObject visualRoot;

    void Update()
    {
        if (spinAxis != Vector3.zero)
            transform.Rotate(spinAxis * spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        NetworkPlayer np = other.GetComponentInParent<NetworkPlayer>();
        if (np == null || !np.isLocalPlayer) return;

        np.SwitchGun(gunIndex);

        if (bonusReserveAmmo > 0)
        {
            // GunScript lives on the root player object
            GunScript gs = np.GetComponent<GunScript>();
            gs?.AddReserveAmmo(bonusReserveAmmo);
        }

        gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
    }
}
