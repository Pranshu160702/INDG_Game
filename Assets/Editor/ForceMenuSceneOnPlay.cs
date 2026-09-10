using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class ForceMenuSceneOnPlay
{
    static ForceMenuSceneOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        string[] targetScenes = { "Game", "Lobby", "Rooms", "Menu" };
        string currentPath = EditorSceneManager.GetActiveScene().path;
        bool isTargetScene = System.Array.Exists(targetScenes, s => currentPath.Contains(s));

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorPrefs.SetString("LastEditScene", currentPath);

            if (isTargetScene && !currentPath.Contains("Menu"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            string lastScene = EditorPrefs.GetString("LastEditScene", "");
            if (!string.IsNullOrEmpty(lastScene) && !lastScene.Contains("Menu"))
                EditorSceneManager.OpenScene(lastScene);
        }
    }
}
