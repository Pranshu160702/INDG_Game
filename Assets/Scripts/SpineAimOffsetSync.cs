using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SpineAimOffsetSync : MonoBehaviour
{
    public PlayerController playerController;
    public MultiAimConstraint spineAimConstraint;

    [Tooltip("The transform your MultiAimConstraint sources point at. Replaces the physical child aim object.")]
    public Transform aimTarget;

    [Tooltip("Camera used to cast the screen-center ray (FPS or TPS camera).")]
    public Camera aimCamera;

    private NetworkPlayer ownerPlayer;

    void Start()
    {
        ownerPlayer = GetComponentInParent<NetworkPlayer>();
    }

    public void SetAimCamera(Camera cam)
    {
        aimCamera = cam;
    }

    void LateUpdate()
    {
        if (ownerPlayer == null) return;
        // Only run for the local player — remote players use synced head rotation
        if (!ownerPlayer.isLocalPlayer) return;
        if (playerController == null || spineAimConstraint == null) return;

        // Drive aimTarget to screen-center raycast hit (infinite distance)
        if (aimTarget != null && aimCamera != null)
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
            Vector3 mapEndPoint = ray.GetPoint(aimCamera.farClipPlane);
            foreach (var h in hits)
            {
                if (h.collider.CompareTag("MapEnd"))
                {
                    mapEndPoint = h.point;
                    break;
                }
            }
            aimTarget.position = mapEndPoint;
        }

        // Keep spine offset in sync with vertical look angle
        float xRot = Mathf.Clamp(playerController.xRotation, -80f, 80f);
        float offsetX = Mathf.Lerp(75f, -75f, (xRot + 80f) / 160f);
        var data = spineAimConstraint.data;
        data.offset = new Vector3(offsetX, data.offset.y, data.offset.z);
        spineAimConstraint.data = data;
    }
}
