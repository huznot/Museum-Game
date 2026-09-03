using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MuseumQuizUISetup
{
    [MenuItem("Tools/Auto Setup Museum Quiz UI")]
    static void Setup()
    {
        // Find Prompt including inactive objects
        GameObject promptGO = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "Prompt" && go.scene.isLoaded)
            {
                promptGO = go;
                break;
            }
        }
        if (promptGO == null)
        {
            Debug.LogError("[QuizUISetup] Could not find a GameObject named 'Prompt' in the scene.");
            return;
        }

        // Add CanvasGroup if missing
        var cg = promptGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = promptGO.AddComponent<CanvasGroup>();

        // Add MuseumQuizUI if missing
        var ui = promptGO.GetComponent<MuseumQuizUI>();
        if (ui == null) ui = promptGO.AddComponent<MuseumQuizUI>();

        var so = new SerializedObject(ui);

        // CanvasGroup
        so.FindProperty("promptGroup").objectReferenceValue = cg;

        // question text
        AssignTMP(so, "questionText", promptGO, "question");

        // option texts: Prompt/1/1-text, Prompt/2/2-text, Prompt/3/3-text
        AssignTMP(so, "option1Text", promptGO, "1/1-text");
        AssignTMP(so, "option2Text", promptGO, "2/2-text");
        AssignTMP(so, "option3Text", promptGO, "3/3-text");

        // back images
        AssignImage(so, "back1Image", promptGO, "1back");
        AssignImage(so, "back2Image", promptGO, "2back");
        AssignImage(so, "back3Image", promptGO, "3back");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(promptGO);
        Debug.Log("[QuizUISetup] MuseumQuizUI wired up on Prompt.");
    }

    static void AssignTMP(SerializedObject so, string propName, GameObject root, string childName)
    {
        var child = root.transform.Find(childName);
        if (child == null) { Debug.LogWarning($"[QuizUISetup] Child '{childName}' not found."); return; }
        var tmp = child.GetComponent<TMP_Text>();
        if (tmp == null) { Debug.LogWarning($"[QuizUISetup] No TMP_Text on '{childName}'."); return; }
        so.FindProperty(propName).objectReferenceValue = tmp;
    }

    static void AssignImage(SerializedObject so, string propName, GameObject root, string childName)
    {
        var child = root.transform.Find(childName);
        if (child == null) { Debug.LogWarning($"[QuizUISetup] Child '{childName}' not found."); return; }
        var img = child.GetComponentInChildren<RawImage>(true);
        if (img == null) { Debug.LogWarning($"[QuizUISetup] No RawImage in '{childName}' or its children."); return; }
        so.FindProperty(propName).objectReferenceValue = img;
    }
}
