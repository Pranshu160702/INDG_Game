using Mirror;
using UnityEngine;
using EpicTransport;

public class GameNetworkManager : NetworkManager
{
    [Header("Lobby")]
    public GameObject lobbyPlayerPrefab;
    public static string LocalPlayerName { get; set; }

    // Only the server tracks the lobby code
    public static string CurrentLobbyCode { get; private set; }

    // Flag to prevent double session destroy
    private static bool _sessionDestroyed = false;

    public override void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Debug.Log("[GNM] Duplicate NetworkManager detected, destroying self");
            Destroy(gameObject);
            return;
        }

        dontDestroyOnLoad = true;
        base.Awake();

        if (string.IsNullOrEmpty(LocalPlayerName))
        {
            if (!PlayerPrefs.HasKey("PlayerIndex"))
                PlayerPrefs.SetInt("PlayerIndex", 1);
            int idx = PlayerPrefs.GetInt("PlayerIndex");
            LocalPlayerName = $"Player{idx}";
            PlayerPrefs.SetInt("PlayerIndex", idx + 1);
            PlayerPrefs.Save();
        }

        if (EOSLobbyCode.Instance == null)
        {
            var go = new GameObject("EOSLobbyCode");
            go.AddComponent<EOSLobbyCode>();
        }
    }

    public void HostGame(string playerName, System.Action<string> onCodeReady)
    {
        if (EOSLobbyCode.Instance == null) { Debug.LogError("[GNM] EOSLobbyCode null"); onCodeReady?.Invoke(null); return; }
        if (NetworkServer.active || NetworkClient.active) { Debug.LogError("[GNM] Already hosting/connected"); onCodeReady?.Invoke(null); return; }

        LocalPlayerName = playerName;
        onlineScene = "Lobby";
        offlineScene = "Menu";
        maxConnections = 10;
        _sessionDestroyed = false;

        string code = EOSLobbyCode.GenerateCode();

        EOSLobbyCode.Instance.CreateSession(code, readyCode =>
        {
            CurrentLobbyCode = readyCode;
            _sessionDestroyed = false;
            StartHost();
            onCodeReady?.Invoke(readyCode);
        }, () =>
        {
            Debug.LogError("[GNM] CreateSession failed");
            onCodeReady?.Invoke(null);
        });
    }

    public void JoinGame(string lobbyCode, string playerName, System.Action onFailed)
    {
        if (EOSLobbyCode.Instance == null) { Debug.LogError("[GNM] EOSLobbyCode null"); onFailed?.Invoke(); return; }
        LocalPlayerName = playerName;
        onlineScene = "Lobby";
        offlineScene = "Menu";

        EOSLobbyCode.Instance.FindSession(lobbyCode,
            hostId => { networkAddress = hostId; StartClient(); },
            () => { Debug.LogError($"[GNM] Code not found: {lobbyCode}"); onFailed?.Invoke(); });
    }

    public void StartGame()
    {
        if (!NetworkServer.active) { Debug.LogError("[GNM] StartGame: server not active"); return; }
        DestroySessionOnce();
        ServerChangeScene("Game");
    }

    public void QuickTestGame()
    {
        if (NetworkServer.active || NetworkClient.active) { Debug.LogError("[GNM] QuickTestGame: already running"); return; }
        onlineScene = "Game";
        offlineScene = "Menu";
        maxConnections = 1;
        _sessionDestroyed = true; // skip EOS session destroy on stop
        StartHost();
    }

    public void LeaveGame()
    {
        Debug.Log($"[GNM] LeaveGame — isServer={NetworkServer.active} isClient={NetworkClient.isConnected}");

        // Only host destroys the session
        if (NetworkServer.active)
            DestroySessionOnce();

        if (NetworkServer.active && NetworkClient.isConnected) StopHost();
        else if (NetworkClient.isConnected) StopClient();
        else if (NetworkServer.active) StopServer();
    }

    void DestroySessionOnce()
    {
        if (_sessionDestroyed) return;
        if (string.IsNullOrEmpty(CurrentLobbyCode)) return;
        _sessionDestroyed = true;
        CurrentLobbyCode = string.Empty;
        EOSLobbyCode.Instance?.DestroySession();
        Debug.Log("[GNM] DestroySessionOnce called");
    }

    private void UpdateSessionPlayerCount()
    {
        if (!NetworkServer.active || EOSLobbyCode.Instance == null || string.IsNullOrEmpty(CurrentLobbyCode)) return;
        EOSLobbyCode.Instance.UpdateSessionPlayerCount(NetworkServer.connections.Count);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (networkSceneName.Contains("Lobby"))
        {
            if (lobbyPlayerPrefab == null) { Debug.LogError("[GNM] lobbyPlayerPrefab not assigned!"); return; }
            NetworkServer.AddPlayerForConnection(conn, Instantiate(lobbyPlayerPrefab));
        }
        else
        {
            if (playerPrefab == null) { Debug.LogError("[GNM] playerPrefab not assigned!"); return; }
            var pos = GetStartPosition()?.position ?? Vector3.zero;
            NetworkServer.AddPlayerForConnection(conn, Instantiate(playerPrefab, pos, Quaternion.identity));
        }
        Debug.Log($"[GNM] OnServerAddPlayer conn={conn.connectionId} scene={networkSceneName}");
        UpdateSessionPlayerCount();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        bool wasHost = conn.connectionId == 0;
        int remainingAfter = NetworkServer.connections.Count - 1;

        base.OnServerDisconnect(conn);

        // Skip if server is shutting down
        if (!NetworkServer.active) return;

        UpdateSessionPlayerCount();

        Debug.Log($"[GNM] OnServerDisconnect conn={conn.connectionId} wasHost={wasHost} remaining={remainingAfter}");

        if (remainingAfter <= 0)
        {
            Debug.Log("[GNM] Last player left — closing room");
            DestroySessionOnce();
        }
        else if (wasHost)
        {
            Debug.Log("[GNM] Host left — transferring host");
            TransferHost();
        }
    }

    void TransferHost()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            var lp = conn.identity?.GetComponent<LobbyPlayer>();
            if (lp != null) lp.isHost = false;
        }
        foreach (var conn in NetworkServer.connections.Values)
        {
            var player = conn.identity?.GetComponent<LobbyPlayer>();
            if (player != null)
            {
                player.isHost = true;
                player.RpcBecomeHost();
                Debug.Log($"[GNM] Host transferred to {player.playerName}");
                break;
            }
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[GNM] OnClientDisconnect");
        if (!NetworkClient.isConnected)
            LobbyPlayer.All.Clear();
    }

    public override void OnStartHost() { base.OnStartHost(); Debug.Log("[GNM] OnStartHost"); }

    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("[GNM] OnStopHost");
        LobbyPlayer.All.Clear();
        _sessionDestroyed = false;
    }

    public override void OnStartClient() { base.OnStartClient(); Debug.Log("[GNM] OnStartClient"); }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("[GNM] OnStopClient");
        LobbyPlayer.All.Clear();
        // Use Invoke to delay scene load by one frame to avoid mid-frame issues
        if (!string.IsNullOrWhiteSpace(offlineScene) && !Mirror.Utils.IsSceneActive(offlineScene))
            UnityEngine.SceneManagement.SceneManager.LoadScene(offlineScene);
    }

    public override void OnStartServer() { base.OnStartServer(); Debug.Log("[GNM] OnStartServer"); }
    public override void OnStopServer() { base.OnStopServer(); Debug.Log("[GNM] OnStopServer"); }

    public override void OnServerSceneChanged(string s)
    {
        base.OnServerSceneChanged(s);
        Debug.Log($"[GNM] ServerSceneChanged: {s}");
        if (s.Contains("Game"))
            LobbyPlayer.All.Clear();
    }

    public override void OnClientChangeScene(string s, SceneOperation op, bool custom)
    {
        base.OnClientChangeScene(s, op, custom);
        Debug.Log($"[GNM] ClientChangeScene: {s}");
    }

    public override void OnClientConnect() { base.OnClientConnect(); Debug.Log("[GNM] OnClientConnect"); }
}
