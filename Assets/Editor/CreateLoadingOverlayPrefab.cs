using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CreateLoadingOverlayPrefab
{
    [MenuItem("Tools/Create LoadingOverlay Prefab")]
    public static void Create()
    {
        // Root
        var root = new GameObject("LoadingOverlay");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;
        root.AddComponent<LoadingOverlay>();
        var c = root.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 100;
        root.AddComponent<GraphicRaycaster>();

        // Overlay background
        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(root.transform, false);
        var opRT = overlay.AddComponent<RectTransform>();
        opRT.anchorMin = Vector2.zero;
        opRT.anchorMax = Vector2.one;
        opRT.offsetMin = Vector2.zero;
        opRT.offsetMax = Vector2.zero;
        var opImg = overlay.AddComponent<Image>();

        // Load blur sprite
        Sprite blurSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Images/Blur-Transparent.png");
        if (blurSprite == null)
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Images/Blur-Transparent.png"))
                if (obj is Sprite s) { blurSprite = s; break; }
        if (blurSprite != null) { opImg.sprite = blurSprite; opImg.type = Image.Type.Simple; opImg.color = Color.white; }
        else opImg.color = new Color(0f, 0f, 0f, 0.5f);

        // Spinner
        var spinner = new GameObject("Spinner");
        spinner.transform.SetParent(overlay.transform, false);
        var spinRT = spinner.AddComponent<RectTransform>();
        spinRT.anchoredPosition = Vector2.zero;
        spinRT.sizeDelta = new Vector2(512, 512);
        var spinImg = spinner.AddComponent<Image>();

        Sprite bufferSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Images/buffericon.png");
        if (bufferSprite == null)
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Images/buffericon.png"))
                if (obj is Sprite s) { bufferSprite = s; break; }
        if (bufferSprite == null)
        {
            var ti = AssetImporter.GetAtPath("Assets/Resources/Images/buffericon.png") as TextureImporter;
            if (ti != null) { ti.spriteImportMode = SpriteImportMode.Single; ti.textureType = TextureImporterType.Sprite; ti.SaveAndReimport(); }
            bufferSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Images/buffericon.png");
        }
        if (bufferSprite != null) { spinImg.sprite = bufferSprite; spinImg.color = Color.white; }
        else { spinImg.color = new Color(0f, 0.831f, 1f, 1f); spinImg.type = Image.Type.Filled; spinImg.fillMethod = Image.FillMethod.Radial360; spinImg.fillAmount = 0.75f; }

        // Loading text
        var textGO = new GameObject("LoadingText");
        textGO.transform.SetParent(overlay.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchoredPosition = new Vector2(0, -320);
        tRT.sizeDelta = new Vector2(600, 120);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "LOADING...";
        tmp.fontSize = 112;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
        if (font != null) tmp.font = font;

        overlay.SetActive(false);

        // Save as prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var prefabPath = "Assets/Prefabs/LoadingOverlay.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log($"LoadingOverlay prefab saved to {prefabPath}");
    }
}
