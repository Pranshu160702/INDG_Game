using UnityEngine;

/// Samples the idle animation in edit mode so the prefab shows idle pose instead of T-pose.
[ExecuteAlways]
[RequireComponent(typeof(Animator))]
public class PrefabIdlePose : MonoBehaviour
{
#if UNITY_EDITOR
    void OnEnable()
    {
        if (Application.isPlaying) return;
        var animator = GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.Update(0f);
    }
#endif
}
