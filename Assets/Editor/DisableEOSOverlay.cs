using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

[InitializeOnLoad]
public class DisableEOSOverlay : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => -1000;

    static DisableEOSOverlay()
    {
        ExcludeFromBuild();
    }

    [InitializeOnLoadMethod]
    static void ExcludeFromBuild()
    {
        string[] guids = AssetDatabase.FindAssets("GfxPluginNativeRender-x64");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".dll")) continue;

            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.GetCompatibleWithAnyPlatform())
            {
                importer.SetCompatibleWithAnyPlatform(false);
                changed = true;
            }
            if (importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64))
            {
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
                changed = true;
            }
            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"Excluded from builds: {path}");
            }
        }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        ExcludeFromBuild();

        // Also delete if somehow still present
        string buildDir = Path.GetDirectoryName(report.summary.outputPath);
        string dll = Path.Combine(buildDir, "INDG_Data", "Plugins", "x86_64", "GfxPluginNativeRender-x64.dll");
        if (File.Exists(dll))
        {
            File.SetAttributes(dll, FileAttributes.Normal);
            File.Delete(dll);
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        string buildDir = Path.GetDirectoryName(report.summary.outputPath);
        string dll = Path.Combine(buildDir, "INDG_Data", "Plugins", "x86_64", "GfxPluginNativeRender-x64.dll");
        if (File.Exists(dll))
        {
            File.SetAttributes(dll, FileAttributes.Normal);
            File.Delete(dll);
        }
    }
}
