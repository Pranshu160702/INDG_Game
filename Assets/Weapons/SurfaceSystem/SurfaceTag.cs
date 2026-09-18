using UnityEngine;

public enum SurfaceType
{
    Default,
    Metal,
    Wood,
    Stone,
    Sand,
    Flesh,
    Water
}

/// Attach to any GameObject (or its root) to define its surface type.
/// SurfaceManager will walk up the hierarchy to find this if not on the hit collider directly.
public class SurfaceTag : MonoBehaviour
{
    public SurfaceType surfaceType = SurfaceType.Default;
}
