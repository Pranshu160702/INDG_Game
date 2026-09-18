using System.Collections.Generic;
using UnityEngine;

/// Singleton. Place one in your scene (or on a persistent manager object).
/// Assign surface effects in the inspector.
public class SurfaceManager : MonoBehaviour
{
    public static SurfaceManager Instance { get; private set; }

    [System.Serializable]
    public struct SurfaceEntry
    {
        public SurfaceType surfaceType;
        public SurfaceImpactEffect effect;
    }

    [Header("Surface Effects")]
    public SurfaceEntry[] surfaces;

    [Header("Fallback")]
    [Tooltip("Used when no matching SurfaceTag is found")]
    public SurfaceImpactEffect defaultEffect;

    private readonly Queue<AudioSource> audioPool = new Queue<AudioSource>();
    private const int AudioPoolSize = 8;

    // Built at Awake from the surfaces array — O(1) lookup instead of O(n) foreach
    private readonly Dictionary<SurfaceType, SurfaceImpactEffect> effectMap
        = new Dictionary<SurfaceType, SurfaceImpactEffect>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Build O(1) lookup map
        effectMap.Clear();
        if (surfaces != null)
            foreach (var entry in surfaces)
                effectMap[entry.surfaceType] = entry.effect;

        for (int i = 0; i < AudioPoolSize; i++)
        {
            GameObject go = new GameObject("ImpactAudio");
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            src.maxDistance  = 50f;
            go.SetActive(false);
            audioPool.Enqueue(src);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// Call this from GunScript when a bullet hits something.
    /// hitCollider can be null when called from a ClientRpc (collider not available remotely).
    public void HandleImpact(Collider hitCollider, Vector3 point, Vector3 normal, bool isPlayer)
    {
        SurfaceType type = isPlayer ? SurfaceType.Flesh : (hitCollider != null ? GetSurfaceType(hitCollider) : SurfaceType.Default);
        SurfaceImpactEffect effect = GetEffect(type);
        if (effect == null) return;

        SpawnParticle(effect, point, normal);
        PlayAudio(effect, point);
    }

    SurfaceType GetSurfaceType(Collider col)
    {
        // Walk up hierarchy looking for a SurfaceTag
        SurfaceTag tag = col.GetComponentInParent<SurfaceTag>();
        return tag != null ? tag.surfaceType : SurfaceType.Default;
    }

    SurfaceImpactEffect GetEffect(SurfaceType type)
    {
        return effectMap.TryGetValue(type, out var effect) ? effect : defaultEffect;
    }

    void SpawnParticle(SurfaceImpactEffect effect, Vector3 point, Vector3 normal)
    {
        if (effect.particlePrefab == null) return;
        GameObject fx = Instantiate(effect.particlePrefab, point + normal * 0.01f, Quaternion.LookRotation(normal));
        Destroy(fx, effect.particleLifetime);
    }

    void PlayAudio(SurfaceImpactEffect effect, Vector3 point)
    {
        if (effect.audioClips == null || effect.audioClips.Length == 0) return;

        AudioSource src = GetAudioSource();
        src.transform.position = point;
        src.gameObject.SetActive(true);

        AudioClip clip = effect.audioClips[Random.Range(0, effect.audioClips.Length)];
        src.pitch  = Random.Range(effect.pitchRange.x, effect.pitchRange.y);
        src.volume = effect.volume;
        src.PlayOneShot(clip);

        StartCoroutine(ReturnAudioSource(src, clip.length + 0.1f));
    }

    AudioSource GetAudioSource()
    {
        if (audioPool.Count > 0)
        {
            var src = audioPool.Dequeue();
            if (src != null) return src;
        }
        // Pool exhausted — create a temporary one
        GameObject go = new GameObject("ImpactAudio_Temp");
        go.transform.SetParent(transform);
        AudioSource s = go.AddComponent<AudioSource>();
        s.spatialBlend = 1f;
        s.rolloffMode  = AudioRolloffMode.Linear;
        s.maxDistance  = 50f;
        return s;
    }

    System.Collections.IEnumerator ReturnAudioSource(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        src.gameObject.SetActive(false);
        audioPool.Enqueue(src);
    }
}
