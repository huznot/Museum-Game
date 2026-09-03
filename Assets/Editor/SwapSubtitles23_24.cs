using UnityEditor;
using UnityEngine;
using MicrophoneInput;

public static class SwapSubtitles23_24
{
    [MenuItem("Tools/Swap Subtitles 2.3 and 2.4")]
    static void Swap()
    {
        var mm = Object.FindFirstObjectByType<MuseumMicrophone>();
        if (mm == null) { Debug.LogError("No MuseumMicrophone found."); return; }

        var so   = new SerializedObject(mm);
        var act2 = so.FindProperty("acts").GetArrayElementAtIndex(1); // Act 2 (0-indexed)
        var subs = act2.FindPropertyRelative("subDisplays");

        var sub23 = subs.GetArrayElementAtIndex(2); // 2.3
        var sub24 = subs.GetArrayElementAtIndex(3); // 2.4

        // Read both subtitle arrays
        string[] st23 = ReadStringArray(sub23.FindPropertyRelative("subtitles"));
        string[] st24 = ReadStringArray(sub24.FindPropertyRelative("subtitles"));
        float[]  dl23 = ReadFloatArray(sub23.FindPropertyRelative("transitionDelays"));
        float[]  dl24 = ReadFloatArray(sub24.FindPropertyRelative("transitionDelays"));

        // Write them swapped
        WriteStringArray(sub23.FindPropertyRelative("subtitles"), st24);
        WriteStringArray(sub24.FindPropertyRelative("subtitles"), st23);
        WriteFloatArray(sub23.FindPropertyRelative("transitionDelays"), dl24);
        WriteFloatArray(sub24.FindPropertyRelative("transitionDelays"), dl23);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mm);
        Debug.Log("Subtitles and delays for 2.3 and 2.4 swapped.");
    }

    static string[] ReadStringArray(SerializedProperty prop)
    {
        var arr = new string[prop.arraySize];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = prop.GetArrayElementAtIndex(i).stringValue;
        return arr;
    }

    static float[] ReadFloatArray(SerializedProperty prop)
    {
        var arr = new float[prop.arraySize];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = prop.GetArrayElementAtIndex(i).floatValue;
        return arr;
    }

    static void WriteStringArray(SerializedProperty prop, string[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    static void WriteFloatArray(SerializedProperty prop, float[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).floatValue = values[i];
    }
}
