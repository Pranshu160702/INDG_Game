using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// Samples the idle animation in edit mode so the prefab shows idle pose instead of T-pose.
[ExecuteAlways]
[RequireComponent(typeof(Animator))]
public class PrefabIdlePose : MonoBehaviour
{
#if UNITY_EDITOR
    public bool showIdlePose = true;

    // Cached T-pose data per bone
    Transform[] _bones;
    Quaternion[] _tposeRotations;

    void OnEnable()
    {
        if (Application.isPlaying) return;
        CacheTpose();
        Apply();
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;
        // Defer so transform hierarchy is ready
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            if (_bones == null) CacheTpose();
            Apply();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        };
    }

    void CacheTpose()
    {
        _bones = GetComponentsInChildren<Transform>(true);
        _tposeRotations = new Quaternion[_bones.Length];
        for (int i = 0; i < _bones.Length; i++)
            _tposeRotations[i] = _bones[i].localRotation;
    }

    void Apply()
    {
        var animator = GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;

        if (showIdlePose)
        {
            animator.Update(0f);
        }
        else
        {
            // Restore T-pose rotations
            if (_bones != null)
            {
                for (int i = 0; i < _bones.Length; i++)
                {
                    if (_bones[i] != null)
                        _bones[i].localRotation = _tposeRotations[i];
                }
            }
        }
    }
#endif
}
