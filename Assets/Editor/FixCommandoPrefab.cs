using UnityEditor;

public class FixCommandoPrefab
{
    [MenuItem("Tools/Fix Commando Prefab SceneId")]
    static void Fix()
    {
        string path = "Assets/Prefabs/Commando.prefab";
        var prefab = AssetDatabase.LoadMainAssetAtPath(path);
        if (prefab == null) { UnityEngine.Debug.LogError("Commando.prefab not found at " + path); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            EditorUtility.SetDirty(scope.prefabContentsRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("Commando prefab resaved — sceneId will be assigned by Mirror on next build.");
    }
}
