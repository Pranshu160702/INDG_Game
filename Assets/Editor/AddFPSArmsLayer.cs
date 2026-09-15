using UnityEngine;
using UnityEditor;

public class AddFPSArmsLayer
{
    [MenuItem("INDG/Setup/Add FPSArms Layer")]
    static void AddLayer()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");

        AddIfMissing(layers, "FPSArms");
        AddIfMissing(layers, "PlayerBody");

        tagManager.ApplyModifiedProperties();
    }

    static void AddIfMissing(SerializedProperty layers, string layerName)
    {
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                Debug.Log($"[INDG] Layer '{layerName}' already exists.");
                return;
            }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                Debug.Log($"[INDG] Layer '{layerName}' added at index {i}.");
                return;
            }
        }

        Debug.LogError($"[INDG] No empty slot for layer '{layerName}'.");
    }
}
