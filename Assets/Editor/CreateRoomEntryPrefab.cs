using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CreateRoomEntryPrefab
{
    [MenuItem("Tools/Create RoomEntry Prefab")]
    public static void Create()
    {
        var fontBold  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
        var fontLight = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-LIGHT SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SlimUI/Modern Menu 1/Graphics/Buttons/Button Fram 256px.png");

        // Root
        var entry = new GameObject("RoomEntry");
        var rt = entry.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 66);
        var le = entry.AddComponent<LayoutElement>();
        le.minHeight = 66; le.preferredHeight = 66;
        entry.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

        // Left orange accent bar
        var accent = new GameObject("Accent");
        accent.transform.SetParent(entry.transform, false);
        var aRT = accent.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0, 0); aRT.anchorMax = new Vector2(0, 1);
        aRT.offsetMin = Vector2.zero; aRT.offsetMax = new Vector2(5, 0);
        accent.AddComponent<Image>().color = new Color(1f, 0.686f, 0f, 1f);

        // Room Code (col 1: 1%-25%)
        AddTMP(entry.transform, "RoomCode", "------",
            new Vector2(0.02f, 0.08f), new Vector2(0.25f, 0.92f),
            fontBold, new Color(1f, 0.686f, 0f, 1f), 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // Vertical separator 1
        AddSeparator(entry.transform, 0.25f);

        // Host ID (col 2: 26%-64%)
        AddTMP(entry.transform, "HostId", "Unknown",
            new Vector2(0.26f, 0.08f), new Vector2(0.64f, 0.92f),
            fontLight, new Color(0.75f, 0.75f, 0.75f, 1f), 18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

        // Vertical separator 2
        AddSeparator(entry.transform, 0.65f);

        // Status (col 3: 66%-82%)
        AddTMP(entry.transform, "Status", "OPEN",
            new Vector2(0.66f, 0.08f), new Vector2(0.82f, 0.92f),
            fontBold, new Color(0.2f, 0.9f, 0.4f, 1f), 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // Join Button (col 4: 83%-99%)
        var btnGO = new GameObject("JoinButton");
        btnGO.transform.SetParent(entry.transform, false);
        var bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.83f, 0.12f);
        bRT.anchorMax = new Vector2(0.99f, 0.88f);
        bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
        var bImg = btnGO.AddComponent<Image>();
        bImg.color = new Color(1f, 0.686f, 0f, 1f);
        if (btnSprite != null) { bImg.sprite = btnSprite; bImg.type = Image.Type.Sliced; }
        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.717f, 0.717f, 0.717f, 1f);
        colors.pressedColor = new Color(0.49f, 0.49f, 0.49f, 1f);
        btn.colors = colors;

        var bTextGO = new GameObject("Text");
        bTextGO.transform.SetParent(btnGO.transform, false);
        var btRT = bTextGO.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
        var bTMP = bTextGO.AddComponent<TextMeshProUGUI>();
        bTMP.text = "JOIN";
        bTMP.fontSize = 18;
        bTMP.fontStyle = FontStyles.Bold;
        bTMP.characterSpacing = 3f;
        bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.color = new Color(0.08f, 0.08f, 0.10f, 1f);
        if (fontBold != null) bTMP.font = fontBold;

        // Save to Resources/Prefabs
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        var path = "Assets/Resources/Prefabs/RoomEntry.prefab";
        PrefabUtility.SaveAsPrefabAsset(entry, path);
        Object.DestroyImmediate(entry);
        AssetDatabase.SaveAssets();
        Debug.Log($"RoomEntry prefab saved to {path}");
    }

    static void AddTMP(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        TMP_FontAsset font, Color color, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.color = color; tmp.fontStyle = style;
        tmp.alignment = align;
        if (font != null) tmp.font = font;
    }

    static void AddSeparator(Transform parent, float xAnchor)
    {
        var go = new GameObject("Sep");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xAnchor, 0.1f);
        rt.anchorMax = new Vector2(xAnchor, 0.9f);
        rt.sizeDelta = new Vector2(1, 0);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = new Color(1f, 0.686f, 0f, 0.2f);
    }
}
