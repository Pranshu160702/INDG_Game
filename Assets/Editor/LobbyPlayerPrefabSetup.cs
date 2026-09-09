using UnityEngine;
using UnityEditor;
using Mirror;

public class LobbyPlayerPrefabSetup
{
    [MenuItem("Tools/Create LobbyPlayer Prefab")]
    public static void CreateLobbyPlayerPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var obj = new GameObject("LobbyPlayer");
        obj.AddComponent<NetworkIdentity>();
        obj.AddComponent<LobbyPlayer>();

        PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Prefabs/LobbyPlayer.prefab");
        Object.DestroyImmediate(obj);

        Debug.Log("LobbyPlayer prefab created at Assets/Prefabs/LobbyPlayer.prefab — now drag it into NetworkManager's Registered Spawnable Prefabs list.");
    }
}
