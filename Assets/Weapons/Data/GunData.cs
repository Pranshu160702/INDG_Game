using UnityEngine;

[CreateAssetMenu(fileName = "NewGunData", menuName = "Weapons/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("Identity")]
    public string gunName;
    public Sprite gunIcon;

    [Header("Firing")]
    public float fireRate = 0.1f;       // seconds between shots
    public float spread = 0.02f;        // bullet spread radius (viewport units)
    public int bulletsPerTap = 1;       // 1 = semi/auto, >1 = shotgun
    public float timeBetweenBursts = 0.05f; // delay between bullets in a tap

    [Header("Magazine")]
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("Damage")]
    public float headshotDamage = 100f;
    public float bodyshotDamage = 34f;
    public float legsshotDamage = 17f;

    [Header("Range")]
    public float range = 300f;
}
