using UnityEngine;

[CreateAssetMenu(fileName = "NewSurfaceImpactEffect", menuName = "Weapons/Surface Impact Effect")]
public class SurfaceImpactEffect : ScriptableObject
{
    [Header("Particle")]
    [Tooltip("Particle prefab spawned at hit point. Leave null to skip.")]
    public GameObject particlePrefab;
    [Tooltip("How long before the particle is destroyed")]
    public float particleLifetime = 2f;

    [Header("Audio")]
    [Tooltip("One is chosen at random each hit")]
    public AudioClip[] audioClips;
    [Range(0f, 1f)] public float volume = 0.8f;
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
}
