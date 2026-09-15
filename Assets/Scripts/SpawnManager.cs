using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    [Tooltip("Tag all spawn point GameObjects with 'SpawnPoint'")]
    private Transform[] spawnPoints;

    void Awake()
    {
        instance = this;
        var gos = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints = new Transform[gos.Length];
        for (int i = 0; i < gos.Length; i++)
            spawnPoints[i] = gos[i].transform;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Returns the spawn point furthest from all living players
    public Transform GetBestSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform;

        var players = GameObject.FindGameObjectsWithTag("Player");

        Transform best = spawnPoints[0];
        float bestDist = 0f;

        foreach (var sp in spawnPoints)
        {
            float minDist = float.MaxValue;
            foreach (var p in players)
            {
                float d = Vector3.Distance(sp.position, p.transform.position);
                if (d < minDist) minDist = d;
            }
            if (minDist > bestDist)
            {
                bestDist = minDist;
                best = sp;
            }
        }

        return best;
    }

    // Simple random spawn fallback
    public Transform GetRandomSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
