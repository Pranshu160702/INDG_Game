using UnityEngine;
using UnityEditor;

public class CowsinsMaterialConverter
{
    [MenuItem("Tools/Log Cowsins Shader Names")]
    static void LogShaders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Cowsins" });
        System.Collections.Generic.HashSet<string> names = new();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) names.Add(mat.shader.name);
        }
        System.IO.File.WriteAllLines("console_log.txt", names);
        Debug.Log("Shader names written to console_log.txt");
    }

    [MenuItem("Tools/Convert Cowsins Materials to URP")]
    static void Convert()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Cowsins" });
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shaderName = mat.shader.name;

            if (shaderName == "Standard")
            {
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Texture metallicGloss = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
                float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1f;
                int srcBlend = mat.HasProperty("_SrcBlend") ? (int)mat.GetFloat("_SrcBlend") : 1;
                int mode = mat.HasProperty("_Mode") ? (int)mat.GetFloat("_Mode") : 0;

                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                mat.SetColor("_BaseColor", color);
                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
                if (bumpMap != null) { mat.SetTexture("_BumpMap", bumpMap); mat.SetFloat("_BumpScale", bumpScale); }
                if (metallicGloss != null) mat.SetTexture("_MetallicGlossMap", metallicGloss);

                // Handle transparency
                if (mode == 3 || srcBlend == 5) // Transparent
                {
                    mat.SetFloat("_Surface", 1);
                    mat.SetOverrideTag("RenderType", "Transparent");
                }

                EditorUtility.SetDirty(mat);
                converted++;
            }
            else if (shaderName.StartsWith("Particles/") || shaderName.StartsWith("Legacy Shaders/Particles/"))
            {
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;

                mat.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                mat.SetColor("_BaseColor", color);
                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);

                EditorUtility.SetDirty(mat);
                converted++;
            }
            else if (shaderName.StartsWith("Legacy Shaders/") || shaderName == "Diffuse" || shaderName == "Specular")
            {
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;

                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                mat.SetColor("_BaseColor", color);
                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);

                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Cowsins Material Converter: converted {converted} materials out of {guids.Length} found.");
    }
}
