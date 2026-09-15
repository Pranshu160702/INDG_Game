using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class CommandoAnimatorSetup : EditorWindow
{
    [MenuItem("Tools/Setup Commando Animator")]
    static void Run()
    {
        SetupAnimator();
        SetupMasks();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Commando Animator setup complete.");
    }

    [MenuItem("INDG/Setup/Add Gun Action States to Commando Animator")]
    static void AddGunActionStates()
    {
        string controllerPath = "Assets/Characters/Commando/Animator/CommandoAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) { Debug.LogError("CommandoAnimator.controller not found."); return; }

        // --- Ensure parameters exist ---
        EnsureParameter(controller, "isFiring",    AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isReloading", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isMelee",     AnimatorControllerParameterType.Bool);

        // --- Load clips ---
        AnimationClip firingClip   = LoadClip("Assets/Characters/Commando/Animations/UpperBodyAnimations/Aiming/firing rifle.fbx");
        AnimationClip reloadClip   = LoadClip("Assets/Characters/Commando/Animations/UpperBodyAnimations/Aiming/reloading.fbx");
        // No melee clip in upper body folder — reuse idle aiming as placeholder until you add one
        AnimationClip meleeClip    = LoadClip("Assets/Characters/Commando/Animations/UpperBodyAnimations/Aiming/IdleAiming.fbx");
        AnimationClip aimingClip   = LoadClip("Assets/Characters/Commando/Animations/UpperBodyAnimations/Aiming/IdleAiming.fbx");

        if (firingClip == null)  { Debug.LogError("firing rifle.fbx clip not found."); return; }
        if (reloadClip == null)  { Debug.LogError("reloading.fbx clip not found."); return; }

        // --- Find upper body layer ---
        int upperLayerIdx = -1;
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name.ToLower().Contains("upper"))
            { upperLayerIdx = i; break; }
        }
        if (upperLayerIdx < 0) { Debug.LogError("No upper body layer found in CommandoAnimator."); return; }

        var upperSM = controller.layers[upperLayerIdx].stateMachine;

        // --- Find or create states ---
        AnimatorState aimingState  = FindOrCreateState(upperSM, "Aiming Upper",   aimingClip,  true);
        AnimatorState firingState  = FindOrCreateState(upperSM, "Firing Upper",   firingClip,  false);
        AnimatorState reloadState  = FindOrCreateState(upperSM, "Reloading Upper", reloadClip, false);
        AnimatorState meleeState   = FindOrCreateState(upperSM, "Melee Upper",    meleeClip,   false);

        // Set aiming as default if not already
        if (upperSM.defaultState == null)
            upperSM.defaultState = aimingState;

        // --- Clear existing transitions from these states to avoid duplicates ---
        ClearTransitionsFrom(aimingState);
        ClearTransitionsFrom(firingState);
        ClearTransitionsFrom(reloadState);
        ClearTransitionsFrom(meleeState);

        // --- Wire transitions ---

        // Aiming → Firing (isFiring true)
        AddBoolTransition(aimingState, firingState, "isFiring", true, false, 0.1f);
        // Aiming → Reloading (isReloading true)
        AddBoolTransition(aimingState, reloadState, "isReloading", true, false, 0.1f);
        // Aiming → Melee (isMelee true)
        AddBoolTransition(aimingState, meleeState, "isMelee", true, false, 0.1f);

        // Firing → Aiming (isFiring false)
        AddBoolTransition(firingState, aimingState, "isFiring", false, false, 0.1f);
        // Reloading → Aiming (isReloading false)
        AddBoolTransition(reloadState, aimingState, "isReloading", false, false, 0.1f);
        // Melee → Aiming (isMelee false)
        AddBoolTransition(meleeState, aimingState, "isMelee", false, false, 0.1f);

        // Firing can also go to Reloading directly
        AddBoolTransition(firingState, reloadState, "isReloading", true, false, 0.1f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[INDG] Gun action states added to Commando upper body layer.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static AnimationClip LoadClip(string path)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                return c;
        return null;
    }

    static void EnsureParameter(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name) return;
        ctrl.AddParameter(name, type);
        Debug.Log($"[INDG] Added parameter '{name}' to CommandoAnimator.");
    }

    static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string name, AnimationClip clip, bool loop)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == name)
            {
                cs.state.motion = clip;
                return cs.state;
            }
        var state = sm.AddState(name);
        state.motion = clip;
        if (clip != null)
        {
            // Set loop time on the clip via SerializedObject
            var so = new SerializedObject(clip);
            var settings = so.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
                so.ApplyModifiedProperties();
            }
        }
        return state;
    }

    static void ClearTransitionsFrom(AnimatorState state)
    {
        foreach (var t in state.transitions)
            Object.DestroyImmediate(t, true);
        // Reassign empty array
        state.transitions = new AnimatorStateTransition[0];
    }

    static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value, bool hasExitTime, float duration)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = hasExitTime;
        t.duration = duration;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    // ── Original setup below (unchanged) ─────────────────────────────────────

    static void SetupAnimator()
    {
        string controllerPath = "Assets/Characters/Commando/Animator/CommandoAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) { Debug.LogError("CommandoAnimator.controller not found."); return; }

        var clipMap = new Dictionary<string, string>
        {
            { "idle",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/idle/idle.fbx" },
            { "idle crouch",    "Assets/Characters/Commando/Animations/LowerBodyAnimations/idle/idle crouching.fbx" },
            { "jump",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/Jumping/jump.fbx" },
            { "crouch jump",    "Assets/Characters/Commando/Animations/LowerBodyAnimations/Jumping/jump.fbx" },
            { "death",          "Assets/Characters/Commando/Animations/Dying/death from bodyshot.fbx" },
            { "death headshot", "Assets/Characters/Commando/Animations/Dying/death from headshot.fbx" },
            { "death crouch",   "Assets/Characters/Commando/Animations/Dying/death crouching.fbx" },
            { "idle upper",     "Assets/Characters/Commando/Animations/UpperBodyAnimations/Idle/idle.fbx" },
            { "aiming upper",   "Assets/Characters/Commando/Animations/UpperBodyAnimations/Aiming/IdleAiming.fbx" },
            { "walk",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward.fbx" },
            { "crouch walk",    "Assets/Characters/Commando/Animations/LowerBodyAnimations/idle/idle crouching.fbx" },
            { "sprint",         "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward.fbx" },
        };

        var blendChildMap = new Dictionary<string, string>
        {
            { "walk forward",                "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward.fbx" },
            { "walk backward",               "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk backward.fbx" },
            { "walk left",                   "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk left.fbx" },
            { "walk right",                  "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk right.fbx" },
            { "walk forward left",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward left.fbx" },
            { "walk forward right",          "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward right.fbx" },
            { "walk backward left",          "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk backward left.fbx" },
            { "walk backward right",         "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk backward right.fbx" },
            { "run forward",                 "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward.fbx" },
            { "run backward",                "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run backward.fbx" },
            { "run left",                    "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run left.fbx" },
            { "run right",                   "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run right.fbx" },
            { "run forward left",            "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward left.fbx" },
            { "run forward right",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward right.fbx" },
            { "run backward left",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run backward left.fbx" },
            { "run backward right",          "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run backward right.fbx" },
            { "walk crouching forward",      "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching forward.fbx" },
            { "walk crouching backward",     "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching backward.fbx" },
            { "walk crouching left",         "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching left.fbx" },
            { "walk crouching right",        "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching right.fbx" },
            { "walk crouching forward left", "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching forward left.fbx" },
            { "walk crouching forward right","Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching forward right.fbx" },
            { "walk crouching backward left","Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching backward left.fbx" },
            { "walk crouching backward right","Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching backward right.fbx" },
        };

        var resolved = new Dictionary<string, AnimationClip>();
        void Resolve(Dictionary<string, string> map)
        {
            foreach (var kv in map)
            {
                if (resolved.ContainsKey(kv.Key)) continue;
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(kv.Value))
                    if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    { resolved[kv.Key] = c; break; }
                if (!resolved.ContainsKey(kv.Key))
                    Debug.LogWarning($"Clip not found for '{kv.Key}' at: {kv.Value}");
            }
        }
        Resolve(clipMap);
        Resolve(blendChildMap);

        foreach (var layer in controller.layers)
            RemapStateMachine(layer.stateMachine, resolved);

        EditorUtility.SetDirty(controller);
    }

    static void RemapStateMachine(AnimatorStateMachine sm, Dictionary<string, AnimationClip> clips)
    {
        foreach (var cs in sm.states)
            RemapState(cs.state, clips);
        foreach (var csm in sm.stateMachines)
            RemapStateMachine(csm.stateMachine, clips);
    }

    static void RemapState(AnimatorState state, Dictionary<string, AnimationClip> clips)
    {
        state.motion = RemapMotion(state.motion, clips, state.name);
        EditorUtility.SetDirty(state);
    }

    static Motion RemapMotion(Motion motion, Dictionary<string, AnimationClip> clips, string stateName)
    {
        if (motion is BlendTree bt)
        {
            var children = bt.children;
            for (int i = 0; i < children.Length; i++)
                children[i].motion = RemapMotion(children[i].motion, clips, stateName);
            bt.children = children;
            EditorUtility.SetDirty(bt);
            return bt;
        }

        if (motion is AnimationClip)
        {
            if (clips.TryGetValue(stateName.ToLower(), out var c)) return c;
            string assetPath = AssetDatabase.GetAssetPath(motion);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower();
                if (clips.TryGetValue(fileName, out c)) return c;
            }
            Debug.LogWarning($"No Commando clip mapped for state '{stateName}' / asset '{AssetDatabase.GetAssetPath(motion)}'");
        }

        return motion;
    }

    static void SetupMasks()
    {
        string lowerPath = "Assets/Characters/Commando/Animator/CommandoLowerBodyMask.mask";
        string upperPath = "Assets/Characters/Commando/Animator/CommandoUpperBodyMask.mask";

        AvatarMask lower = AssetDatabase.LoadAssetAtPath<AvatarMask>(lowerPath);
        AvatarMask upper = AssetDatabase.LoadAssetAtPath<AvatarMask>(upperPath);

        if (lower != null)
        {
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, true);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, true);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, true);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, false);
            lower.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, false);
            EditorUtility.SetDirty(lower);
        }

        if (upper != null)
        {
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, false);
            upper.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, false);
            EditorUtility.SetDirty(upper);
        }
    }
}
