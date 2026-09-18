using UnityEngine;

/// Place on a world GameObject with a Collider (set Is Trigger = true).
/// Adds reserve ammo to whatever gun the player currently has active.
public class AmmoPickup : MonoBehaviour
{
    [Tooltip("How much reserve ammo to add")]
    public int ammoAmount = 30;

    [Tooltip("Spin axis for the pickup prop")]
    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 60f;

    void Update()
    {
        if (spinAxis != Vector3.zero)
            transform.Rotate(spinAxis * spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        NetworkPlayer np = other.GetComponentInParent<NetworkPlayer>();
        if (np == null || !np.isLocalPlayer) return;

        GunScript gs = np.GetComponent<GunScript>();
        if (gs == null) return;

        gs.AddReserveAmmo(ammoAmount);

        gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
    }
}
