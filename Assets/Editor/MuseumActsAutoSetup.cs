using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MicrophoneInput;

public static class MuseumActsAutoSetup
{
    private const string AudioRoot = "Assets/_Project/Audio/MuseumLines";

    [MenuItem("Tools/Auto Setup Museum Acts")]
    static void Setup()
    {
        var mm = UnityEngine.Object.FindFirstObjectByType<MuseumMicrophone>();
        if (mm == null)
        {
            Debug.LogError("[MuseumActsAutoSetup] No MuseumMicrophone found in the open scene.");
            return;
        }

        Transform root = mm.transform;
        SerializedObject so = new SerializedObject(mm);

        // ── Intro Greeting ────────────────────────────────────────────────────
        LoadVoiceLines(so.FindProperty("introVoiceLines"), $"{AudioRoot}/Intro");
        SetStringArray(so.FindProperty("introSubtitles"), new[]
        {
            "Welcome to the Black History Museum.",
            "This is not just a collection of facts and dates.",
            "It is a story.",
            "And it starts long before anything you may have been taught.",
            "Follow me."
        });
        SetFloatArray(so.FindProperty("introTransitionDelays"), new[] { 0.5f, 0.5f, 0.5f, 0.6f, 0.4f });
        Debug.Log("[MuseumActsAutoSetup] Intro greeting set");

        // ── Acts ──────────────────────────────────────────────────────────────
        SerializedProperty actsProp = so.FindProperty("acts");
        actsProp.arraySize = 3;

        // ── ACT 1 — Africa Before ─────────────────────────────────────────────
        {
            var act = actsProp.GetArrayElementAtIndex(0);
            act.FindPropertyRelative("actName").stringValue = "Africa Before";
            act.FindPropertyRelative("actStartDelay").floatValue = 0f;
            act.FindPropertyRelative("loadSceneOnComplete").boolValue = false;

            var subs = act.FindPropertyRelative("subDisplays");
            subs.arraySize = 3;

            SetSubDisplay(subs.GetArrayElementAtIndex(0), root, "1.1", "Architecture & Cities",
                $"{AudioRoot}/1.1",
                new[]
                {
                    "Great Zimbabwe was a stone city housing 18,000 people, built without a single drop of mortar.",
                    "The Great Mosque of Djenne has been standing for over 700 years.",
                    "Timbuktu had scholars traveling from Europe and Asia just to study there.",
                    "They were not coming to teach. They were coming to learn.",
                    "These cities were built, thriving, and trading long before European contact."
                },
                new[] { 0.5f, 0.5f, 0.5f, 0.4f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 1 — SubDisplay 1.1 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(1), root, "1.2", "Arts & Culture",
                $"{AudioRoot}/1.2",
                new[]
                {
                    "Every object in this room was made with a specific purpose.",
                    "The masks, the sculptures, the jewellery, none of it was decoration.",
                    "When a mask was worn, the person inside it was understood to become the spirit it represented.",
                    "These portrait heads come from the Yoruba city of Ife in Nigeria, dating back to the 12th century.",
                    "When Europeans encountered them in the 1800s, many refused to believe Africans had made them.",
                    "The craftsmanship speaks for itself."
                },
                new[] { 0.5f, 0.5f, 0.6f, 0.6f, 0.5f, 0.4f });
            Debug.Log("[MuseumActsAutoSetup] Act 1 — SubDisplay 1.2 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(2), root, "1.3", "The Great Empires",
                $"{AudioRoot}/1.3",
                new[]
                {
                    "Mansa Musa was so wealthy that when he passed through Egypt in 1324,",
                    "he gave away enough gold to crash their entire economy.",
                    "Just passing through.",
                    "The Kingdom of Kush had more pyramids than Egypt.",
                    "Timbuktu had one of the first universities on earth with over 25,000 students.",
                    "The Mali Empire, the Songhai, the Kingdom of Benin.",
                    "These were some of the most powerful civilisations the world has ever seen."
                },
                new[] { 0.3f, 0.5f, 0.6f, 0.5f, 0.5f, 0.5f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 1 — SubDisplay 1.3 set");

            SetQuiz(act,
                question: "Who was considered the wealthiest person in all of human history?",
                opt1: "Mansa Musa", opt2: "Julius Caesar", opt3: "N/A", correctIndex: 0,
                correctSub: "Correct! Mansa Musa of the Mali Empire is considered the wealthiest person in history.",
                wrongSub: "Not quite. Mansa Musa of the Mali Empire is considered the wealthiest person in history.");
            Debug.Log("[MuseumActsAutoSetup] Act 1 complete");
        }

        // ── ACT 2 — Enslavement & Resistance ─────────────────────────────────
        {
            var act = actsProp.GetArrayElementAtIndex(1);
            act.FindPropertyRelative("actName").stringValue = "Enslavement & Resistance";
            act.FindPropertyRelative("actStartDelay").floatValue = 0f;
            act.FindPropertyRelative("loadSceneOnComplete").boolValue = false;

            var subs = act.FindPropertyRelative("subDisplays");
            subs.arraySize = 4;

            SetSubDisplay(subs.GetArrayElementAtIndex(0), root, "2.1", "The Slave Trade",
                $"{AudioRoot}/2.1",
                new[]
                {
                    "Between the 1600s and 1800s, between 12 and 15 million people were taken from Africa by force.",
                    "Loaded onto ships, chained below deck, shipped across an ocean.",
                    "An estimated 2 million did not survive the crossing.",
                    "Those who arrived were sold, separated from their families, stripped of their names.",
                    "This was not a small event in history.",
                    "It reshaped the entire continent of Africa and the lives of every person on it."
                },
                new[] { 0.6f, 0.5f, 0.5f, 0.5f, 0.4f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 2 — SubDisplay 2.1 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(1), root, "2.2", "Plantation Life",
                $"{AudioRoot}/2.2",
                new[]
                {
                    "For those who survived the crossing, this was daily life.",
                    "Working from before sunrise to after dark with nothing in return.",
                    "Families could be separated and sold on any given morning.",
                    "Despite that, communities held together.",
                    "They kept their music, their languages, their stories.",
                    "Entire cultures were maintained inside conditions designed to destroy them."
                },
                new[] { 0.5f, 0.5f, 0.5f, 0.4f, 0.5f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 2 — SubDisplay 2.2 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(2), root, "2.3", "The Underground Railroad",
                $"{AudioRoot}/2.3",
                new[]
                {
                    "It took a civil war to end slavery in America.",
                    "Over 180,000 Black men fought in that war for a country that still legally owned them.",
                    "Lincoln signed the Emancipation Proclamation in 1863.",
                    "But freedom on paper and freedom in reality are two different things.",
                    "Jim Crow laws enforced segregation for another hundred years after that.",
                    "The fight for real equality was only just beginning.",
                    "And it would take another century of resistance to make it real."
                },
                new[] { 0.5f, 0.6f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 2 — SubDisplay 2.3 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(3), root, "2.4", "Freedom & Emancipation",
                $"{AudioRoot}/2.4",
                new[]
                {
                    "Harriet Tubman escaped slavery in 1849.",
                    "Then she went back. Thirteen times.",
                    "Every trip south could have been her last.",
                    "She carried a pistol and had one rule. Nobody turns back.",
                    "She led over 70 people to freedom and never lost a single one.",
                    "She later led a military raid that freed over 700 more people in a single night.",
                    "A lit lantern meant the path ahead was safe. Follow it."
                },
                new[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.6f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 2 — SubDisplay 2.4 set");

            SetQuiz(act,
                question: "True or False: Harriet Tubman never lost a passenger on the Underground Railroad.",
                opt1: "True", opt2: "False", opt3: "N/A", correctIndex: 0,
                correctSub: "Correct! Harriet Tubman completed 13 missions and never lost a single passenger.",
                wrongSub: "Actually true. Harriet Tubman completed 13 missions and never lost a single passenger.");
            Debug.Log("[MuseumActsAutoSetup] Act 2 complete");
        }

        // ── ACT 3 — Black Excellence ──────────────────────────────────────────
        {
            var act = actsProp.GetArrayElementAtIndex(2);
            act.FindPropertyRelative("actName").stringValue = "Black Excellence";
            act.FindPropertyRelative("actStartDelay").floatValue = 0f;
            act.FindPropertyRelative("hasQuiz").boolValue = false;
            act.FindPropertyRelative("loadSceneOnComplete").boolValue = true;
            act.FindPropertyRelative("sceneToLoad").stringValue = "MuseumCredits";
            act.FindPropertyRelative("sceneLoadDelay").floatValue = 3f;

            var subs = act.FindPropertyRelative("subDisplays");
            subs.arraySize = 5;

            SetSubDisplay(subs.GetArrayElementAtIndex(0), root, "3.1", "The Harlem Renaissance",
                $"{AudioRoot}/3.1",
                new[]
                {
                    "After the war, millions of Black Americans moved North.",
                    "In Harlem in the 1920s and 30s, something remarkable happened.",
                    "Langston Hughes. Zora Neale Hurston. Aaron Douglas. Billie Holiday. Louis Armstrong.",
                    "All producing some of the greatest work in American history at the same time.",
                    "The writing, the art, and the music that came out of Harlem changed American culture permanently.",
                    "And it came from people who had been carrying that creativity through everything."
                },
                new[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.6f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 3 — SubDisplay 3.1 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(1), root, "3.2", "Athletes",
                $"{AudioRoot}/3.2",
                new[]
                {
                    "Jesse Owens won four gold medals at the 1936 Berlin Olympics.",
                    "Jackie Robinson entered Major League Baseball in 1947 knowing exactly the hostility waiting for him.",
                    "He went anyway and changed the sport forever.",
                    "Muhammad Ali won the heavyweight championship of the world.",
                    "Then gave up his title rather than fight in a war he did not believe in.",
                    "He lost three years of his career for that decision and did not back down.",
                    "These athletes were not just competing. Every win was a statement."
                },
                new[] { 0.5f, 0.6f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 3 — SubDisplay 3.2 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(2), root, "3.3", "Inventors & Innovators",
                $"{AudioRoot}/3.3",
                new[]
                {
                    "Garrett Morgan invented the traffic light in 1923.",
                    "The same man invented the gas mask that saved soldiers in World War One.",
                    "Lewis Latimer developed the carbon filament that made Edison's lightbulb last long enough to actually be useful.",
                    "Madam C.J. Walker built a haircare empire from nothing and became the first self-made female millionaire in American history.",
                    "Mark Dean co-created the IBM personal computer and holds three of the nine original patents.",
                    "Mae Jemison became the first Black woman in space in 1992.",
                    "All of this achieved while navigating a system built to exclude them."
                },
                new[] { 0.5f, 0.5f, 0.6f, 0.6f, 0.5f, 0.5f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 3 — SubDisplay 3.3 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(3), root, "3.4", "Leaders & Trailblazers",
                $"{AudioRoot}/3.4",
                new[]
                {
                    "Nelson Mandela was imprisoned for 27 years.",
                    "He was offered release if he publicly renounced his movement. He refused.",
                    "He walked out in 1990 and led South Africa out of apartheid.",
                    "Rosa Parks refused to give up her seat in 1955 and sparked a 381 day boycott.",
                    "Frederick Douglass taught himself to read in secret as an enslaved person",
                    "and became one of the most important voices in American history.",
                    "George Floyd was killed in May 2020.",
                    "Within weeks, people in over 60 countries were in the streets."
                },
                new[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.3f, 0.5f, 0.5f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 3 — SubDisplay 3.4 set");

            SetSubDisplay(subs.GetArrayElementAtIndex(4), root, "3.5", "The Modern Era",
                $"{AudioRoot}/3.5",
                new[]
                {
                    "In 2008 Barack Obama became the 44th President of the United States.",
                    "In 2022 Ketanji Brown Jackson became the first Black woman on the Supreme Court.",
                    "Simone Biles is the most decorated gymnast in history.",
                    "Kendrick Lamar won the Pulitzer Prize, the first rapper to ever receive it.",
                    "Serena Williams won 23 Grand Slam titles.",
                    "None of this was given.",
                    "Every person in this room built their achievement inside systems that were not designed for them.",
                    "And that thread runs all the way back to the first room you walked into."
                },
                new[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.4f, 0.6f, 0.5f });
            Debug.Log("[MuseumActsAutoSetup] Act 3 — SubDisplay 3.5 set");

            Debug.Log("[MuseumActsAutoSetup] Act 3 complete — Credits loads 3s after final line");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mm);
        Debug.Log("[MuseumActsAutoSetup] Setup complete");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SetSubDisplay(SerializedProperty prop, Transform parent, string spotName,
        string displayName, string audioFolder, string[] subtitles, float[] delays)
    {
        prop.FindPropertyRelative("displayName").stringValue = displayName;

        Transform spot = parent.Find(spotName);
        if (spot == null)
            Debug.LogWarning($"[MuseumActsAutoSetup] Spot '{spotName}' not found as child of '{parent.name}'");
        prop.FindPropertyRelative("spot").objectReferenceValue = spot;

        LoadVoiceLines(prop.FindPropertyRelative("voiceLines"), audioFolder);
        SetStringArray(prop.FindPropertyRelative("subtitles"), subtitles);
        SetFloatArray(prop.FindPropertyRelative("transitionDelays"), delays);
    }

    static void LoadVoiceLines(SerializedProperty prop, string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[MuseumActsAutoSetup] No audio clips found in '{folderPath}'");
            prop.arraySize = 0;
            return;
        }

        // ElevenLabs timestamps sort alphabetically in correct playback order
        List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
        paths.Sort(StringComparer.Ordinal);

        prop.arraySize = paths.Count;
        for (int i = 0; i < paths.Count; i++)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
            prop.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            if (clip == null)
                Debug.LogWarning($"[MuseumActsAutoSetup] Could not load clip: '{paths[i]}'");
        }
        Debug.Log($"[MuseumActsAutoSetup] Loaded {paths.Count} clips from '{folderPath}'");
    }

    static void SetQuiz(SerializedProperty actProp, string question,
        string opt1, string opt2, string opt3, int correctIndex,
        string correctSub, string wrongSub)
    {
        actProp.FindPropertyRelative("hasQuiz").boolValue = true;
        var quiz = actProp.FindPropertyRelative("quiz");
        quiz.FindPropertyRelative("questionText").stringValue = question;
        quiz.FindPropertyRelative("option1").stringValue = opt1;
        quiz.FindPropertyRelative("option2").stringValue = opt2;
        quiz.FindPropertyRelative("option3").stringValue = opt3;
        quiz.FindPropertyRelative("correctOptionIndex").intValue = correctIndex;
        quiz.FindPropertyRelative("correctSubtitle").stringValue = correctSub;
        quiz.FindPropertyRelative("wrongSubtitle").stringValue = wrongSub;
    }

    static void SetStringArray(SerializedProperty prop, string[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    static void SetFloatArray(SerializedProperty prop, float[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).floatValue = values[i];
    }
}
