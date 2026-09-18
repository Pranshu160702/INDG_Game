using UnityEngine;

[CreateAssetMenu(fileName = "NewGunData", menuName = "Weapons/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("Identity")]
    public string gunName;
    public Sprite gunIcon;

    [Header("Firing")]
    public float fireRate = 0.1f;
    public int bulletsPerTap = 1;
    public float timeBetweenBursts = 0.05f;

    [Header("Spread")]
    [Tooltip("Minimum spread when first firing (viewport units)")]
    public float minSpread = 0.005f;
    [Tooltip("Maximum spread after sustained fire")]
    public float maxSpread = 0.06f;
    [Tooltip("Seconds of continuous fire to reach max spread")]
    public float maxSpreadTime = 1.5f;
    [Tooltip("How fast spread recovers per second after releasing fire")]
    public float spreadRecoverySpeed = 2f;

    [Header("Magazine")]
    public int magazineSize = 30;
    [Tooltip("Total reserve ammo carried (not counting current mag)")]
    public int maxReserveAmmo = 90;
    public float reloadTime = 2f;

    [Header("Damage")]
    public float headshotDamage = 160f;
    public float bodyshotDamage = 40f;
    [Tooltip("Used for knife right-click alt attack. 0 = not used.")]
    public float altDamage = 0f;
    [Tooltip("Optional: damage falloff curve. X = distance (0-1 normalized to range), Y = damage multiplier.")]
    public AnimationCurve damageFalloff;

    [Header("Range")]
    public float range = 300f;

    [Header("Audio")]
    [Tooltip("Randomly selected each shot")]
    public AudioClip[] fireClips;
    [Tooltip("Played on last bullet in mag")]
    public AudioClip lastBulletClip;
    [Tooltip("Played when trying to fire with empty mag")]
    public AudioClip emptyClip;
    public AudioClip reloadClip;
    [Range(0f, 1f)] public float audioVolume = 1f;

    [Header("Trail / Tracer")]
    public bool showBulletTrail = true;
    public Material trailMaterial;
    public float trailDuration = 0.15f;
    public float trailWidth = 0.03f;
    public float trailSimulationSpeed = 200f;
    [Tooltip("How far the trail travels if nothing is hit")]
    public float trailMissDistance = 200f;

    // ── Runtime helpers ──────────────────────────────────────────

    public float GetDamageMultiplier(float distance)
    {
        if (damageFalloff == null || damageFalloff.length == 0) return 1f;
        float t = Mathf.Clamp01(distance / Mathf.Max(range, 0.01f));
        return damageFalloff.Evaluate(t);
    }

    public float GetSpread(float continuousFireTime)
    {
        return Mathf.Lerp(minSpread, maxSpread, Mathf.Clamp01(continuousFireTime / Mathf.Max(maxSpreadTime, 0.01f)));
    }
}
