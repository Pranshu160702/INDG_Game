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
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save current scene path so we can restore it after play
            EditorPrefs.SetString("LastEditScene", EditorSceneManager.GetActiveScene().path);

            // Force open Menu scene before entering play mode
            if (!EditorSceneManager.GetActiveScene().path.Contains("Menu"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Restore the scene we were editing before play
            string lastScene = EditorPrefs.GetString("LastEditScene", "");
            if (!string.IsNullOrEmpty(lastScene) && !lastScene.Contains("Menu"))
                EditorSceneManager.OpenScene(lastScene);
        }
    }
}
