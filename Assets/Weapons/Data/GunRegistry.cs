using UnityEngine;

/// Single source of truth for every gun in the game.
/// Create one instance at: Assets/Weapons/Data/GunRegistry.asset
/// 
/// HOW TO ADD A NEW GUN:
/// 1. Create a new GunData asset (right-click → Weapons/Gun Data)
/// 2. Fill in all stats, audio clips, trail settings
/// 3. Add an entry here in the Guns array
/// 4. Add the gun mesh as a child of FPSArms in the Networked_Commando prefab
/// 5. Add the entry to FPSArmsAnimator.guns[] in the prefab inspector
/// That's it — the index in this registry matches the index in FPSArmsAnimator.guns[]
[CreateAssetMenu(fileName = "GunRegistry", menuName = "Weapons/Gun Registry")]
public class GunRegistry : ScriptableObject
{
    [System.Serializable]
    public struct GunRegistryEntry
    {
        [Tooltip("Must match the index in FPSArmsAnimator.guns[]")]
        public int gunIndex;
        public GunData data;
        [Tooltip("World-space pickup model from Assets/Weapons/PickupModels/")]
        public GameObject pickupModelPrefab;
        [Tooltip("World-space ammo pickup model")]
        public GameObject ammoPickupModelPrefab;
        [Tooltip("Default reserve ammo given when picking up this gun")]
        public int pickupReserveAmmo;
    }

    public GunRegistryEntry[] guns;

    public GunData GetData(int index)
    {
        foreach (var entry in guns)
            if (entry.gunIndex == index) return entry.data;
        return null;
    }

    public GunRegistryEntry? GetEntry(int index)
    {
        foreach (var entry in guns)
            if (entry.gunIndex == index) return entry;
        return null;
    }
}
