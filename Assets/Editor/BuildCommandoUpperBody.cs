using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BuildCommandoUpperBody
{
    const string ControllerPath = "Assets/Characters/Commando/Animator/CommandoAnimator.controller";
    const string PlaceholderDir = "Assets/Characters/Commando/Animator/Placeholders";

    [MenuItem("INDG/Setup/Rebuild Commando Upper Body Layer")]
    static void Build()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) { Debug.LogError("CommandoAnimator not found at " + ControllerPath); return; }

        if (!AssetDatabase.IsValidFolder(PlaceholderDir))
            AssetDatabase.CreateFolder("Assets/Characters/Commando/Animator", "Placeholders");

        // ── Parameters ───────────────────────────────────────────
        AddBoolParam(controller, "isEquipping");
        AddBoolParam(controller, "isReloadingEmpty");
        AddBoolParam(controller, "isMelee1");
        AddBoolParam(controller, "isMelee2");
        AddBoolParam(controller, "isMelee3");
        AddBoolParam(controller, "isMelee4");
        RemoveParam(controller, "isMelee");
        RemoveParam(controller, "isAiming");

        // ── Find Upper Body layer ─────────────────────────────────
        var layers = controller.layers;
        int ubIdx = -1;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i].name == "Upper Body") { ubIdx = i; break; }
        if (ubIdx < 0) { Debug.LogError("Upper Body layer not found."); return; }

        var sm = layers[ubIdx].stateMachine;
        foreach (var cs in sm.states)             sm.RemoveState(cs.state);
        foreach (var t  in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);

        // ── States with placeholder clips ─────────────────────────
        var sIdle        = MakeState(sm, "Idle Upper",               new Vector3(250,   0, 0));
        var sEquip       = MakeState(sm, "Equip Upper",              new Vector3(550,   0, 0));
        var sFire        = MakeState(sm, "Firing Upper",             new Vector3(550,  80, 0));
        var sReload      = MakeState(sm, "Reloading Upper",          new Vector3(550, 160, 0));
        var sReloadEmpty = MakeState(sm, "Reloading EmptyMag Upper", new Vector3(550, 240, 0));
        var sKnife1      = MakeState(sm, "Knife1 Upper",             new Vector3(550, 320, 0));
        var sKnife2      = MakeState(sm, "Knife2 Upper",             new Vector3(550, 400, 0));
        var sKnife3      = MakeState(sm, "Knife3 Upper",             new Vector3(550, 480, 0));
        var sKnife4      = MakeState(sm, "Knife4 Upper",             new Vector3(550, 560, 0));

        sm.defaultState = sIdle;

        // ── AnyState → action states ──────────────────────────────
        AnyTo(sm, sEquip,       "isEquipping",      0.05f);
        AnyTo(sm, sFire,        "isFiring",         0.05f);
        AnyTo(sm, sReload,      "isReloading",      0.1f);
        AnyTo(sm, sReloadEmpty, "isReloadingEmpty", 0.1f);
        AnyTo(sm, sKnife1,      "isMelee1",         0.05f);
        AnyTo(sm, sKnife2,      "isMelee2",         0.05f);
        AnyTo(sm, sKnife3,      "isMelee3",         0.05f);
        AnyTo(sm, sKnife4,      "isMelee4",         0.05f);

        // ── Action states → Idle Upper on bool false ──────────────
        ExitToIdle(sEquip,       sIdle, "isEquipping",      0.1f);
        ExitToIdle(sFire,        sIdle, "isFiring",         0.05f);
        ExitToIdle(sReload,      sIdle, "isReloading",      0.1f);
        ExitToIdle(sReloadEmpty, sIdle, "isReloadingEmpty", 0.1f);
        ExitToIdle(sKnife1,      sIdle, "isMelee1",         0.05f);
        ExitToIdle(sKnife2,      sIdle, "isMelee2",         0.05f);
        ExitToIdle(sKnife3,      sIdle, "isMelee3",         0.05f);
        ExitToIdle(sKnife4,      sIdle, "isMelee4",         0.05f);

        layers[ubIdx].stateMachine = sm;
        controller.layers = layers;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[INDG] Commando Upper Body layer rebuilt. Wire tpsAnimator + tpsBaseController on FPSArmsAnimator, then fill TPS clips per GunEntry.");
    }

    static AnimatorState MakeState(AnimatorStateMachine sm, string stateName, Vector3 pos)
    {
        string path = PlaceholderDir + "/" + stateName + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = stateName };
            AssetDatabase.CreateAsset(clip, path);
        }
        var s = sm.AddState(stateName, pos);
        s.motion = clip;
        s.writeDefaultValues = true;
        return s;
    }

    static void AddBoolParam(AnimatorController c, string name)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, AnimatorControllerParameterType.Bool);
    }

    static void RemoveParam(AnimatorController c, string name)
    {
        var list = new System.Collections.Generic.List<AnimatorControllerParameter>(c.parameters);
        list.RemoveAll(p => p.name == name);
        c.parameters = list.ToArray();
    }

    static void AnyTo(AnimatorStateMachine sm, AnimatorState dst, string param, float duration)
    {
        var t = sm.AddAnyStateTransition(dst);
        t.hasExitTime = false;
        t.duration    = duration;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, param);
    }

    static void ExitToIdle(AnimatorState from, AnimatorState idle, string param, float duration)
    {
        var t = from.AddTransition(idle);
        t.hasExitTime = false;
        t.duration    = duration;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, param);
    }
}
