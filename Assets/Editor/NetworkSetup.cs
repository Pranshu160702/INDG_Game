using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using EpicTransport;
using Mirror;

public class NetworkSetup
{
    [MenuItem("Tools/Setup Network")]
    public static void SetupNetwork()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        // Clean up old network manager if any
        var old = GameObject.Find("NetworkManager");
        if (old != null) GameObject.DestroyImmediate(old);

        // NetworkManager GameObject
        GameObject nmObj = new GameObject("NetworkManager");

        // Add EOS SDK Component
        var eosSDK = nmObj.AddComponent<EOSSDKComponent>();

        // Add EOS Transport
        var eosTransport = nmObj.AddComponent<EosTransport>();

        // Add our NetworkManager and wire up transport
        var netManager = nmObj.AddComponent<GameNetworkManager>();
        netManager.transport = eosTransport;
        Mirror.Transport.active = eosTransport;

        // Load and assign the EosApiKey asset
        var apiKey = AssetDatabase.LoadAssetAtPath<EosApiKey>("Assets/Scripts/EOSApiKey.asset");
        if (apiKey != null)
        {
            var so = new SerializedObject(eosSDK);
            so.FindProperty("apiKeys").objectReferenceValue = apiKey;
            so.ApplyModifiedProperties();
            Debug.Log("EosApiKey assigned to EOSSDKComponent.");
        }
        else
        {
            Debug.LogWarning("EOSApiKey.asset not found. Run Tools > Setup EOS Api Key first.");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Network setup done.");
    }
}
