using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SpineAimOffsetSync : MonoBehaviour
{
    public PlayerController playerController;
    public MultiAimConstraint spineAimConstraint;

    void LateUpdate()
    {
        if (playerController == null || spineAimConstraint == null) return;

        // xRotation: -80 (full up) → offsetX -75, +80 (full down) → offsetX +75
        float xRot = Mathf.Clamp(playerController.xRotation, -80f, 80f);
        float offsetX = Mathf.Lerp(75f, -75f, (xRot + 80f) / 160f);

        var data = spineAimConstraint.data;
        data.offset = new Vector3(offsetX, data.offset.y, data.offset.z);
        spineAimConstraint.data = data;
    }
}
