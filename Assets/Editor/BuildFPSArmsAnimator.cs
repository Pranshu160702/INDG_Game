using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BuildFPSArmsAnimator
{
    const string ControllerPath = "Assets/FPSHands/FPSArmsController.controller";

    const string PlaceholderDir = "Assets/FPSHands/Placeholders";

    [MenuItem("INDG/Setup/Build FPS Arms Controller")]
    static void Build()
    {
        if (System.IO.File.Exists(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);

        // Create placeholder clip directory
        if (!AssetDatabase.IsValidFolder(PlaceholderDir))
            AssetDatabase.CreateFolder("Assets/FPSHands", "Placeholders");

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // ── Parameters (all Triggers) ────────────────────────────
        controller.AddParameter("fire",             AnimatorControllerParameterType.Trigger);
        controller.AddParameter("equip",            AnimatorControllerParameterType.Trigger);
        controller.AddParameter("reload",           AnimatorControllerParameterType.Trigger);
        controller.AddParameter("reload_emptymag",  AnimatorControllerParameterType.Trigger);
        controller.AddParameter("knife1",           AnimatorControllerParameterType.Trigger);
        controller.AddParameter("knife2",           AnimatorControllerParameterType.Trigger);
        controller.AddParameter("knife3",           AnimatorControllerParameterType.Trigger);
        controller.AddParameter("knife4",           AnimatorControllerParameterType.Trigger);

        var root = controller.layers[0].stateMachine;

        // ── States ───────────────────────────────────────────────
        // Each state needs a real placeholder clip so AnimatorOverrideController
        // has something to override at runtime.
        AnimationClip MakePlaceholder(string clipName)
        {
            string path = $"{PlaceholderDir}/{clipName}.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) return existing;
            var clip = new AnimationClip { name = clipName };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        AnimatorState MakeState(string stateName, Vector3 pos)
        {
            var s = root.AddState(stateName, pos);
            s.motion = MakePlaceholder(stateName);
            s.writeDefaultValues = true;
            return s;
        }

        var stateIdle          = MakeState("idle",            new Vector3(200,   0, 0));
        var stateEquip         = MakeState("equip",           new Vector3(400,   0, 0));
        var stateFire          = MakeState("fire",            new Vector3(400,  70, 0));
        var stateReload        = MakeState("reload",          new Vector3(400, 140, 0));
        var stateReloadEmpty   = MakeState("reload_emptymag", new Vector3(400, 210, 0));
        var stateKnife1        = MakeState("knife1",          new Vector3(400, 280, 0));
        var stateKnife2        = MakeState("knife2",          new Vector3(400, 350, 0));
        var stateKnife3        = MakeState("knife3",          new Vector3(400, 420, 0));
        var stateKnife4        = MakeState("knife4",          new Vector3(400, 490, 0));

        root.defaultState = stateIdle;

        // ── Helper ───────────────────────────────────────────────
        AnimatorStateTransition Trigger(AnimatorState from, AnimatorState to, string param)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration    = 0.05f;
            t.AddCondition(AnimatorConditionMode.If, 0, param);
            return t;
        }

        AnimatorStateTransition ExitToIdle(AnimatorState from)
        {
            var t = from.AddTransition(stateIdle);
            t.hasExitTime = true;
            t.exitTime    = 1f;
            t.duration    = 0.05f;
            return t;
        }

        // ── Transitions from Idle ────────────────────────────────
        Trigger(stateIdle, stateEquip,       "equip");
        Trigger(stateIdle, stateFire,        "fire");
        Trigger(stateIdle, stateReload,      "reload");
        Trigger(stateIdle, stateReloadEmpty, "reload_emptymag");
        Trigger(stateIdle, stateKnife1,      "knife1");
        Trigger(stateIdle, stateKnife2,      "knife2");
        Trigger(stateIdle, stateKnife3,      "knife3");
        Trigger(stateIdle, stateKnife4,      "knife4");

        // ── All action states return to Idle on finish ───────────
        ExitToIdle(stateEquip);
        ExitToIdle(stateFire);
        ExitToIdle(stateReload);
        ExitToIdle(stateReloadEmpty);
        ExitToIdle(stateKnife1);
        ExitToIdle(stateKnife2);
        ExitToIdle(stateKnife3);
        ExitToIdle(stateKnife4);

        // ── Fire can also be triggered from itself (auto-fire) ───
        Trigger(stateFire, stateFire, "fire");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FPSArms] FPSArmsController built at " + ControllerPath +
                  "\nStates: idle, equip, fire, reload, reload_emptymag, knife1, knife2, knife3, knife4" +
                  "\nAll clips are empty — assign them via FPSArmsAnimator.guns[] entries in the prefab.");
    }
}
