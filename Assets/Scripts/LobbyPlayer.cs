using Mirror;
using UnityEngine;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))] public string playerName;
    [SyncVar(hook = nameof(OnCodeChanged))] public string roomCode;
    [SyncVar(hook = nameof(OnHostChanged))] public bool isHost;

    public static System.Collections.Generic.List<LobbyPlayer> All = new();

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All = new();

    void OnNameChanged(string _, string newName)
    {
        Debug.Log($"[LobbyPlayer] Name changed to: {newName}");
        LobbyUI.Instance?.RefreshPlayerList();
    }

    void OnCodeChanged(string _, string newCode)
    {
        Debug.Log($"[LobbyPlayer] Code changed to: {newCode}");
        LobbyUI.Instance?.UpdateRoomCode(newCode);
    }

    void OnHostChanged(bool _, bool newIsHost)
    {
        Debug.Log($"[LobbyPlayer] isHost changed to: {newIsHost} for {playerName}");
        LobbyUI.Instance?.RefreshPlayerList();
        // Update start button visibility
        if (LobbyUI.Instance != null && LobbyUI.Instance.startButton != null)
            LobbyUI.Instance.startButton.gameObject.SetActive(newIsHost && isLocalPlayer);
    }

    public override void OnStartClient()
    {
        Debug.Log($"[LobbyPlayer] OnStartClient — name={playerName}, code={roomCode}");
        if (!All.Contains(this)) All.Add(this);
        LobbyUI.Instance?.RefreshPlayerList();
        if (!string.IsNullOrEmpty(roomCode))
            LobbyUI.Instance?.UpdateRoomCode(roomCode);
    }

    public override void OnStopClient()
    {
        Debug.Log($"[LobbyPlayer] OnStopClient — name={playerName}");
        All.Remove(this);
        // Only refresh if LobbyUI is still alive (not during scene teardown)
        if (LobbyUI.Instance != null && LobbyUI.Instance.gameObject != null)
            LobbyUI.Instance.RefreshPlayerList();
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[LobbyPlayer] OnStartLocalPlayer — sending name: {GameNetworkManager.LocalPlayerName}");
        CmdSetName(GameNetworkManager.LocalPlayerName);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (NetworkServer.active)
        {
            roomCode = GameNetworkManager.CurrentLobbyCode;
            isHost = connectionToClient != null && connectionToClient.connectionId == 0;
        }
    }

    [ClientRpc]
    public void RpcBecomeHost()
    {
        Debug.Log($"[LobbyPlayer] {playerName} is now the host");
        if (isLocalPlayer)
        {
            if (LobbyUI.Instance?.startButton != null)
                LobbyUI.Instance.startButton.gameObject.SetActive(true);
            if (LobbyUI.Instance != null)
                LobbyUI.Instance.SetStatus("You are now the host");
        }
    }

    [Command]
    void CmdSetName(string name) => playerName = name;
}
