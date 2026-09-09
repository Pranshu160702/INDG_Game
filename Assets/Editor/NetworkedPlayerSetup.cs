using UnityEngine;
using UnityEditor;
using Mirror;

public class NetworkedPlayerSetup
{
    [MenuItem("Tools/Create Networked Player Prefab")]
    public static void CreateNetworkedPlayerPrefab()
    {
        // Root
        GameObject player = new GameObject("NetworkedPlayer");
        player.transform.position = Vector3.zero;

        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1f, 0);
        cc.height = 2f;
        cc.radius = 0.4f;

        player.AddComponent<NetworkIdentity>();
        player.AddComponent<PlayerController>().enabled = false;
        player.AddComponent<NetworkPlayer>();

        // Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 1f, 0);
        body.transform.localScale = Vector3.one;

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0, 2.2f, 0);
        head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Save as prefab
        string path = "Assets/Prefabs/NetworkedPlayer.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(player, path);
        Object.DestroyImmediate(player);

        Debug.Log("NetworkedPlayer prefab created at " + path);
    }
}
