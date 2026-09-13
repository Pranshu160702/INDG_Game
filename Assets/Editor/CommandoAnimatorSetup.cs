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

    static void SetupAnimator()
    {
        string controllerPath = "Assets/Characters/Commando/Animator/CommandoAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) { Debug.LogError("CommandoAnimator.controller not found."); return; }

        // Map by STATE NAME (lowercase) -> Commando fbx path
        var clipMap = new Dictionary<string, string>
        {
            // Base Layer states
            { "idle",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/idle/idle.fbx" },
            { "idle crouch",    "Assets/Characters/Commando/Animations/LowerBodyAnimations/idle/idle crouching.fbx" },
            { "jump",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/Jumping/jump.fbx" },
            { "crouch jump",    "Assets/Characters/Commando/Animations/LowerBodyAnimations/Jumping/jump.fbx" },
            { "death",          "Assets/Characters/Commando/Animations/Dying/death from bodyshot.fbx" },
            { "death headshot", "Assets/Characters/Commando/Animations/Dying/death from headshot.fbx" },
            { "death crouch",   "Assets/Characters/Commando/Animations/Dying/death crouching.fbx" },
            // Upper Body layer states
            { "idle upper",     "Assets/Characters/Commando/Animations/UpperBodyAnimations/Idle/idle.fbx" },
            { "aiming upper",   "Assets/Characters/Commando/Animations/UpperBodyAnimations/Aiming/IdleAiming.fbx" },
            // Walk blend tree children (matched by blend tree child state name = parent state name)
            { "walk",                        "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward.fbx" },
            // Crouch Walk blend tree children
            { "crouch walk",                 "Assets/Characters/Commando/Animations/LowerBodyAnimations/idle/idle crouching.fbx" },
            // Sprint blend tree children
            { "sprint",                      "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward.fbx" },
        };

        // Blend tree child clips mapped by fbx filename (since children have no state name)
        var blendChildMap = new Dictionary<string, string>
        {
            // Walk
            { "walk forward",               "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward.fbx" },
            { "walk backward",              "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk backward.fbx" },
            { "walk left",                  "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk left.fbx" },
            { "walk right",                 "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk right.fbx" },
            { "walk forward left",          "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward left.fbx" },
            { "walk forward right",         "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk forward right.fbx" },
            { "walk backward left",         "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk backward left.fbx" },
            { "walk backward right",        "Assets/Characters/Commando/Animations/LowerBodyAnimations/Walking/walk backward right.fbx" },
            // Sprint
            { "run forward",                "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward.fbx" },
            { "run backward",               "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run backward.fbx" },
            { "run left",                   "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run left.fbx" },
            { "run right",                  "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run right.fbx" },
            { "run forward left",           "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward left.fbx" },
            { "run forward right",          "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run forward right.fbx" },
            { "run backward left",          "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run backward left.fbx" },
            { "run backward right",         "Assets/Characters/Commando/Animations/LowerBodyAnimations/Sprinting/run backward right.fbx" },
            // Crouch Walk
            { "walk crouching forward",     "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching forward.fbx" },
            { "walk crouching backward",    "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching backward.fbx" },
            { "walk crouching left",        "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching left.fbx" },
            { "walk crouching right",       "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching right.fbx" },
            { "walk crouching forward left", "Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching forward left.fbx" },
            { "walk crouching forward right","Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching forward right.fbx" },
            { "walk crouching backward left","Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching backward left.fbx" },
            { "walk crouching backward right","Assets/Characters/Commando/Animations/LowerBodyAnimations/crouching/walk crouching backward right.fbx" },
        };

        // Build lookup: key -> AnimationClip
        var resolved = new Dictionary<string, AnimationClip>();
        void Resolve(Dictionary<string, string> map)
        {
            foreach (var kv in map)
            {
                if (resolved.ContainsKey(kv.Key)) continue;
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(kv.Value))
                {
                    if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    { resolved[kv.Key] = c; break; }
                }
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
            // Try state name first (for direct states)
            if (clips.TryGetValue(stateName.ToLower(), out var c)) return c;

            // For blend tree children, match by asset filename (without extension, lowercase)
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
            // Lower body: hips + legs only (no arms/spine upper)
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
            // Upper body: spine, arms, head (no legs)
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
