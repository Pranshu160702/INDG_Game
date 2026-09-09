using UnityEditor;
using UnityEngine;

public class BuildSetup
{
    [MenuItem("Tools/Setup Build Scenes")]
    public static void SetupBuildScenes()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };

        EditorBuildSettings.scenes = scenes;
        Debug.Log("Build scenes set: Menu (0), Game (1)");
    }
}
