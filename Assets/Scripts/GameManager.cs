using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [Header("Game Settings")]
    public int killsToWin = 10;
    public float endGameDelay = 8f;

    [Header("UI")]
    public GameObject winnerPanel;
    public TMPro.TextMeshProUGUI winnerText;
    public TMPro.TextMeshProUGUI scoreText; // e.g. "6 / 10 kills"

    // SyncVar so all clients see live score
    [SyncVar(hook = nameof(OnScoreChanged))] private int topScore = 0;
    [SyncVar(hook = nameof(OnWinnerChanged))] private string winnerName = "";

    private bool gameEnded = false;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        if (winnerPanel != null) winnerPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Called by NetworkPlayer on server when it gets a kill
    [Server]
    public void RegisterKill(string killerName, int killerTotalKills)
    {
        if (gameEnded) return;

        if (killerTotalKills > topScore)
            topScore = killerTotalKills;

        if (killerTotalKills >= killsToWin)
        {
            gameEnded = true;
            winnerName = killerName;
            RpcEndGame(killerName);
            StartCoroutine(ReturnToLobby());
        }
    }

    [ClientRpc]
    void RpcEndGame(string winner)
    {
        if (winnerPanel != null) winnerPanel.SetActive(true);
        if (winnerText != null) winnerText.text = $"{winner} wins!";
    }

    void OnScoreChanged(int _, int newScore)
    {
        if (scoreText != null) scoreText.text = $"{newScore} / {killsToWin}";
    }

    void OnWinnerChanged(string _, string newWinner) { }

    [Server]
    IEnumerator ReturnToLobby()
    {
        yield return new WaitForSeconds(endGameDelay);
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }
}
