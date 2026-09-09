using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class LobbySceneBuilder
{
    // ── Assets ──
    static TMP_FontAsset FontBold  => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
    static TMP_FontAsset FontLight => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-LIGHT SDF.asset");
    static Sprite BtnSprite        => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SlimUI/Modern Menu 1/Graphics/Buttons/Button Fram 256px.png");
    static Sprite PanelSprite      => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SlimUI/Modern Menu 1/Graphics/Frames/Panel 1920x1080px.png");

    // ── Colors ──
    static readonly Color BgColor    = new Color(0.08f, 0.08f, 0.10f, 1f);
    static readonly Color PanelColor = new Color(0.11f, 0.11f, 0.14f, 1f);
    static readonly Color CardColor  = new Color(0.13f, 0.13f, 0.16f, 1f);
    static readonly Color Accent     = new Color(1f, 0.686f, 0f, 1f);
    static readonly Color AccentDim  = new Color(1f, 0.686f, 0f, 0.25f);
    static readonly Color AccentMid  = new Color(1f, 0.686f, 0f, 0.8f);
    static readonly Color TextWhite  = Color.white;
    static readonly Color TextGray   = new Color(0.65f, 0.65f, 0.65f, 1f);
    static readonly Color GreenBtn   = new Color(0.08f, 0.60f, 0.28f, 1f);
    static readonly Color RedBtn     = new Color(0.65f, 0.12f, 0.12f, 1f);
    static readonly Color HostBadge  = new Color(1f, 0.686f, 0f, 1f);
    static readonly Color EmptyCard  = new Color(0.10f, 0.10f, 0.12f, 0.5f);

    [MenuItem("Tools/Build Lobby Scene")]
    public static void BuildLobbyScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        cam.tag = "MainCamera";
        camGO.AddComponent<UniversalAdditionalCameraData>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

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

        // Full background
        var bgGO = Stretch(new GameObject("Background"), canvasGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        if (PanelSprite != null) { bgImg.sprite = PanelSprite; bgImg.type = Image.Type.Sliced; bgImg.color = new Color(1,1,1,0.04f); }
        else bgImg.color = BgColor;

        // ── TOP ACCENT LINE ──
        MakeLine(canvasGO.transform, "TopLine", new Vector2(0.01f, 0.975f), new Vector2(0.99f, 0.98f), AccentMid);

        // ── HEADER (top 12%) ──
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(canvasGO.transform, false);
        headerGO.AddComponent<RectTransform>();
        Anchors(headerGO, new Vector2(0.02f, 0.87f), new Vector2(0.98f, 0.97f));

        // Left: title
        var titleGO = MakeTMP(headerGO.transform, "Title", "LOBBY", FontBold, TextWhite, 48, TextAlignmentOptions.MidlineLeft);
        Anchors(titleGO, new Vector2(0f, 0f), new Vector2(0.35f, 1f));
        titleGO.GetComponent<TextMeshProUGUI>().characterSpacing = 8f;

        // Center: player count
        var countGO = MakeTMP(headerGO.transform, "PlayerCount", "0 / 10 PLAYERS", FontLight, TextGray, 30, TextAlignmentOptions.MidlineRight);
        Anchors(countGO, new Vector2(0.35f, 0f), new Vector2(0.65f, 1f));

        // Right: room code pill
        var codePillGO = new GameObject("CodePill");
        codePillGO.transform.SetParent(headerGO.transform, false);
        codePillGO.AddComponent<RectTransform>();
        Anchors(codePillGO, new Vector2(0.65f, 0.1f), new Vector2(1f, 0.9f));
        var pillImg = codePillGO.AddComponent<Image>();
        pillImg.color = PanelColor;

        var pillAccent = new GameObject("PillAccent");
        pillAccent.transform.SetParent(codePillGO.transform, false);
        var paRT = pillAccent.AddComponent<RectTransform>();
        paRT.anchorMin = new Vector2(0,0); paRT.anchorMax = new Vector2(0,1);
        paRT.offsetMin = Vector2.zero; paRT.offsetMax = new Vector2(4,0);
        pillAccent.AddComponent<Image>().color = Accent;

        var codeLabelGO = MakeTMP(codePillGO.transform, "CodeLabel", "ROOM CODE : ", FontBold, AccentMid, 40, TextAlignmentOptions.MidlineLeft);
        Anchors(codeLabelGO, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.95f));
        codeLabelGO.GetComponent<TextMeshProUGUI>().characterSpacing = 3f;

        var codeValueGO = MakeTMP(codePillGO.transform, "RoomCodeText", "______", FontBold, Accent, 40, TextAlignmentOptions.MidlineLeft);
        Anchors(codeValueGO, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.55f));
        codeValueGO.GetComponent<TextMeshProUGUI>().characterSpacing = 6f;

        // Header divider
        MakeLine(canvasGO.transform, "HeaderDivider", new Vector2(0.01f, 0.865f), new Vector2(0.99f, 0.870f), AccentMid);

        // ── MAIN CONTENT AREA (header to footer) ──
        // Player grid label
        var gridLabelGO = MakeTMP(canvasGO.transform, "GridLabel", "PLAYERS", FontBold, AccentMid, 25, TextAlignmentOptions.MidlineLeft);
        Anchors(gridLabelGO, new Vector2(0.02f, 0.82f), new Vector2(0.3f, 0.86f));
        gridLabelGO.GetComponent<TextMeshProUGUI>().characterSpacing = 4f;

        // Player grid scroll area
        var scrollGO = new GameObject("PlayerListScroll");
        scrollGO.transform.SetParent(canvasGO.transform, false);
        scrollGO.AddComponent<RectTransform>();
        Anchors(scrollGO, new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.82f));
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false; scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollGO.AddComponent<Image>().color = Color.clear;

        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<Image>().color = new Color(0,0,0,0.01f);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = vpRT;

        var listGO = new GameObject("PlayerListContainer");
        listGO.transform.SetParent(vpGO.transform, false);
        var listRT = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0,1); listRT.anchorMax = new Vector2(1,1);
        listRT.pivot = new Vector2(0.5f,1f);
        listRT.anchoredPosition = Vector2.zero; listRT.sizeDelta = Vector2.zero;

        // Grid layout — 2 columns
        var grid = listGO.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(880, 90);
        grid.spacing = new Vector2(16, 12);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperLeft;

        var csf = listGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = listRT;

        // ── FOOTER DIVIDER ──
        MakeLine(canvasGO.transform, "FooterDivider", new Vector2(0.01f, 0.215f), new Vector2(0.99f, 0.220f), AccentDim);

        // ── FOOTER BUTTONS ──
        var leaveBtn = MakeButton(canvasGO.transform, "LeaveButton", "LEAVE ROOM", RedBtn);
        Anchors(leaveBtn, new Vector2(0.02f, 0.10f), new Vector2(0.30f, 0.20f));

        var statusGO = MakeTMP(canvasGO.transform, "StatusText", "Waiting for host to start", FontLight, TextGray, 40, TextAlignmentOptions.MidlineRight);
        statusGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        Anchors(statusGO, new Vector2(0.31f, 0.10f), new Vector2(0.69f, 0.20f));

        var startBtn = MakeButton(canvasGO.transform, "StartButton", "START MATCH", GreenBtn);
        Anchors(startBtn, new Vector2(0.70f, 0.10f), new Vector2(0.98f, 0.20f));

        // Version
        var verGO = MakeTMP(canvasGO.transform, "Version", "v1.1", FontLight, Color.white, 25, TextAlignmentOptions.BottomRight);
        verGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        Anchors(verGO, new Vector2(0.85f, 0.01f), new Vector2(0.99f, 0.06f));

        // ── PLAYER ENTRY PREFAB ──
        var entryPrefab = CreatePlayerEntryPrefab();

        // ── WIRE LobbyUI ──
        var lobbyUI = canvasGO.AddComponent<LobbyUI>();
        lobbyUI.roomCodeText          = codeValueGO.GetComponent<TextMeshProUGUI>();
        lobbyUI.playerCountText       = countGO.GetComponent<TextMeshProUGUI>();
        lobbyUI.statusText            = statusGO.GetComponent<TextMeshProUGUI>();
        lobbyUI.playerListContainer   = listRT;
        lobbyUI.startButton           = startBtn.GetComponent<Button>() ?? startBtn.GetComponentInChildren<Button>(true);
        lobbyUI.leaveButton           = leaveBtn.GetComponent<Button>() ?? leaveBtn.GetComponentInChildren<Button>(true);

        // Save prefab
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(entryPrefab, "Assets/Resources/Prefabs/PlayerNameEntry.prefab");
        Object.DestroyImmediate(entryPrefab);
        lobbyUI.playerNameEntryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayerNameEntry.prefab");

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Rooms.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Lobby.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Lobby.unity");
        Debug.Log("[LobbySceneBuilder] Done.");
    }

    // ── Player Entry Prefab ──
    // Card layout: index number | name | HOST badge
    static GameObject CreatePlayerEntryPrefab()
    {
        var go = new GameObject("PlayerNameEntry");
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(880, 90);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 90; le.preferredHeight = 90;

        // Card background
        var img = go.AddComponent<Image>();
        img.color = CardColor;

        // Left gold accent bar
        var bar = new GameObject("AccentBar");
        bar.transform.SetParent(go.transform, false);
        var barRT = bar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0,0); barRT.anchorMax = new Vector2(0,1);
        barRT.offsetMin = Vector2.zero; barRT.offsetMax = new Vector2(5,0);
        bar.AddComponent<Image>().color = Accent;

        // Player index number (e.g. "01")
        var indexGO = new GameObject("PlayerIndex");
        indexGO.transform.SetParent(go.transform, false);
        var iRT = indexGO.AddComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0,0); iRT.anchorMax = new Vector2(0.12f,1);
        iRT.offsetMin = new Vector2(12,0); iRT.offsetMax = Vector2.zero;
        var iTMP = indexGO.AddComponent<TextMeshProUGUI>();
        iTMP.text = "01"; iTMP.fontSize = 32; iTMP.color = AccentDim;
        iTMP.alignment = TextAlignmentOptions.Center; iTMP.fontStyle = FontStyles.Bold;
        if (FontBold != null) iTMP.font = FontBold;

        // Vertical divider
        var vDiv = new GameObject("VDivider");
        vDiv.transform.SetParent(go.transform, false);
        var vRT = vDiv.AddComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0.12f, 0.15f); vRT.anchorMax = new Vector2(0.125f, 0.85f);
        vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;
        vDiv.AddComponent<Image>().color = AccentDim;

        // Player name
        var nameGO = new GameObject("PlayerName");
        nameGO.transform.SetParent(go.transform, false);
        var nRT = nameGO.AddComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0.14f, 0); nRT.anchorMax = new Vector2(0.75f, 1);
        nRT.offsetMin = new Vector2(8,0); nRT.offsetMax = Vector2.zero;
        var nTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nTMP.text = "Player"; nTMP.fontSize = 26; nTMP.color = TextWhite;
        nTMP.alignment = TextAlignmentOptions.MidlineLeft;
        if (FontLight != null) nTMP.font = FontLight;

        // HOST badge (right side)
        var badgeGO = new GameObject("HostBadge");
        badgeGO.transform.SetParent(go.transform, false);
        var bRT = badgeGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.76f, 0.2f); bRT.anchorMax = new Vector2(0.98f, 0.8f);
        bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
        var bImg = badgeGO.AddComponent<Image>();
        bImg.color = new Color(Accent.r, Accent.g, Accent.b, 0.15f);
        var bTMP = new GameObject("BadgeText");
        bTMP.transform.SetParent(badgeGO.transform, false);
        var btRT = bTMP.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
        var btTMP = bTMP.AddComponent<TextMeshProUGUI>();
        btTMP.text = "HOST"; btTMP.fontSize = 18; btTMP.color = Accent;
        btTMP.alignment = TextAlignmentOptions.Center; btTMP.fontStyle = FontStyles.Bold;
        btTMP.characterSpacing = 3f;
        if (FontBold != null) btTMP.font = FontBold;

        return go;
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

    static void MakeLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
    }

    static GameObject MakeTMP(Transform parent, string name, string text, TMP_FontAsset font, Color color, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.alignment = align;
        if (font != null) tmp.font = font;
        return go;
    }

    static void Anchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static GameObject MakeButton(Transform parent, string name, string label, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        if (BtnSprite != null) { img.sprite = BtnSprite; img.type = Image.Type.Sliced; }
        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cols.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        btn.colors = cols;
        btn.targetGraphic = img;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 24; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        if (FontBold != null) tmp.font = FontBold;

        return go;
    }
}
