using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GunEntry
{
    public string     gunName;
    public GameObject gunRoot;
    public GunData    data;

    [Header("FPS Clips")]
    public AnimationClip idle;
    public AnimationClip equip;
    public AnimationClip fire;
    public AnimationClip reload;
    public AnimationClip reloadEmptyMag;

    [Header("FPS Clips — knife only")]
    public AnimationClip knife1;
    public AnimationClip knife2;
    public AnimationClip knife3;
    public AnimationClip knife4;

    [System.NonSerialized] public Animator                   animator;
    [System.NonSerialized] public AnimatorOverrideController overrideController;
}

public class FPSArmsAnimator : MonoBehaviour
{
    [Header("Base Controller")]
    public RuntimeAnimatorController baseController;

    [Header("Guns")]
    public GunEntry[] guns;
    public int        defaultGunIndex = 0;

    public GunEntry ActiveEntry { get; private set; }
    public int      ActiveIndex { get; private set; } = -1;

    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> clipBuffer
        = new List<KeyValuePair<AnimationClip, AnimationClip>>(9);

    private static readonly int HashFire        = Animator.StringToHash("fire");
    private static readonly int HashEquip       = Animator.StringToHash("equip");
    private static readonly int HashReload      = Animator.StringToHash("reload");
    private static readonly int HashReloadEmpty = Animator.StringToHash("reload_emptymag");
    private static readonly int HashKnife1      = Animator.StringToHash("knife1");
    private static readonly int HashKnife2      = Animator.StringToHash("knife2");
    private static readonly int HashKnife3      = Animator.StringToHash("knife3");
    private static readonly int HashKnife4      = Animator.StringToHash("knife4");

    private const string ClipIdle        = "idle";
    private const string ClipEquip       = "equip";
    private const string ClipFire        = "fire";
    private const string ClipReload      = "reload";
    private const string ClipReloadEmpty = "reload_emptymag";
    private const string ClipKnife1      = "knife1";
    private const string ClipKnife2      = "knife2";
    private const string ClipKnife3      = "knife3";
    private const string ClipKnife4      = "knife4";

    void Awake()
    {
        if (baseController == null)
        {
            Debug.LogError("[FPSArmsAnimator] No base controller on " + name);
            return;
        }

        if (guns == null) return;

        foreach (var entry in guns)
        {
            if (entry.gunRoot == null) continue;

            entry.animator = entry.gunRoot.GetComponent<Animator>()
                          ?? entry.gunRoot.GetComponentInChildren<Animator>(true);

            if (entry.animator == null)
            {
                Debug.LogWarning("[FPSArmsAnimator] No Animator on: " + entry.gunName);
                continue;
            }

            entry.overrideController = new AnimatorOverrideController(baseController);
            entry.animator.runtimeAnimatorController = entry.overrideController;
        }
    }

    void OnEnable()
    {
        if (ActiveIndex >= 0 && guns != null && ActiveIndex < guns.Length)
            ApplyClips(guns[ActiveIndex]);
        else
            SwitchGun(defaultGunIndex);
    }

    public void SwitchGun(int index)
    {
        if (guns == null || guns.Length == 0) return;
        index = Mathf.Clamp(index, 0, guns.Length - 1);
        if (index == ActiveIndex) return;

        if (ActiveIndex >= 0 && guns[ActiveIndex].gunRoot != null)
            guns[ActiveIndex].gunRoot.SetActive(false);

        ActiveIndex = index;
        ActiveEntry = guns[index];

        if (ActiveEntry.gunRoot != null)
            ActiveEntry.gunRoot.SetActive(true);

        ApplyClips(ActiveEntry);
    }

    void ApplyClips(GunEntry entry)
    {
        if (entry.overrideController == null) return;

        entry.overrideController.GetOverrides(clipBuffer);

        for (int i = 0; i < clipBuffer.Count; i++)
        {
            AnimationClip original    = clipBuffer[i].Key;
            AnimationClip replacement = GetReplacement(original.name, entry);
            clipBuffer[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement ?? original);
        }

        entry.overrideController.ApplyOverrides(clipBuffer);
    }

    AnimationClip GetReplacement(string clipName, GunEntry entry)
    {
        return clipName switch
        {
            ClipIdle        => entry.idle,
            ClipEquip       => entry.equip,
            ClipFire        => entry.fire,
            ClipReload      => entry.reload,
            ClipReloadEmpty => entry.reloadEmptyMag,
            ClipKnife1      => entry.knife1,
            ClipKnife2      => entry.knife2,
            ClipKnife3      => entry.knife3,
            ClipKnife4      => entry.knife4,
            _               => null
        };
    }

    public void PlayFire()                { ActiveEntry?.animator?.SetTrigger(HashFire); }
    public void PlayEquip()               { ActiveEntry?.animator?.SetTrigger(HashEquip); }
    public void PlayReload(bool emptyMag) { ActiveEntry?.animator?.SetTrigger(emptyMag ? HashReloadEmpty : HashReload); }
    public void PlayKnife(int combo)
    {
        if (ActiveEntry?.animator == null) return;
        int hash = combo switch { 1 => HashKnife1, 2 => HashKnife2, 3 => HashKnife3, _ => HashKnife4 };
        ActiveEntry.animator.SetTrigger(hash);
    }
}
