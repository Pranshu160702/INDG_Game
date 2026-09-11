using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BuildJamesAnimator : EditorWindow
{
    [MenuItem("Tools/Build James Animator")]
    public static void Build()
    {
        string animBase = "Assets/Characters/James/Animations/";
        string outPath  = "Assets/Characters/James/Animator/JamesAnimator.controller";

        var controller = AnimatorController.CreateAnimatorControllerAtPath(outPath);

        // --- Parameters ---
        controller.AddParameter("isIdle",           AnimatorControllerParameterType.Bool);
        controller.AddParameter("isWalking",         AnimatorControllerParameterType.Bool);
        controller.AddParameter("isRunning",         AnimatorControllerParameterType.Bool);
        controller.AddParameter("isCrouching",       AnimatorControllerParameterType.Bool);
        controller.AddParameter("isJumping",         AnimatorControllerParameterType.Bool);
        controller.AddParameter("isDead",            AnimatorControllerParameterType.Bool);
        controller.AddParameter("isHeadshot",        AnimatorControllerParameterType.Bool);
        controller.AddParameter("isCrouchWalking",   AnimatorControllerParameterType.Bool);
        controller.AddParameter("Horizontal",        AnimatorControllerParameterType.Float);
        controller.AddParameter("Vertical",          AnimatorControllerParameterType.Float);

        var parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
            if (parameters[i].name == "isIdle") { parameters[i].defaultBool = true; break; }
        controller.parameters = parameters;

        var rootSM = controller.layers[0].stateMachine;

        AnimationClip Clip(string path)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(animBase + path);
            foreach (var a in clips)
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    return c;
            Debug.LogWarning($"[JamesAnimator] Clip not found: {path}");
            return null;
        }

        // --- States ---
        var idleState = rootSM.AddState("Idle", new Vector3(250, 0));
        idleState.motion = Clip("idle/idle.fbx");
        rootSM.defaultState = idleState;

        var idleCrouchState = rootSM.AddState("Idle Crouch", new Vector3(250, 80));
        idleCrouchState.motion = Clip("idle/idle crouching.fbx");

        // Walk Blend Tree
        var walkState = rootSM.AddState("Walk", new Vector3(550, 0));
        var walkTree  = new BlendTree();
        AssetDatabase.AddObjectToAsset(walkTree, controller);
        walkTree.name            = "Walk Blend Tree";
        walkTree.blendType       = BlendTreeType.FreeformDirectional2D;
        walkTree.blendParameter  = "Horizontal";
        walkTree.blendParameterY = "Vertical";
        walkTree.AddChild(Clip("Walking/walk forward.fbx"),        new Vector2(0,  1));
        walkTree.AddChild(Clip("Walking/walk backward.fbx"),       new Vector2(0, -1));
        walkTree.AddChild(Clip("Walking/walk left.fbx"),           new Vector2(-1, 0));
        walkTree.AddChild(Clip("Walking/walk right.fbx"),          new Vector2( 1, 0));
        walkTree.AddChild(Clip("Walking/walk forward left.fbx"),   new Vector2(-1, 1));
        walkTree.AddChild(Clip("Walking/walk forward right.fbx"),  new Vector2( 1, 1));
        walkTree.AddChild(Clip("Walking/walk backward left.fbx"),  new Vector2(-1,-1));
        walkTree.AddChild(Clip("Walking/walk backward right.fbx"), new Vector2( 1,-1));
        walkState.motion = walkTree;

        // Sprint Blend Tree
        var sprintState = rootSM.AddState("Sprint", new Vector3(550, 80));
        var sprintTree  = new BlendTree();
        AssetDatabase.AddObjectToAsset(sprintTree, controller);
        sprintTree.name            = "Sprint Blend Tree";
        sprintTree.blendType       = BlendTreeType.FreeformDirectional2D;
        sprintTree.blendParameter  = "Horizontal";
        sprintTree.blendParameterY = "Vertical";
        sprintTree.AddChild(Clip("Sprinting/sprint forward.fbx"),        new Vector2(0,  1));
        sprintTree.AddChild(Clip("Sprinting/sprint backward.fbx"),       new Vector2(0, -1));
        sprintTree.AddChild(Clip("Sprinting/sprint left.fbx"),           new Vector2(-1, 0));
        sprintTree.AddChild(Clip("Sprinting/sprint right.fbx"),          new Vector2( 1, 0));
        sprintTree.AddChild(Clip("Sprinting/sprint forward left.fbx"),   new Vector2(-1, 1));
        sprintTree.AddChild(Clip("Sprinting/sprint forward right.fbx"),  new Vector2( 1, 1));
        sprintTree.AddChild(Clip("Sprinting/sprint backward left.fbx"),  new Vector2(-1,-1));
        sprintTree.AddChild(Clip("Sprinting/sprint backward right.fbx"), new Vector2( 1,-1));
        sprintState.motion = sprintTree;

        // Crouch Walk Blend Tree
        var crouchWalkState = rootSM.AddState("Crouch Walk", new Vector3(550, 160));
        var crouchTree      = new BlendTree();
        AssetDatabase.AddObjectToAsset(crouchTree, controller);
        crouchTree.name            = "Crouch Walk Blend Tree";
        crouchTree.blendType       = BlendTreeType.FreeformDirectional2D;
        crouchTree.blendParameter  = "Horizontal";
        crouchTree.blendParameterY = "Vertical";
        crouchTree.AddChild(Clip("crouching/walk crouching forward.fbx"),        new Vector2(0,  1));
        crouchTree.AddChild(Clip("crouching/walk crouching backward.fbx"),       new Vector2(0, -1));
        crouchTree.AddChild(Clip("crouching/walk crouching left.fbx"),           new Vector2(-1, 0));
        crouchTree.AddChild(Clip("crouching/walk crouching right.fbx"),          new Vector2( 1, 0));
        crouchTree.AddChild(Clip("crouching/walk crouching forward left.fbx"),   new Vector2(-1, 1));
        crouchTree.AddChild(Clip("crouching/walk crouching forward right.fbx"),  new Vector2( 1, 1));
        crouchTree.AddChild(Clip("crouching/walk crouching backward left.fbx"),  new Vector2(-1,-1));
        crouchTree.AddChild(Clip("crouching/walk crouching backward right.fbx"), new Vector2( 1,-1));
        crouchWalkState.motion = crouchTree;

        // Jump
        var jumpState = rootSM.AddState("Jump", new Vector3(850, -100));
        jumpState.motion = Clip("Jumping/jump loop.fbx");
        jumpState.speed  = 1.1f;

        // Death states
        var deathState         = rootSM.AddState("Death",          new Vector3(250, 160));
        var deathHeadshotState = rootSM.AddState("Death Headshot", new Vector3(550, 160));
        var deathCrouchState   = rootSM.AddState("Death Crouch",   new Vector3(850, 160));
        deathState.motion         = Clip("Dying/death from bodyshot.fbx");
        deathHeadshotState.motion = Clip("Dying/death from headshot.fbx");
        deathCrouchState.motion   = Clip("Dying/death crouching.fbx");

        // --- Transitions ---
        float td = 0.15f;

        // Idle -> Walk
        var t = idleState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        t.duration = td; t.hasExitTime = false;

        // Idle -> Sprint
        t = idleState.AddTransition(sprintState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
        t.duration = td; t.hasExitTime = false;

        // Idle -> Idle Crouch
        t = idleState.AddTransition(idleCrouchState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouchWalking");
        t.duration = td; t.hasExitTime = false;

        // Idle -> Crouch Walk
        t = idleState.AddTransition(crouchWalkState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouchWalking");
        t.duration = td; t.hasExitTime = false;

        // Idle -> Jump
        t = idleState.AddTransition(jumpState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isJumping");
        t.duration = 0.32f; t.hasExitTime = false;

        // Walk -> Idle
        t = walkState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        t.duration = td; t.hasExitTime = false;

        // Walk -> Sprint
        t = walkState.AddTransition(sprintState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
        t.duration = td; t.hasExitTime = false;

        // Walk -> Idle Crouch
        t = walkState.AddTransition(idleCrouchState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouchWalking");
        t.duration = td; t.hasExitTime = false;

        // Walk -> Crouch Walk
        t = walkState.AddTransition(crouchWalkState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouchWalking");
        t.duration = td; t.hasExitTime = false;

        // Sprint -> Idle
        t = sprintState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        t.duration = td; t.hasExitTime = false;

        // Sprint -> Walk
        t = sprintState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        t.duration = td; t.hasExitTime = false;

        // Sprint -> Idle Crouch
        t = sprintState.AddTransition(idleCrouchState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouchWalking");
        t.duration = td; t.hasExitTime = false;

        // Sprint -> Crouch Walk
        t = sprintState.AddTransition(crouchWalkState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouchWalking");
        t.duration = td; t.hasExitTime = false;

        // Idle Crouch -> Idle
        t = idleCrouchState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        t.duration = td; t.hasExitTime = false;

        // Idle Crouch -> Crouch Walk
        t = idleCrouchState.AddTransition(crouchWalkState);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouchWalking");
        t.duration = td; t.hasExitTime = false;

        // Crouch Walk -> Idle Crouch
        t = crouchWalkState.AddTransition(idleCrouchState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouchWalking");
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        t.duration = td; t.hasExitTime = false;

        // Crouch Walk -> Idle
        t = crouchWalkState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
        t.duration = td; t.hasExitTime = false;

        // Crouch Walk -> Walk
        t = crouchWalkState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        t.duration = td; t.hasExitTime = false;

        // Crouch Walk -> Sprint
        t = crouchWalkState.AddTransition(sprintState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
        t.duration = td; t.hasExitTime = false;

        // Jump -> Idle
        t = jumpState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isJumping");
        t.hasExitTime = true; t.exitTime = 0.39f; t.duration = 0.57f; t.hasFixedDuration = true;

        // Any State -> Death Crouch
        var anyDeathCrouch = rootSM.AddAnyStateTransition(deathCrouchState);
        anyDeathCrouch.AddCondition(AnimatorConditionMode.If, 0, "isDead");
        anyDeathCrouch.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        anyDeathCrouch.duration = td; anyDeathCrouch.hasExitTime = false; anyDeathCrouch.canTransitionToSelf = false;

        // Any State -> Death Headshot
        var anyHeadshot = rootSM.AddAnyStateTransition(deathHeadshotState);
        anyHeadshot.AddCondition(AnimatorConditionMode.If, 0, "isHeadshot");
        anyHeadshot.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        anyHeadshot.duration = td; anyHeadshot.hasExitTime = false; anyHeadshot.canTransitionToSelf = false;

        // Any State -> Death Body
        var anyDeath = rootSM.AddAnyStateTransition(deathState);
        anyDeath.AddCondition(AnimatorConditionMode.If, 0, "isDead");
        anyDeath.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        anyDeath.AddCondition(AnimatorConditionMode.IfNot, 0, "isHeadshot");
        anyDeath.duration = td; anyDeath.hasExitTime = false; anyDeath.canTransitionToSelf = false;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[JamesAnimator] Built successfully!");
        EditorUtility.DisplayDialog("Done", "JamesAnimator built successfully!", "OK");
    }
}
