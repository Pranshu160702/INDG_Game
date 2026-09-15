using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// Attach to the HUD Canvas in the Game scene.
/// Assign all slots in the Inspector.
public class GameHUD : MonoBehaviour
{
    [Header("Health")]
    public Image healthBarFill;
    public TextMeshProUGUI healthText;

    [Header("Ammo")]
    public TextMeshProUGUI ammoText;        // "28 / 30"
    public Image gunIcon;                   // current gun sprite from GunData

    [Header("Blood Overlay")]
    public GameObject bloodOverlay;         // full-screen red vignette image

    [Header("Kill Feed")]
    public Transform killFeedParent;        // vertical layout group
    public GameObject killFeedEntryPrefab;  // TextMeshProUGUI entry

    [Header("Ping")]
    public TextMeshProUGUI pingText;

    [Header("Respawn")]
    public GameObject respawnPanel;
    public TextMeshProUGUI respawnText;

    [Header("Crosshair")]
    public GameObject crosshair;

    // Runtime refs — assigned by NetworkPlayer.OnStartLocalPlayer
    [HideInInspector] public NetworkPlayer networkPlayer;
    [HideInInspector] public GunScript gunScript;

    public static GameHUD instance;

    void Awake()
    {
        instance = this;
        if (bloodOverlay != null) bloodOverlay.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        UpdateHealth();
        UpdateAmmo();
        UpdatePing();
    }

    void UpdateHealth()
    {
        if (networkPlayer == null) return;
        float pct = networkPlayer.HealthPct;
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = pct;
            healthBarFill.color = new Color(1f, pct, pct, 1f);
        }
        if (healthText != null)
            healthText.text = Mathf.CeilToInt(pct * 100f).ToString();
    }

    void UpdateAmmo()
    {
        if (gunScript == null || gunScript.data == null) return;
        if (ammoText != null)
            ammoText.text = $"{gunScript.BulletsLeft} / {gunScript.data.magazineSize}";
        if (gunIcon != null && gunScript.data.gunIcon != null)
            gunIcon.sprite = gunScript.data.gunIcon;
    }

    void UpdatePing()
    {
        if (pingText == null) return;
        int ms = (int)(NetworkTime.rtt * 1000f);
        string color = ms <= 80 ? "#08FF00" : ms <= 150 ? "#FDFF00" : "#FF0000";
        pingText.text = $"<color={color}>{ms}ms</color>";
    }

    private Coroutine respawnCountdownCoroutine;

    public void ShowRespawnScreen(float seconds)
    {
        if (respawnPanel != null) respawnPanel.SetActive(true);
        if (respawnCountdownCoroutine != null) StopCoroutine(respawnCountdownCoroutine);
        respawnCountdownCoroutine = StartCoroutine(RespawnCountdown(seconds));
    }

    IEnumerator RespawnCountdown(float seconds)
    {
        float t = seconds;
        while (t > 0f)
        {
            if (respawnText != null) respawnText.text = $"Respawning in {Mathf.CeilToInt(t)}...";
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }
    }

    public void HideRespawnScreen()
    {
        if (respawnPanel != null) respawnPanel.SetActive(false);
    }

    // Called from NetworkPlayer when local player takes damage
    private Coroutine bloodOverlayCoroutine;

    public void ShowBloodHit()
    {
        if (bloodOverlayCoroutine != null) StopCoroutine(bloodOverlayCoroutine);
        bloodOverlayCoroutine = StartCoroutine(BloodOverlayRoutine());
    }

    IEnumerator BloodOverlayRoutine()
    {
        if (bloodOverlay == null) yield break;
        bloodOverlay.SetActive(true);
        yield return new WaitForSeconds(0.8f);
        bloodOverlay.SetActive(false);
    }

    // Called from NetworkPlayer when a kill happens
    public static void AddKillFeed(string killer, string victim)
    {
        if (instance == null || instance.killFeedParent == null || instance.killFeedEntryPrefab == null) return;
        GameObject entry = Instantiate(instance.killFeedEntryPrefab, instance.killFeedParent);
        entry.transform.localScale = Vector3.one;
        var tmp = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = $"{killer} <color=red>killed</color> {victim}";
        Destroy(entry, 4f);
    }
}
