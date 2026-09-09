using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PatchDemo1EventSystem
{
    [MenuItem("Tools/Patch Demo1 EventSystem (run once)")]
    public static void Patch()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/SlimUI/Modern Menu 1/Scenes/Demos/Demo1.unity",
            OpenSceneMode.Single);

        GameObject esGO = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            esGO = FindDeep(root, "EventSystem");
            if (esGO != null) break;
        }

        if (esGO == null)
        {
            Debug.LogError("EventSystem not found in Demo1.");
            return;
        }

        // Remove all old input modules
        foreach (var m in esGO.GetComponents<UnityEngine.EventSystems.BaseInputModule>())
        {
            Object.DestroyImmediate(m);
            Debug.Log($"Removed: {m.GetType().Name}");
        }

        // Add new one
        if (esGO.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Demo1 EventSystem patched and saved.");
    }

    static GameObject FindDeep(GameObject root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root.transform)
        {
            var r = FindDeep(child.gameObject, name);
            if (r != null) return r;
        }
        return null;
    }
}
