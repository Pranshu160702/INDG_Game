using UnityEngine;
using Mirror;

public class NetworkUI : MonoBehaviour
{
    private string joinCode = "";
    private string displayCode = "";

    void OnGUI()
    {
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            GUILayout.Label(NetworkServer.active ? $"Hosting - Code: {displayCode}" : "Connected");
            if (GUILayout.Button("Disconnect")) FindAnyObjectByType<GameNetworkManager>().LeaveGame();
            return;
        }

        if (GUILayout.Button("Host"))
            FindAnyObjectByType<GameNetworkManager>().HostGame("Player", (code) => displayCode = code);

        GUILayout.Label("Lobby Code:");
        joinCode = GUILayout.TextField(joinCode, GUILayout.Width(200));
        if (GUILayout.Button("Join"))
            FindAnyObjectByType<GameNetworkManager>().JoinGame(joinCode, "Player", () => Debug.LogError("Lobby not found"));
    }
}
