using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TPSGunEntry
{
    public string     gunName;
    public GameObject tpsGunRoot;

    [Header("TPS Clips")]
    public AnimationClip idle;
    public AnimationClip equip;
    public AnimationClip fire;
    public AnimationClip reload;
    public AnimationClip reloadEmptyMag;

    [Header("TPS Clips — knife only")]
    public AnimationClip knife1;
    public AnimationClip knife2;
    public AnimationClip knife3;
    public AnimationClip knife4;

    [System.NonSerialized] public AnimatorOverrideController overrideController;
}

public class TPSArmsAnimator : MonoBehaviour
{
    [Header("Base Controller")]
    public RuntimeAnimatorController baseController;

    [Header("Guns")]
    public TPSGunEntry[] guns;

    private Animator animator;
    private int activeIndex = -1;

    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> clipBuffer
        = new List<KeyValuePair<AnimationClip, AnimationClip>>(9);

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[TPSArmsAnimator] No Animator on " + name);
            return;
        }

        if (baseController == null)
        {
            Debug.LogError("[TPSArmsAnimator] No base controller on " + name);
            return;
        }

        if (guns == null) return;

        foreach (var entry in guns)
            entry.overrideController = new AnimatorOverrideController(baseController);
    }

    public void SwitchGun(int index)
    {
        if (guns == null || guns.Length == 0) return;
        index = Mathf.Clamp(index, 0, guns.Length - 1);
        if (index == activeIndex) return;

        if (activeIndex >= 0 && guns[activeIndex].tpsGunRoot != null)
            guns[activeIndex].tpsGunRoot.SetActive(false);

        activeIndex = index;

        if (guns[activeIndex].tpsGunRoot != null)
            guns[activeIndex].tpsGunRoot.SetActive(true);

        ApplyClips(guns[activeIndex]);
    }

    void ApplyClips(TPSGunEntry entry)
    {
        if (animator == null || entry.overrideController == null) return;

        animator.runtimeAnimatorController = entry.overrideController;

        entry.overrideController.GetOverrides(clipBuffer);
        for (int i = 0; i < clipBuffer.Count; i++)
        {
            AnimationClip original    = clipBuffer[i].Key;
            AnimationClip replacement = GetReplacement(original.name, entry);
            clipBuffer[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement ?? original);
        }
        entry.overrideController.ApplyOverrides(clipBuffer);
    }

    AnimationClip GetReplacement(string clipName, TPSGunEntry entry)
    {
        return clipName switch
        {
            "Idle Upper"               => entry.idle,
            "Equip Upper"              => entry.equip,
            "Firing Upper"             => entry.fire,
            "Reloading Upper"          => entry.reload,
            "Reloading EmptyMag Upper" => entry.reloadEmptyMag,
            "Knife1 Upper"             => entry.knife1,
            "Knife2 Upper"             => entry.knife2,
            "Knife3 Upper"             => entry.knife3,
            "Knife4 Upper"             => entry.knife4,
            _                          => null
        };
    }
}
