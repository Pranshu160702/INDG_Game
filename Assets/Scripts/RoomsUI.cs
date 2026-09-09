using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EpicTransport;
using System.Collections.Generic;

public class RoomsUI : MonoBehaviour
{
    public Transform roomListContainer;
    public TextMeshProUGUI statusText;
    public Button refreshButton;
    public Button backButton;

    private GameNetworkManager netManager;

    void Start()
    {
        netManager = FindAnyObjectByType<GameNetworkManager>();
        if (roomListContainer == null) Debug.LogError("[RoomsUI] roomListContainer not assigned!");
        if (statusText == null) Debug.LogError("[RoomsUI] statusText not assigned!");

        refreshButton?.onClick.AddListener(Refresh);
        backButton?.onClick.AddListener(OnBack);

        // Hide loading overlay shown when navigating from Menu
        LoadingOverlay.Hide();

        Refresh();
    }

    public void Refresh()
    {
        if (statusText != null) statusText.text = "Searching...";
        if (refreshButton != null) refreshButton.interactable = false;

        // Safely destroy all existing entries
        if (roomListContainer != null)
        {
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in roomListContainer) children.Add(child.gameObject);
            foreach (var child in children) Destroy(child);
        }

        if (!IsEOSReady()) { StartCoroutine(WaitThenRefresh()); return; }
        if (EOSLobbyCode.Instance == null) { SetStatus("EOS not ready."); return; }
        // Small delay to allow any pending EOS session destroys to propagate
        StartCoroutine(DelayedRefresh());
    }

    System.Collections.IEnumerator DelayedRefresh()
    {
        if (statusText != null) statusText.text = "Loading rooms...";
        // Wait for EOS session changes to propagate across servers
        yield return new WaitForSeconds(3f);
        StartCoroutine(RefreshWithTimeout());
    }

    System.Collections.IEnumerator RefreshWithTimeout()
    {
        bool done = false;
        EOSLobbyCode.Instance.FindAllSessions(rooms => { done = true; OnRoomsFound(rooms); });
        float t = 10f;
        while (!done && t > 0f) { t -= UnityEngine.Time.deltaTime; yield return null; }
        if (!done) SetStatus("Search timed out. Try refreshing.");
    }

    System.Collections.IEnumerator WaitThenRefresh()
    {
        // Wait for EOS session changes to propagate
        yield return new WaitForSeconds(3f);
        float t = 10f;
        while (!IsEOSReady() && t > 0f) { t -= UnityEngine.Time.deltaTime; yield return null; }
        if (!IsEOSReady()) { SetStatus("EOS not ready."); yield break; }
        if (EOSLobbyCode.Instance == null) { SetStatus("EOS not ready."); yield break; }
        StartCoroutine(RefreshWithTimeout());
    }

    static bool IsEOSReady()
    {
        try { return EOSSDKComponent.Initialized; }
        catch { return false; }
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        if (refreshButton != null) refreshButton.interactable = true;
    }

    void OnRoomsFound(List<EOSLobbyCode.RoomInfo> rooms)
    {
        if (refreshButton != null) refreshButton.interactable = true;
        if (roomListContainer == null) return;

        if (rooms.Count == 0) { SetStatus("No rooms found."); return; }

        if (statusText != null) statusText.text = $"{rooms.Count} room(s) found.";

        var entryPrefab = Resources.Load<GameObject>("Prefabs/RoomEntry");

        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            GameObject entry;

            if (entryPrefab != null)
            {
                entry = UnityEngine.Object.Instantiate(entryPrefab, roomListContainer);
                var codeTMP = entry.transform.Find("RoomCode")?.GetComponent<TextMeshProUGUI>();
                if (codeTMP != null) codeTMP.text = string.IsNullOrEmpty(room.LobbyCode) ? "------" : room.LobbyCode;
                var hostTMP = entry.transform.Find("HostId")?.GetComponent<TextMeshProUGUI>();
                if (hostTMP != null) hostTMP.text = string.IsNullOrEmpty(room.HostId) ? "Unknown" : room.HostId.Substring(0, Mathf.Min(14, room.HostId.Length)) + "...";
                var bg = entry.GetComponent<Image>();
                if (bg != null) bg.color = i % 2 == 0 ? new Color(0.12f, 0.12f, 0.15f) : new Color(0.09f, 0.09f, 0.12f);
                var btn = entry.transform.Find("JoinButton")?.GetComponent<Button>();
                if (btn != null) { var r = room; btn.onClick.AddListener(() => JoinRoom(r)); }
            }
            else
            {
                entry = BuildEntryInline(room, i);
            }
        }
    }

    private bool _isJoining = false;

    void JoinRoom(EOSLobbyCode.RoomInfo room)
    {
        if (_isJoining) return; // prevent double-click
        if (string.IsNullOrEmpty(room.HostId)) { SetStatus("Error: Invalid room."); return; }

        if (netManager == null) netManager = FindAnyObjectByType<GameNetworkManager>();
        if (netManager == null) { Debug.LogError("[RoomsUI] No GameNetworkManager!"); return; }

        if (Mirror.NetworkClient.active) netManager.StopClient();
        if (string.IsNullOrEmpty(GameNetworkManager.LocalPlayerName)) GameNetworkManager.LocalPlayerName = "Player";

        _isJoining = true;
        if (statusText != null) statusText.text = "Joining...";
        if (refreshButton != null) refreshButton.interactable = false;
        LoadingOverlay.Show();

        netManager.onlineScene = "Lobby";
        netManager.offlineScene = "Menu";
        netManager.networkAddress = room.HostId;
        netManager.StartClient();
    }

    void OnBack()
    {
        LoadingOverlay.Hide();
        if (Mirror.NetworkClient.active) netManager?.StopClient();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    // ── Inline fallback entry builder ──
    GameObject BuildEntryInline(EOSLobbyCode.RoomInfo room, int index)
    {
        var entry = new GameObject("RoomEntry");
        entry.transform.SetParent(roomListContainer, false);
        var rt = entry.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 62);
        var le = entry.AddComponent<LayoutElement>();
        le.minHeight = 62; le.preferredHeight = 62;
        entry.AddComponent<Image>().color = index % 2 == 0 ? new Color(0.12f, 0.12f, 0.15f) : new Color(0.09f, 0.09f, 0.12f);

        var accent = new GameObject("Accent"); accent.transform.SetParent(entry.transform, false);
        var aRT = accent.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0, 0); aRT.anchorMax = new Vector2(0, 1);
        aRT.offsetMin = Vector2.zero; aRT.offsetMax = new Vector2(4, 0);
        accent.AddComponent<Image>().color = new Color(1f, 0.686f, 0f, 0.9f);

        AddTMP(entry.transform, "RoomCode",  string.IsNullOrEmpty(room.LobbyCode) ? "------" : room.LobbyCode,
            new Vector2(0.01f, 0), new Vector2(0.25f, 1), 26, new Color(1f, 0.686f, 0f), FontStyles.Bold);
        AddTMP(entry.transform, "HostId", string.IsNullOrEmpty(room.HostId) ? "Unknown" : room.HostId.Substring(0, Mathf.Min(14, room.HostId.Length)) + "...",
            new Vector2(0.25f, 0), new Vector2(0.65f, 1), 20, new Color(0.75f, 0.75f, 0.75f), FontStyles.Normal);
        AddTMP(entry.transform, "Status", "OPEN",
            new Vector2(0.65f, 0), new Vector2(0.82f, 1), 18, new Color(0.2f, 0.9f, 0.4f), FontStyles.Bold);

        var btnGO = new GameObject("JoinButton"); btnGO.transform.SetParent(entry.transform, false);
        var bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.83f, 0.1f); bRT.anchorMax = new Vector2(0.99f, 0.9f);
        bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
        btnGO.AddComponent<Image>().color = new Color(1f, 0.686f, 0f);
        var btn = btnGO.AddComponent<Button>();
        var bText = new GameObject("Text"); bText.transform.SetParent(btnGO.transform, false);
        var btRT = bText.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
        var bTMP = bText.AddComponent<TextMeshProUGUI>();
        bTMP.text = "JOIN"; bTMP.fontSize = 20; bTMP.fontStyle = FontStyles.Bold;
        bTMP.alignment = TextAlignmentOptions.Center; bTMP.color = new Color(0.08f, 0.08f, 0.10f);
        var captured = room;
        btn.onClick.AddListener(() => JoinRoom(captured));
        return entry;
    }

    void AddTMP(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax,
        float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }
}
