using UnityEngine;

/// Attach to the Crosshair Canvas. Drag the FPS camera here.
/// The canvas is shown only when the FPS camera is enabled.
public class Crosshair : MonoBehaviour
{
    public Camera fpsCamera;
    private Canvas canvas;

    void Awake() => canvas = GetComponent<Canvas>();

    void Update()
    {
        if (canvas != null && fpsCamera != null)
            canvas.enabled = fpsCamera.enabled;
    }
}
