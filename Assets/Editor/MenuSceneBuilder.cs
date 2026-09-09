using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MenuSceneBuilder
{
    [MenuItem("Tools/Build Menu Scene")]
    public static void BuildMenuScene()
    {
        string menuPath = "Assets/Scenes/Menu.unity";
        string sourcePath = System.IO.File.Exists(menuPath)
            ? menuPath
            : "Assets/SlimUI/Modern Menu 1/Scenes/Demos/Demo1.unity";

        var scene = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Single);

        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        if (sourcePath.Contains("Demo1"))
        {
            var es = FindInScene("EventSystem");
            if (es != null)
            {
                foreach (var m in es.GetComponents<UnityEngine.EventSystems.BaseInputModule>())
                    Object.DestroyImmediate(m);
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        // NetworkManager
        var existingNM = FindInScene("NetworkManager");
        if (existingNM != null) Object.DestroyImmediate(existingNM);
        var nmObj = new GameObject("NetworkManager");
        var eosSDK = nmObj.AddComponent<EpicTransport.EOSSDKComponent>();
        var eosTransport = nmObj.AddComponent<EpicTransport.EosTransport>();
        var netManager = nmObj.AddComponent<GameNetworkManager>();
        netManager.transport = eosTransport;
        Mirror.Transport.active = eosTransport;
        var apiKey = AssetDatabase.LoadAssetAtPath<EosApiKey>("Assets/Scripts/EOSApiKey.asset");
        if (apiKey != null)
        {
            var so = new SerializedObject(eosSDK);
            so.FindProperty("apiKeys").objectReferenceValue = apiKey;
            so.ApplyModifiedProperties();
        }
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NetworkedPlayer.prefab");
        if (playerPrefab != null) netManager.playerPrefab = playerPrefab;
        var lobbyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LobbyPlayer.prefab");
        if (lobbyPrefab != null)
        {
            netManager.lobbyPlayerPrefab = lobbyPrefab;
            var spawnList = new System.Collections.Generic.List<GameObject>(netManager.spawnPrefabs);
            if (!spawnList.Contains(lobbyPrefab)) spawnList.Add(lobbyPrefab);
            if (playerPrefab != null && !spawnList.Contains(playerPrefab)) spawnList.Add(playerPrefab);
            netManager.spawnPrefabs = spawnList;
        }

        // Find SlimUI objects
        var mainMenu  = FindInScene("Main_Menu");
        var canvMain  = FindInScene("Canv_Main");
        var mainPanel = FindInScene("MAIN");
        var playPanel = FindInScene("PLAY");
        var extrasPanel = FindInScene("EXTRAS");
        var exitPanel = FindInScene("EXIT");
        var btnPlay   = FindInScene("Btn_PlayCampaign");

        if (mainPanel == null || playPanel == null)
        {
            Debug.LogError("[MenuSceneBuilder] Could not find required SlimUI panels.");
            return;
        }

        // Remove existing MenuUI
        var existingUI = canvMain.GetComponent<MenuUI>();
        if (existingUI != null) Object.DestroyImmediate(existingUI);

        // ── PLAY panel → 3 option buttons ──
        var playVL = playPanel.GetComponentInChildren<VerticalLayoutGroup>();
        Transform playParent = playVL != null ? playVL.transform : playPanel.transform;
        foreach (Transform child in GetChildList(playParent))
            Object.DestroyImmediate(child.gameObject);

        CreateButton(playParent, "Btn_HostGame",   "HOST GAME");
        CreateButton(playParent, "Btn_JoinGame",   "JOIN GAME");
        CreateButton(playParent, "Btn_JoinRandom", "SHOW ROOMS");
        CreateButton(playParent, "Btn_BackPlay",   "BACK");

        // ── LOADING OVERLAY — instantiated at runtime, not needed in scene ──
        // Remove any existing LoadingOverlay from scene
        if (canvMain != null)
        {
            var toDelete = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in canvMain.transform)
                if (child.name == "LoadingOverlay") toDelete.Add(child.gameObject);
            foreach (var go in toDelete) Object.DestroyImmediate(go);
        }

        // EXTRAS panel — leave completely untouched to preserve manual changes

        // ── JOIN sub-panel — child of Btn_JoinGame, shown inline ──
        var btnJoinGO = FindInScene("Btn_JoinGame", playPanel);
        GameObject joinPanel = null;
        if (btnJoinGO != null)
        {
            // Remove existing join panel if rebuilding
            var existing = FindInScene("JoinSubPanel", btnJoinGO);
            if (existing != null) Object.DestroyImmediate(existing);

            joinPanel = new GameObject("JoinSubPanel");
            joinPanel.transform.SetParent(btnJoinGO.transform, false);
            var jRT = joinPanel.AddComponent<RectTransform>();
            jRT.anchorMin = new Vector2(1, 1);
            jRT.anchorMax = new Vector2(1, 1);
            jRT.pivot = new Vector2(0, 1f);
            jRT.anchoredPosition = new Vector2(40, 77.3f);
            jRT.sizeDelta = new Vector2(playParent.GetComponent<RectTransform>()?.rect.width > 0 ? playParent.GetComponent<RectTransform>().rect.width : 337.65f, 0);
            var jVLG = joinPanel.AddComponent<VerticalLayoutGroup>();
            jVLG.spacing = 16; jVLG.childAlignment = TextAnchor.UpperLeft;
            jVLG.childForceExpandWidth = true; jVLG.childForceExpandHeight = false;
            jVLG.childControlHeight = true; jVLG.childControlWidth = true;
            jVLG.padding = new RectOffset(0, 0, 0, 0);
            var csf = joinPanel.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Block parent button events
            joinPanel.AddComponent<BlockParentEvents>();
            var img = joinPanel.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;

            // Add Canvas to fully isolate from parent button
            var subCanvas = joinPanel.AddComponent<Canvas>();
            subCanvas.overrideSorting = true;
            subCanvas.sortingOrder = 10;
            joinPanel.AddComponent<GraphicRaycaster>();

            // Add popup animation — same as PLAY panel
            var animator = joinPanel.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/SlimUI/Modern Menu 1/Animations/Panels/WindowPopUpSmall.controller");
            if (controller != null) animator.runtimeAnimatorController = controller;

            CreateInputField(joinPanel.transform, "RoomCodeInput", "\u25cf \u25cf \u25cf \u25cf \u25cf \u25cf");
            var confirmBtn = CreateButton(joinPanel.transform, "Btn_ConfirmJoin", "ENTER GAME");
            var backBtn = CreateButton(joinPanel.transform, "Btn_BackJoin", "BACK");
            CreateTMP(joinPanel.transform, "JoinStatusText", "", 32, Color.gray);
            joinPanel.SetActive(false);
        }

        // Grab references
        var roomCodeInput    = FindInScene("RoomCodeInput",    joinPanel)?.GetComponent<TMP_InputField>();
        var joinStatusText   = FindInScene("JoinStatusText",   joinPanel)?.GetComponent<TextMeshProUGUI>();
        var confirmJoinBtn   = FindInScene("Btn_ConfirmJoin",  joinPanel);
        var confirmJoinButton = confirmJoinBtn?.GetComponent<Button>() ?? confirmJoinBtn?.GetComponentInChildren<Button>();

        // ── Add MenuUI ──
        var menuUI = canvMain.AddComponent<MenuUI>();
        menuUI.mainPanel        = mainPanel;
        menuUI.playPanel        = playPanel;
        menuUI.joinPanel        = joinPanel;
        menuUI.roomCodeInput    = roomCodeInput;
        menuUI.joinStatusText   = joinStatusText;
        menuUI.confirmJoinButton = confirmJoinButton;

        // Wire Btn_PlayCampaign → show play options
        if (btnPlay != null) AddButtonListener(btnPlay, menuUI, "OnPlayClicked");

        // Wire play panel buttons
        var uiManager = mainMenu?.GetComponent<SlimUI.ModernMenu.UIMenuManager>();
        AddButtonListener(FindInScene("Btn_HostGame",   playPanel), menuUI, "OnHostClicked");
        AddButtonListener(FindInScene("Btn_JoinGame",   playPanel), menuUI, "OnJoinClicked");
        AddButtonListener(FindInScene("Btn_JoinRandom", playPanel), menuUI, "OnJoinRandomClicked");
        var btnBackPlay = FindInScene("Btn_BackPlay", playPanel);
        AddButtonListener(btnBackPlay, menuUI, "OnBackFromPlay");
        if (uiManager != null) AddButtonListenerGeneric(btnBackPlay, uiManager, "ReturnMenu");

        // Wire join panel
        AddButtonListener(FindInScene("Btn_ConfirmJoin", joinPanel), menuUI, "OnConfirmJoinClicked");
        AddButtonListener(FindInScene("Btn_BackJoin", joinPanel), menuUI, "OnJoinClicked");

        // ── Testing button — only create if it doesn't already exist ──
        var existingTest = FindInScene("Btn_Testing", canvMain);
        if (existingTest == null)
        {
            var testBtn = new GameObject("Btn_Testing");
            testBtn.transform.SetParent(canvMain.transform, false);
            var testRT = testBtn.AddComponent<RectTransform>();
            testRT.anchorMin = new Vector2(1, 1);
            testRT.anchorMax = new Vector2(1, 1);
            testRT.pivot = new Vector2(1, 1);
            testRT.anchoredPosition = new Vector2(-20, -20);
            testRT.sizeDelta = new Vector2(200, 60);
            var testImg = testBtn.AddComponent<Image>();
            testImg.color = Color.white;
            var testBtnComp = testBtn.AddComponent<Button>();
            testBtnComp.targetGraphic = testImg;
            var testColors = testBtnComp.colors;
            testColors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            testColors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            testBtnComp.colors = testColors;
            var testTextGO = new GameObject("Text");
            testTextGO.transform.SetParent(testBtn.transform, false);
            var testTextRT = testTextGO.AddComponent<RectTransform>();
            testTextRT.anchorMin = Vector2.zero; testTextRT.anchorMax = Vector2.one;
            testTextRT.offsetMin = Vector2.zero; testTextRT.offsetMax = Vector2.zero;
            var testTMP = testTextGO.AddComponent<TextMeshProUGUI>();
            testTMP.text = "TESTING"; testTMP.fontSize = 24;
            testTMP.alignment = TextAlignmentOptions.Center;
            testTMP.color = Color.black;
            testTMP.fontStyle = FontStyles.Bold;
            testTMP.raycastTarget = false;
            AddButtonListener(testBtn, menuUI, "OnTestingClicked");
        }
        else
        {
            // Button already exists — just ensure the click listener is wired to the current MenuUI instance
            var existingBtn = existingTest.GetComponent<Button>() ?? existingTest.GetComponentInChildren<Button>();
            if (existingBtn != null)
            {
                var so = new SerializedObject(existingBtn);
                var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                // Clear stale listeners and re-wire to fresh MenuUI
                calls.ClearArray();
                so.ApplyModifiedProperties();
            }
            AddButtonListener(existingTest, menuUI, "OnTestingClicked");
        }

        // Rename Btn_Extras to Developer
        var btnExtras = FindInScene("Btn_Extras");
        if (btnExtras != null)
        {
            var tmp3d = btnExtras.GetComponentInChildren<TMPro.TextMeshPro>(true);
            if (tmp3d != null) tmp3d.text = "Developer";
            var tmpUI = btnExtras.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpUI != null) tmpUI.text = "Developer";
        }

        // Rename EXTRAS panel to Developer in hierarchy and keep it hidden
        var extrasGO = FindInScene("EXTRAS") ?? FindInScene("Developer");
        if (extrasGO != null)
        {
            extrasGO.name = "Developer";
            extrasGO.SetActive(false);
            // Wire any back button inside Developer panel to OnDeveloperBackClicked
            var backBtn = FindInScene("Btn_BackDeveloper", extrasGO) 
                       ?? FindInScene("Btn_Back", extrasGO)
                       ?? FindInScene("BackButton", extrasGO);
            if (backBtn != null) AddButtonListener(backBtn, menuUI, "OnDeveloperBackClicked");
            else Debug.LogWarning("[MenuSceneBuilder] No back button found in Developer panel. Name it Btn_BackDeveloper, Btn_Back, or BackButton.");
        }

        // Update version text
        var versionGO = FindInScene("Version");
        if (versionGO != null)
        {
            var tmp = versionGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "Version v1.1";
            var tmp3d = versionGO.GetComponent<TMPro.TextMeshPro>();
            if (tmp3d != null) tmp3d.text = "Version v1.1";
        }

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Rooms.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Lobby.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };

        EditorSceneManager.SaveScene(scene, menuPath);
        Debug.Log($"Menu scene built from {sourcePath}.");
    }

    static System.Collections.Generic.List<Transform> GetChildList(Transform t)
    {
        var list = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in t) list.Add(child);
        return list;
    }

    static GameObject FindInScene(string name, GameObject searchRoot = null)
    {
        if (searchRoot != null) return FindDeep(searchRoot, name);
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var r = FindDeep(root, name);
            if (r != null) return r;
        }
        return null;
    }

    static GameObject FindDeep(GameObject root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root.transform)
        {
            var r = FindDeep(child.gameObject, name);
            if (r != null) return r;
        }
        return null;
    }

    static TMP_InputField CreateInputField(Transform parent, string name, string placeholder)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(337.65f, 77.3f);
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // fully transparent
        var field = go.AddComponent<TMP_InputField>();
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        var taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 0); taRT.offsetMax = new Vector2(-10, 0);
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 46; tmp.color = Color.white; // 75% of original 78
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = placeholder; phTMP.fontSize = 32;
        phTMP.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        phTMP.alignment = TextAlignmentOptions.Center;
        if (font != null) phTMP.font = font;
        field.textComponent = tmp; field.placeholder = phTMP; field.textViewport = taRT;
        field.characterLimit = 6;
        field.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 77.3f; le.preferredWidth = 337.65f;
        return field;
    }

    static GameObject CreateTMP(Transform parent, string name, string text, int size, Color color)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(500, size + 16);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size + 16;
        return go;
    }

    static GameObject CreateUIButton(Transform parent, string name, string label)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SlimUI/Modern Menu 1/Graphics/Buttons/Button Fram 256px.png");
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(337.65f, 77.3f);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 0.686f, 0f, 1f); // SlimUI orange
        if (btnSprite != null) { img.sprite = btnSprite; img.type = Image.Type.Sliced; }
        var btn = go.AddComponent<Button>();
        // Match SlimUI button transition colors
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.717f, 0.717f, 0.717f, 1f);
        colors.pressedColor = new Color(0.49f, 0.49f, 0.49f, 1f);
        btn.colors = colors;
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(-16f, -11.3f); tRT.offsetMax = new Vector2(-16f, -11.3f);
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 52;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.margin = new Vector4(20, 0, 0, 0);
        tmp.color = Color.white;
        if (font != null) tmp.font = font;
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = 77.3f; le.preferredWidth = 337.65f;
        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string label)
    {
        var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SlimUI/Modern Menu 1/Prefabs/Buttons/Btn_MainMenu.prefab");
        GameObject go;
        if (btnPrefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            // SlimUI button uses TextMeshPro (3D) not TextMeshProUGUI
            var tmp3d = go.GetComponentInChildren<TMPro.TextMeshPro>(true);
            if (tmp3d != null)
                tmp3d.text = label;
            else
            {
                // Fallback to UI version
                var tmpUI = go.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmpUI != null) tmpUI.text = label;
            }
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(500, 55);
            go.AddComponent<Image>().color = new Color(0.2f, 0.4f, 0.8f);
            go.AddComponent<Button>();
            var t = new GameObject("Text"); t.transform.SetParent(go.transform, false);
            var tRT = t.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
            var tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        }
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = 77.3f; le.preferredWidth = 337.65f;
        return go;
    }

    static void AddButtonListener(GameObject btnGO, MenuUI target, string methodName)
    {
        if (btnGO == null) return;
        var btn = btnGO.GetComponent<Button>() ?? btnGO.GetComponentInChildren<Button>();
        if (btn == null) return;
        var so = new SerializedObject(btn);
        var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.arraySize++;
        var call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").intValue = 1;
        call.FindPropertyRelative("m_CallState").intValue = 2;
        so.ApplyModifiedProperties();
    }

    static void AddButtonListenerGeneric(GameObject btnGO, Object target, string methodName)
    {
        if (btnGO == null) return;
        var btn = btnGO.GetComponent<Button>() ?? btnGO.GetComponentInChildren<Button>();
        if (btn == null) return;
        var so = new SerializedObject(btn);
        var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.arraySize++;
        var call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").intValue = 1;
        call.FindPropertyRelative("m_CallState").intValue = 2;
        so.ApplyModifiedProperties();
    }
}
