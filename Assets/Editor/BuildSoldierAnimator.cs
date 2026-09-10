using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BuildSoldierAnimator
{
    const string ANIM_PATH = "Assets/CowboyAssets/Animations/";
    const string OUT_PATH  = "Assets/Soldier/Animations/SoldierAnimator.controller";

    static AnimationClip Clip(string folder, string name)
    {
        string path = ANIM_PATH + folder + "/" + name + ".fbx";
        var clips = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in clips)
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                return c;
        Debug.LogError("[BuildAnimator] Clip not found: " + path);
        return null;
    }

    static BlendTree Make2DBlendTree(AnimatorController ac, string name,
        string paramH, string paramV,
        (AnimationClip clip, float x, float y)[] motions)
    {
        var bt = new BlendTree();
        ac.CreateBlendTreeInController(name, out bt);
        bt.name           = name;
        bt.blendType      = BlendTreeType.FreeformDirectional2D;
        bt.blendParameter  = paramH;
        bt.blendParameterY = paramV;
        foreach (var m in motions)
            bt.AddChild(m.clip, new Vector2(m.x, m.y));
        return bt;
    }

    static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to,
        float duration = 0.1f, bool hasExitTime = false)
    {
        var t = from.AddTransition(to);
        t.duration     = duration;
        t.hasExitTime  = hasExitTime;
        t.exitTime     = 0.9f;
        return t;
    }

    static AnimatorStateTransition AddAnyTransition(AnimatorStateMachine sm, AnimatorState to,
        float duration = 0.1f)
    {
        var t = sm.AddAnyStateTransition(to);
        t.duration    = duration;
        t.hasExitTime = false;
        t.canTransitionToSelf = false;
        return t;
    }

    [MenuItem("Tools/Build Soldier Animator")]
    public static void Build()
    {
        // ── Delete old controller ──────────────────────────────────────────
        AssetDatabase.DeleteAsset(OUT_PATH);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(OUT_PATH);
        var sm = ac.layers[0].stateMachine;

        // ── Parameters ────────────────────────────────────────────────────
        ac.AddParameter("Horizontal",   AnimatorControllerParameterType.Float);
        ac.AddParameter("Vertical",     AnimatorControllerParameterType.Float);
        ac.AddParameter("isIdle",       AnimatorControllerParameterType.Bool);
        ac.AddParameter("isWalking",    AnimatorControllerParameterType.Bool);
        ac.AddParameter("isRunning",    AnimatorControllerParameterType.Bool);
        ac.AddParameter("isCrouching",  AnimatorControllerParameterType.Bool);
        ac.AddParameter("isJumping",    AnimatorControllerParameterType.Bool);
        ac.AddParameter("isDead",       AnimatorControllerParameterType.Bool);
        ac.AddParameter("isHeadshot",   AnimatorControllerParameterType.Bool);

        // ── Clips ─────────────────────────────────────────────────────────
        var cIdle          = Clip("idle",       "idle");
        var cIdleCrouch    = Clip("idle",       "idle crouching");

        var cWalkF         = Clip("Walking",    "walk forward");
        var cWalkB         = Clip("Walking",    "walk backward");
        var cWalkL         = Clip("Walking",    "walk left");
        var cWalkR         = Clip("Walking",    "walk right");
        var cWalkFL        = Clip("Walking",    "walk forward left");
        var cWalkFR        = Clip("Walking",    "walk forward right");
        var cWalkBL        = Clip("Walking",    "walk backward left");
        var cWalkBR        = Clip("Walking",    "walk backward right");

        var cSprintF       = Clip("Sprinting",  "sprint forward");
        var cSprintB       = Clip("Sprinting",  "sprint backward");
        var cSprintL       = Clip("Sprinting",  "sprint left");
        var cSprintR       = Clip("Sprinting",  "sprint right");
        var cSprintFL      = Clip("Sprinting",  "sprint forward left");
        var cSprintFR      = Clip("Sprinting",  "sprint forward right");
        var cSprintBL      = Clip("Sprinting",  "sprint backward left");
        var cSprintBR      = Clip("Sprinting",  "sprint backward right");

        var cCrouchF       = Clip("crouching",  "walk crouching forward");
        var cCrouchB       = Clip("crouching",  "walk crouching backward");
        var cCrouchL       = Clip("crouching",  "walk crouching left");
        var cCrouchR       = Clip("crouching",  "walk crouching right");
        var cCrouchFL      = Clip("crouching",  "walk crouching forward left");
        var cCrouchFR      = Clip("crouching",  "walk crouching forward right");
        var cCrouchBL      = Clip("crouching",  "walk crouching backward left");
        var cCrouchBR      = Clip("crouching",  "walk crouching backward right");

        var cJumpUp        = Clip("Jumping",    "jump up");
        var cJumpLoop      = Clip("Jumping",    "jump loop");
        var cJumpDown      = Clip("Jumping",    "jump down");

        var cDeathBody     = Clip("Dying",      "death from bodyshot");
        var cDeathHead     = Clip("Dying",      "death from headshot");
        var cDeathCrouch   = Clip("Dying",      "death crouching");

        // ── States ────────────────────────────────────────────────────────
        // Idle
        var sIdle = sm.AddState("Idle", new Vector3(-200, 0));
        sIdle.motion = cIdle;
        sm.defaultState = sIdle;

        // Idle Crouch
        var sIdleCrouch = sm.AddState("Idle Crouch", new Vector3(-200, 100));
        sIdleCrouch.motion = cIdleCrouch;

        // Walk blend tree
        var sWalk = sm.AddState("Walk", new Vector3(200, -100));
        BlendTree btWalk;
        ac.CreateBlendTreeInController("Walk BT", out btWalk);
        btWalk.blendType       = BlendTreeType.FreeformDirectional2D;
        btWalk.blendParameter  = "Horizontal";
        btWalk.blendParameterY = "Vertical";
        btWalk.AddChild(cWalkF,  new Vector2( 0,  1));
        btWalk.AddChild(cWalkB,  new Vector2( 0, -1));
        btWalk.AddChild(cWalkL,  new Vector2(-1,  0));
        btWalk.AddChild(cWalkR,  new Vector2( 1,  0));
        btWalk.AddChild(cWalkFL, new Vector2(-1,  1));
        btWalk.AddChild(cWalkFR, new Vector2( 1,  1));
        btWalk.AddChild(cWalkBL, new Vector2(-1, -1));
        btWalk.AddChild(cWalkBR, new Vector2( 1, -1));
        sWalk.motion = btWalk;

        // Sprint blend tree
        var sSprint = sm.AddState("Sprint", new Vector3(200, 0));
        BlendTree btSprint;
        ac.CreateBlendTreeInController("Sprint BT", out btSprint);
        btSprint.blendType       = BlendTreeType.FreeformDirectional2D;
        btSprint.blendParameter  = "Horizontal";
        btSprint.blendParameterY = "Vertical";
        btSprint.AddChild(cSprintF,  new Vector2( 0,  1));
        btSprint.AddChild(cSprintB,  new Vector2( 0, -1));
        btSprint.AddChild(cSprintL,  new Vector2(-1,  0));
        btSprint.AddChild(cSprintR,  new Vector2( 1,  0));
        btSprint.AddChild(cSprintFL, new Vector2(-1,  1));
        btSprint.AddChild(cSprintFR, new Vector2( 1,  1));
        btSprint.AddChild(cSprintBL, new Vector2(-1, -1));
        btSprint.AddChild(cSprintBR, new Vector2( 1, -1));
        sSprint.motion = btSprint;

        // Crouch blend tree
        var sCrouch = sm.AddState("Crouch Walk", new Vector3(200, 100));
        BlendTree btCrouch;
        ac.CreateBlendTreeInController("Crouch BT", out btCrouch);
        btCrouch.blendType       = BlendTreeType.FreeformDirectional2D;
        btCrouch.blendParameter  = "Horizontal";
        btCrouch.blendParameterY = "Vertical";
        btCrouch.AddChild(cCrouchF,  new Vector2( 0,  1));
        btCrouch.AddChild(cCrouchB,  new Vector2( 0, -1));
        btCrouch.AddChild(cCrouchL,  new Vector2(-1,  0));
        btCrouch.AddChild(cCrouchR,  new Vector2( 1,  0));
        btCrouch.AddChild(cCrouchFL, new Vector2(-1,  1));
        btCrouch.AddChild(cCrouchFR, new Vector2( 1,  1));
        btCrouch.AddChild(cCrouchBL, new Vector2(-1, -1));
        btCrouch.AddChild(cCrouchBR, new Vector2( 1, -1));
        sCrouch.motion = btCrouch;

        // Jump states
        var sJumpUp   = sm.AddState("Jump Up",   new Vector3(500, -100));
        var sJumpLoop = sm.AddState("Jump Loop",  new Vector3(500,    0));
        var sJumpDown = sm.AddState("Jump Down",  new Vector3(500,  100));
        sJumpUp.motion   = cJumpUp;
        sJumpLoop.motion = cJumpLoop;
        sJumpDown.motion = cJumpDown;

        // Death states
        var sDeathBody   = sm.AddState("Death Body",   new Vector3(-200, 300));
        var sDeathHead   = sm.AddState("Death Head",   new Vector3(-200, 400));
        var sDeathCrouch = sm.AddState("Death Crouch", new Vector3(-200, 500));
        sDeathBody.motion   = cDeathBody;
        sDeathHead.motion   = cDeathHead;
        sDeathCrouch.motion = cDeathCrouch;

        // ── Transitions ───────────────────────────────────────────────────

        // Idle -> Walk
        var t = AddTransition(sIdle, sWalk, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isWalking");

        // Idle -> Sprint
        t = AddTransition(sIdle, sSprint, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");

        // Idle -> Crouch Idle
        t = AddTransition(sIdle, sIdleCrouch, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");

        // Walk -> Idle
        t = AddTransition(sWalk, sIdle, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isIdle");

        // Walk -> Sprint
        t = AddTransition(sWalk, sSprint, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");

        // Walk -> Crouch
        t = AddTransition(sWalk, sCrouch, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");

        // Sprint -> Idle
        t = AddTransition(sSprint, sIdle, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isIdle");

        // Sprint -> Walk
        t = AddTransition(sSprint, sWalk, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isWalking");

        // Crouch Idle -> Idle
        t = AddTransition(sIdleCrouch, sIdle, 0.1f);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");

        // Crouch Idle -> Crouch Walk
        t = AddTransition(sIdleCrouch, sCrouch, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isWalking");

        // Crouch Walk -> Crouch Idle
        t = AddTransition(sCrouch, sIdleCrouch, 0.1f);
        t.AddCondition(AnimatorConditionMode.If, 0, "isIdle");

        // Crouch Walk -> Idle (uncrouch while moving)
        t = AddTransition(sCrouch, sIdle, 0.1f);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");

        // Any -> Jump Up
        var at = AddAnyTransition(sm, sJumpUp, 0.1f);
        at.AddCondition(AnimatorConditionMode.If, 0, "isJumping");

        // Jump Up -> Jump Loop (exit time)
        t = AddTransition(sJumpUp, sJumpLoop, 0.1f, true);

        // Jump Loop -> Jump Down
        t = AddTransition(sJumpLoop, sJumpDown, 0.1f);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isJumping");

        // Jump Down -> Idle (exit time)
        t = AddTransition(sJumpDown, sIdle, 0.1f, true);

        // Any -> Death Body
        at = AddAnyTransition(sm, sDeathBody, 0.1f);
        at.AddCondition(AnimatorConditionMode.If,    0, "isDead");
        at.AddCondition(AnimatorConditionMode.IfNot, 0, "isHeadshot");
        at.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");

        // Any -> Death Head
        at = AddAnyTransition(sm, sDeathHead, 0.1f);
        at.AddCondition(AnimatorConditionMode.If,    0, "isDead");
        at.AddCondition(AnimatorConditionMode.If,    0, "isHeadshot");
        at.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");

        // Any -> Death Crouch
        at = AddAnyTransition(sm, sDeathCrouch, 0.1f);
        at.AddCondition(AnimatorConditionMode.If, 0, "isDead");
        at.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");

        AssetDatabase.SaveAssets();
        Debug.Log("[BuildAnimator] SoldierAnimator built successfully!");
    }
}
