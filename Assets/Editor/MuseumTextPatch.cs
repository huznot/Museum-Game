using UnityEditor;
using UnityEngine;
using MicrophoneInput;

public static class MuseumTextPatch
{
    [MenuItem("Tools/Patch Museum Text Fixes")]
    static void Patch()
    {
        var mm = Object.FindFirstObjectByType<MuseumMicrophone>();
        if (mm == null) { Debug.LogError("[MuseumTextPatch] No MuseumMicrophone found in scene."); return; }

        SerializedObject so = new SerializedObject(mm);
        SerializedProperty acts = so.FindProperty("acts");

        // Act 1 (index 0), SubDisplay 1 (index 0), line 2 (index 1): Djenné → Djenne
        var sub0 = acts.GetArrayElementAtIndex(0).FindPropertyRelative("subDisplays").GetArrayElementAtIndex(0);
        var subs0 = sub0.FindPropertyRelative("subtitles");
        if (subs0.arraySize > 1)
        {
            var line = subs0.GetArrayElementAtIndex(1);
            line.stringValue = line.stringValue.Replace("Djenné", "Djenne");
        }

        // Act 1 (index 0), SubDisplay 2 (index 1), line 2 (index 1): em dash → comma
        var sub1 = acts.GetArrayElementAtIndex(0).FindPropertyRelative("subDisplays").GetArrayElementAtIndex(1);
        var subs1 = sub1.FindPropertyRelative("subtitles");
        if (subs1.arraySize > 1)
        {
            var line = subs1.GetArrayElementAtIndex(1);
            line.stringValue = line.stringValue.Replace(" \u2014 ", ", ");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mm);
        Debug.Log("[MuseumTextPatch] Text fixes applied.");
    }
}
