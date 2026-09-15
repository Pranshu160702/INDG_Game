using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BuildFPSArmsAnimator
{
    const string FbxPath        = "Assets/fpshands/rifle-animated/source/arms@FAL.fbx";
    const string ControllerPath = "Assets/fpshands/rifle-animated/FPSArmsController.controller";

    [MenuItem("INDG/Setup/Build FPS Arms Animator Controller")]
    static void Build()
    {
        // --- Extract clips from FBX ---
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
        AnimationClip GetClip(string name)
        {
            foreach (var a in allAssets)
                if (a is AnimationClip c && c.name == name) return c;
            Debug.LogWarning($"[FPSArms] Clip '{name}' not found in FBX.");
            return null;
        }

        AnimationClip fire       = GetClip("fire");
        AnimationClip reload     = GetClip("reload full");
        AnimationClip reloadClip = GetClip("reloadclip");
        AnimationClip hide       = GetClip("hide");
        AnimationClip ready      = GetClip("ready");
        AnimationClip ambient    = GetClip("ambient");
        AnimationClip melee      = GetClip("melee");

        if (fire == null || ambient == null)
        {
            Debug.LogError("[FPSArms] Could not find required clips. Make sure you have defined them in the FBX import settings (Animation tab).");
            return;
        }

        // Warn about optional clips but continue
        if (reload == null)     Debug.LogWarning("[FPSArms] 'reload full' clip not found — Reload state will have no motion.");
        if (reloadClip == null) Debug.LogWarning("[FPSArms] 'reloadclip' clip not found — ReloadClip state will have no motion.");
        if (hide == null)       Debug.LogWarning("[FPSArms] 'hide' clip not found — Hide state will have no motion.");
        if (ready == null)      Debug.LogWarning("[FPSArms] 'ready' clip not found — Ready state will have no motion.");
        if (melee == null)      Debug.LogWarning("[FPSArms] 'melee' clip not found — Melee state will have no motion.");

        // --- Create controller ---
        if (System.IO.File.Exists(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Parameters
        controller.AddParameter("fire",   AnimatorControllerParameterType.Trigger);
        controller.AddParameter("reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("hide",   AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ready",  AnimatorControllerParameterType.Trigger);
        controller.AddParameter("melee",  AnimatorControllerParameterType.Trigger);

        var root = controller.layers[0].stateMachine;

        // States
        AnimatorState AddState(string name, AnimationClip clip)
        {
            var s = root.AddState(name);
            s.motion = clip;
            return s;
        }

        var stateAmbient    = AddState("Ambient",    ambient);
        var stateFire       = AddState("Fire",       fire);
        var stateReload     = AddState("Reload",     reload);
        var stateReloadClip = AddState("ReloadClip", reloadClip);
        var stateHide       = AddState("Hide",       hide);
        var stateReady      = AddState("Ready",      ready);
        var stateMelee      = AddState("Melee",      melee);

        root.defaultState = stateAmbient;

        // Helper: trigger transition from any state back to ambient after clip finishes
        void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger, bool hasExitTime = false)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = hasExitTime;
            t.exitTime = 1f;
            t.duration = 0.05f;
            if (!string.IsNullOrEmpty(trigger))
                t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        }

        // From Ambient: trigger transitions out
        AddTriggerTransition(stateAmbient, stateFire,       "fire");
        AddTriggerTransition(stateAmbient, stateReload,     "reload");
        AddTriggerTransition(stateAmbient, stateHide,       "hide");
        AddTriggerTransition(stateAmbient, stateMelee,      "melee");

        // From Ready: trigger transitions out
        AddTriggerTransition(stateReady,   stateFire,       "fire");
        AddTriggerTransition(stateReady,   stateReload,     "reload");
        AddTriggerTransition(stateReady,   stateHide,       "hide");
        AddTriggerTransition(stateReady,   stateMelee,      "melee");

        // Auto-return to Ambient after each action clip finishes
        AddTriggerTransition(stateFire,       stateAmbient, "", true);
        AddTriggerTransition(stateReload,     stateAmbient, "", true);
        AddTriggerTransition(stateReloadClip, stateAmbient, "", true);
        AddTriggerTransition(stateHide,       stateReady,   "", true);  // hide → ready (weapon drawn)
        // NOTE: Ready does NOT auto-return to Ambient — it waits for a trigger
        AddTriggerTransition(stateMelee,      stateAmbient, "", true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FPSArms] Controller built at {ControllerPath}. Now do the following in the prefab:\n" +
                  "1. Select the arms@FAL GameObject under FPSCamera/FPSArms\n" +
                  "2. Make sure it has an Animator component\n" +
                  "3. Drag FPSArmsController into the Animator's Controller slot\n" +
                  "4. Make sure FPSArmsAnimator component is on FPSArms (or arms@FAL) with the Controller slot also filled");
    }
}
