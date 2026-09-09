using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class RoomsSceneBuilder
{
    static TMP_FontAsset FontBold  => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
    static TMP_FontAsset FontLight => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-LIGHT SDF.asset");
    static Sprite PanelSprite      => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SlimUI/Modern Menu 1/Graphics/Frames/Panel 1920x1080px.png");
    static Sprite CornerSprite     => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SlimUI/Modern Menu 1/Graphics/Misc/Corner Detail 256px.png");

    static Color BtnColor    = new Color(1f, 0.686f, 0f, 1f);
    static Color BgColor     = new Color(0.08f, 0.08f, 0.10f, 1f);
    static Color PanelColor  = new Color(0.10f, 0.10f, 0.13f, 0.97f);
    static Color AccentColor = new Color(1f, 0.686f, 0f, 0.8f);
    static Color AccentDim   = new Color(1f, 0.686f, 0f, 0.25f);
    static Color TextWhite   = Color.white;
    static Color TextGray    = new Color(0.7f, 0.7f, 0.7f, 1f);

    [MenuItem("Tools/Build Rooms Scene")]
    public static void BuildRoomsScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        camGO.tag = "MainCamera";
        camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full screen background
        var bg = Stretch(new GameObject("Background"), canvasGO.transform);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = BgColor;
        if (PanelSprite != null) { bgImg.sprite = PanelSprite; bgImg.type = Image.Type.Sliced; bgImg.color = new Color(1,1,1,0.04f); }

        // ── Container: anchored to fill screen with padding ──
        var container = new GameObject("Container");
        container.transform.SetParent(canvasGO.transform, false);
        var cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.03f, 0.04f);
        cRT.anchorMax = new Vector2(0.97f, 0.96f);
        cRT.offsetMin = Vector2.zero;
        cRT.offsetMax = Vector2.zero;
        container.AddComponent<Image>().color = Color.clear;
        cRT.localScale = new Vector3(1.05f, 1.05f, 1f);

        // Top accent line
        var topLine = new GameObject("TopLine");
        topLine.transform.SetParent(container.transform, false);
        var tlRT = topLine.AddComponent<RectTransform>();
        tlRT.anchorMin = new Vector2(0.01f, 1f); tlRT.anchorMax = new Vector2(0.99f, 1f);
        tlRT.pivot = new Vector2(0.5f, 1f);
        tlRT.anchoredPosition = new Vector2(0, -8);
        tlRT.sizeDelta = new Vector2(0, 5);
        topLine.AddComponent<Image>().color = AccentColor;

        // ── Header row (top 12% of container) ──
        var header = new GameObject("Header");
        header.transform.SetParent(container.transform, false);
        var hRT = header.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.01f, 0.86f); hRT.anchorMax = new Vector2(0.99f, 0.98f);
        hRT.offsetMin = Vector2.zero; hRT.offsetMax = Vector2.zero;

        // Title left
        var titleGO = MakeTMPAnchored(header.transform, "Title", "AVAILABLE ROOMS",
            new Vector2(0,0), new Vector2(0.6f,1), FontBold, TextWhite, TextAlignmentOptions.MidlineLeft, 45);
        titleGO.GetComponent<TextMeshProUGUI>().characterSpacing = 5f;

        // Status right
        var statusGO = MakeTMPAnchored(header.transform, "StatusText", "Searching...",
            new Vector2(0.6f,0), new Vector2(1f,1), FontLight, TextGray, TextAlignmentOptions.MidlineRight, 40);

        // Header divider
        var hDiv = new GameObject("HeaderDivider");
        hDiv.transform.SetParent(container.transform, false);
        var hdRT = hDiv.AddComponent<RectTransform>();
        hdRT.anchorMin = new Vector2(0.01f, 0.85f); hdRT.anchorMax = new Vector2(0.99f, 0.855f);
        hdRT.offsetMin = Vector2.zero; hdRT.offsetMax = Vector2.zero;
        hDiv.AddComponent<Image>().color = AccentColor;

        // ── Column headers (next 7%) ──
        var colRow = new GameObject("ColumnHeaders");
        colRow.transform.SetParent(container.transform, false);
        var crRT = colRow.AddComponent<RectTransform>();
        crRT.anchorMin = new Vector2(0.01f, 0.78f); crRT.anchorMax = new Vector2(0.99f, 0.85f);
        crRT.offsetMin = Vector2.zero; crRT.offsetMax = Vector2.zero;

        MakeTMPAnchored(colRow.transform, "ColCode",   "ROOM CODE", new Vector2(0.01f,0), new Vector2(0.25f,1), FontBold, AccentColor, TextAlignmentOptions.MidlineLeft, 22);
        MakeTMPAnchored(colRow.transform, "ColHost",   "HOST ID",   new Vector2(0.25f,0), new Vector2(0.65f,1), FontBold, AccentColor, TextAlignmentOptions.MidlineLeft, 22);
        MakeTMPAnchored(colRow.transform, "ColStatus", "STATUS",    new Vector2(0.65f,0), new Vector2(0.82f,1), FontBold, AccentColor, TextAlignmentOptions.MidlineLeft, 22);

        // Col divider
        var cDiv = new GameObject("ColDivider");
        cDiv.transform.SetParent(container.transform, false);
        var cdRT = cDiv.AddComponent<RectTransform>();
        cdRT.anchorMin = new Vector2(0.01f, 0.775f); cdRT.anchorMax = new Vector2(0.99f, 0.78f);
        cdRT.offsetMin = Vector2.zero; cdRT.offsetMax = Vector2.zero;
        cDiv.AddComponent<Image>().color = AccentDim;

        // ── Room list (middle 60%) — scrollable ──
        var scrollGO = new GameObject("RoomListScroll");
        scrollGO.transform.SetParent(container.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.01f, 0.18f); scrollRT.anchorMax = new Vector2(0.99f, 0.775f);
        scrollRT.offsetMin = Vector2.zero; scrollRT.offsetMax = Vector2.zero;
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = Color.clear;
        scrollImg.raycastTarget = false;

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var vpRT = viewportGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        viewportGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = vpRT;

        var listPanel = new GameObject("RoomListPanel");
        listPanel.transform.SetParent(viewportGO.transform, false);
        var listRT = listPanel.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0, 1); listRT.anchorMax = new Vector2(1, 1);
        listRT.pivot = new Vector2(0.5f, 1);
        listRT.anchoredPosition = Vector2.zero;
        listRT.sizeDelta = new Vector2(0, 0);
        var vlg = listPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        var csf = listPanel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = listRT;

        // Footer divider
        var fDiv = new GameObject("FooterDivider");
        fDiv.transform.SetParent(container.transform, false);
        var fdRT = fDiv.AddComponent<RectTransform>();
        fdRT.anchorMin = new Vector2(0.01f, 0.175f); fdRT.anchorMax = new Vector2(0.99f, 0.18f);
        fdRT.offsetMin = Vector2.zero; fdRT.offsetMax = Vector2.zero;
        fDiv.AddComponent<Image>().color = AccentDim;

        // ── Footer buttons (bottom 15%) ──
        var footerRow = new GameObject("FooterButtons");
        footerRow.transform.SetParent(container.transform, false);
        var frRT = footerRow.AddComponent<RectTransform>();
        frRT.anchorMin = new Vector2(0.01f, 0.03f); frRT.anchorMax = new Vector2(0.99f, 0.17f);
        frRT.offsetMin = Vector2.zero; frRT.offsetMax = Vector2.zero;
        var hlg = footerRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.padding = new RectOffset(20, 20, 5, 5);

        var refreshBtn = MakeSlimButtonInLayout(footerRow.transform, "RefreshButton", "SEARCH ROOMS");
        var backBtn    = MakeSlimButtonInLayout(footerRow.transform, "BackButton",    "GO BACK TO MENU");

        // Version
        var ver = new GameObject("Version");
        ver.transform.SetParent(container.transform, false);
        var vRT = ver.AddComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0.85f, 0.01f); vRT.anchorMax = new Vector2(0.99f, 0.06f);
        vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;
        var vTMP = ver.AddComponent<TextMeshProUGUI>();
        vTMP.text = "v1.1"; vTMP.fontSize = 16; vTMP.color = new Color(0.4f,0.4f,0.4f);
        vTMP.alignment = TextAlignmentOptions.MidlineRight;
        if (FontLight != null) vTMP.font = FontLight;

        // ── Create RoomEntry prefab ──
        CreateRoomEntryPrefab();

        // ── RoomsUI ──
        var roomsUI = canvasGO.AddComponent<RoomsUI>();
        roomsUI.roomListContainer = listRT;
        roomsUI.statusText = statusGO.GetComponent<TextMeshProUGUI>();
        roomsUI.refreshButton = refreshBtn.GetComponent<Button>() ?? refreshBtn.GetComponentInChildren<Button>();
        roomsUI.backButton    = backBtn.GetComponent<Button>()    ?? backBtn.GetComponentInChildren<Button>();

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Rooms.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Lobby.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Rooms.unity");
        Debug.Log("Rooms scene rebuilt.");
    }

    static void CreateRoomEntryPrefab()
    {
        var entry = new GameObject("RoomEntry");
        var rt = entry.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 62);
        var le = entry.AddComponent<LayoutElement>();
        le.minHeight = 62; le.preferredHeight = 62;

        // Background
        var bg = entry.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.15f, 1f);

        // Left accent bar
        var accent = new GameObject("Accent");
        accent.transform.SetParent(entry.transform, false);
        var aRT = accent.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0,0); aRT.anchorMax = new Vector2(0,1);
        aRT.offsetMin = Vector2.zero; aRT.offsetMax = new Vector2(4,0);
        accent.AddComponent<Image>().color = new Color(1f, 0.686f, 0f, 0.9f);

        // Room code
        var code = MakeTMPAnchored(entry.transform, "RoomCode", "------",
            new Vector2(0.01f,0), new Vector2(0.25f,1), 
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset"),
            new Color(1f, 0.686f, 0f, 1f), TextAlignmentOptions.MidlineLeft, 26);
        code.GetComponent<TextMeshProUGUI>().characterSpacing = 3f;

        // Host ID
        MakeTMPAnchored(entry.transform, "HostId", "Unknown",
            new Vector2(0.25f,0), new Vector2(0.65f,1),
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-LIGHT SDF.asset"),
            new Color(0.75f,0.75f,0.75f,1f), TextAlignmentOptions.MidlineLeft, 20);

        // Status
        MakeTMPAnchored(entry.transform, "Status", "OPEN",
            new Vector2(0.65f,0), new Vector2(0.82f,1),
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset"),
            new Color(0.2f,0.9f,0.4f,1f), TextAlignmentOptions.MidlineLeft, 18);

        // Join button
        var btnGO = new GameObject("JoinButton");
        btnGO.transform.SetParent(entry.transform, false);
        var bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.83f, 0.1f); bRT.anchorMax = new Vector2(0.99f, 0.9f);
        bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
        var bImg = btnGO.AddComponent<Image>();
        bImg.color = new Color(1f, 0.686f, 0f, 1f);
        if (AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SlimUI/Modern Menu 1/Graphics/Buttons/Button Fram 256px.png") is Sprite s)
        { bImg.sprite = s; bImg.type = Image.Type.Sliced; }
        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.717f,0.717f,0.717f,1f);
        colors.pressedColor = new Color(0.49f,0.49f,0.49f,1f);
        btn.colors = colors;
        var bTextGO = new GameObject("Text");
        bTextGO.transform.SetParent(btnGO.transform, false);
        var btRT = bTextGO.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
        var bTMP = bTextGO.AddComponent<TextMeshProUGUI>();
        bTMP.text = "JOIN"; bTMP.fontSize = 20; bTMP.fontStyle = FontStyles.Bold;
        bTMP.characterSpacing = 3f;
        bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.color = new Color(0.08f,0.08f,0.10f);
        if (FontBold != null) bTMP.font = FontBold;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        PrefabUtility.SaveAsPrefabAsset(entry, "Assets/Resources/Prefabs/RoomEntry.prefab");
        Object.DestroyImmediate(entry);
        Debug.Log("RoomEntry prefab saved to Assets/Resources/Prefabs/RoomEntry.prefab");
    }

    // ── Helpers ──

    static GameObject Stretch(GameObject go, Transform parent)
    {
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    static void AddCornerAnchored(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, float rotation)
    {
        if (CornerSprite == null) return;
        var go = new GameObject("Corner");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0, 0, rotation);
        var img = go.AddComponent<Image>();
        img.sprite = CornerSprite;
        img.color = AccentColor;
    }

    static GameObject MakeTMPAnchored(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        TMP_FontAsset font, Color color, TextAlignmentOptions align, float fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = align;
        if (font != null) tmp.font = font;
        return go;
    }

    static GameObject MakeSlimButtonInLayout(Transform parent, string name, string label)
    {
        var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SlimUI/Modern Menu 1/Prefabs/Buttons/Btn_MainMenu.prefab");
        GameObject go;
        if (btnPrefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            var tmp3d = go.GetComponentInChildren<TMPro.TextMeshPro>(true);
            if (tmp3d != null) { tmp3d.text = label; tmp3d.alignment = TextAlignmentOptions.Center; }
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = BtnColor;
            go.AddComponent<Button>();
            var t = new GameObject("Text"); t.transform.SetParent(go.transform, false);
            var tRT = t.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
            var tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            if (FontBold != null) tmp.font = FontBold;
        }
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        // Match manually set button size
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(626, 80);
        return go;
    }
}
