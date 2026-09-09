using UnityEngine;
using UnityEditor;

public class EOSSetup
{
    [MenuItem("Tools/Setup EOS Api Key")]
    public static void SetupEOSApiKey()
    {
        var apiKey = ScriptableObject.CreateInstance<EosApiKey>();
        apiKey.epicProductName    = "INDG";
        apiKey.epicProductVersion = "1.0";
        apiKey.epicProductId      = EOSConfig.ProductId;
        apiKey.epicSandboxId      = EOSConfig.SandboxId;
        apiKey.epicDeploymentId   = EOSConfig.DeploymentId;
        apiKey.epicClientId       = EOSConfig.ClientId;
        apiKey.epicClientSecret   = EOSConfig.ClientSecret;

        AssetDatabase.CreateAsset(apiKey, "Assets/Scripts/EOSApiKey.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("EOSApiKey.asset created.");
    }
}
