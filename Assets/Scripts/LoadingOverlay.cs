using UnityEngine;

public class LoadingOverlay : MonoBehaviour
{
    public static LoadingOverlay Instance { get; private set; }
    public float spinSpeed = 300f;

    private static GameObject overlayInstance;
    private static RectTransform spinner;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Show()
    {
        if (overlayInstance != null) return;

        // Try Resources/Prefabs first, then Resources root
        var prefab = Resources.Load<GameObject>("Prefabs/LoadingOverlay");
        if (prefab == null) prefab = Resources.Load<GameObject>("LoadingOverlay");

        if (prefab == null)
        {
            Debug.LogError("[LoadingOverlay] Prefab not found. Move LoadingOverlay.prefab to Assets/Resources/ or Assets/Resources/Prefabs/");
            return;
        }

        overlayInstance = Instantiate(prefab);
        overlayInstance.name = "LoadingOverlay(Runtime)";

        // Parent to the top-level canvas
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            overlayInstance.transform.SetParent(canvas.transform, false);
            // Ensure it's on top
            overlayInstance.transform.SetAsLastSibling();
        }

        // Activate all children
        foreach (Transform child in overlayInstance.transform)
            child.gameObject.SetActive(true);

        // Find spinner
        spinner = null;
        foreach (Transform t in overlayInstance.GetComponentsInChildren<Transform>(true))
            if (t.name == "Spinner") { spinner = t.GetComponent<RectTransform>(); break; }

        Debug.Log("[LoadingOverlay] Shown");
    }

    public static void Hide()
    {
        if (overlayInstance != null)
        {
            Destroy(overlayInstance);
            overlayInstance = null;
            spinner = null;
            Debug.Log("[LoadingOverlay] Hidden");
        }
    }

    void Update()
    {
        if (spinner != null && overlayInstance != null)
            spinner.Rotate(0, 0, -spinSpeed * Time.deltaTime);
    }
}
