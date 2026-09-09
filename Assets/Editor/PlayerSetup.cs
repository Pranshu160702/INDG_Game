using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PlayerSetup
{
    [MenuItem("Tools/Setup Player")]
    public static void SetupPlayer()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        var oldPlayer = GameObject.Find("Player");
        if (oldPlayer != null) GameObject.DestroyImmediate(oldPlayer);

        // Root: empty GameObject with CharacterController and PlayerController
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0f, -3f);
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1f, 0);
        cc.height = 2f;
        player.AddComponent<PlayerController>();

        // Capsule body as child mesh only
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 1f, 0);
        GameObject.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // Sphere head as child
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0, 2.2f, 0);
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        GameObject.DestroyImmediate(head.GetComponent<SphereCollider>());

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Player setup done.");
    }
}
