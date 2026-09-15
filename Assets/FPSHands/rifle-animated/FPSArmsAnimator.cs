using UnityEngine;

public class FPSArmsAnimator : MonoBehaviour
{
    public RuntimeAnimatorController controllerAsset;

    private Animator animator;

    private static readonly int Fire   = Animator.StringToHash("fire");
    private static readonly int Reload = Animator.StringToHash("reload");
    private static readonly int Hide   = Animator.StringToHash("hide");
    private static readonly int Ready  = Animator.StringToHash("ready");
    private static readonly int Melee  = Animator.StringToHash("melee");

    void OnEnable()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            Debug.LogError("[FPSArmsAnimator] No Animator found on " + gameObject.name);
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            if (controllerAsset != null)
            {
                animator.runtimeAnimatorController = controllerAsset;
                Debug.Log("[FPSArmsAnimator] Assigned controller from controllerAsset: " + controllerAsset.name);
            }
            else
                Debug.LogError("[FPSArmsAnimator] No controller assigned on " + gameObject.name + ". Drag FPSArmsController into the Controller Asset slot.");
        }
        else
        {
            Debug.Log("[FPSArmsAnimator] OnEnable — animator=" + animator.gameObject.name + " controller=" + animator.runtimeAnimatorController.name);
        }
    }

    public void PlayFire()
    {
        if (animator == null) { Debug.LogError("[FPSArmsAnimator] PlayFire — animator is null!"); return; }
        Debug.Log("[FPSArmsAnimator] PlayFire triggered");
        animator.SetTrigger(Fire);
    }
    public void PlayReload()
    {
        if (animator == null) { Debug.LogError("[FPSArmsAnimator] PlayReload — animator is null!"); return; }
        Debug.Log("[FPSArmsAnimator] PlayReload triggered");
        animator.SetTrigger(Reload);
    }
    public void PlayHide()   { if (animator != null) animator.SetTrigger(Hide); }
    public void PlayReady()
    {
        if (animator == null) { Debug.LogError("[FPSArmsAnimator] PlayReady — animator is null!"); return; }
        Debug.Log("[FPSArmsAnimator] PlayReady triggered");
        animator.SetTrigger(Ready);
    }
    public void PlayMelee()  { if (animator != null) animator.SetTrigger(Melee); }
}
