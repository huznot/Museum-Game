using UnityEditor;
using UnityEngine;
using MicrophoneInput;

public static class MuseumActsPopulator
{
    [MenuItem("Tools/Populate Museum Acts")]
    static void Populate()
    {
        var mm = Object.FindFirstObjectByType<MuseumMicrophone>();
        if (mm == null)
        {
            Debug.LogError("[MuseumActsPopulator] No MuseumMicrophone found in scene.");
            return;
        }

        SerializedObject so = new SerializedObject(mm);
        SerializedProperty actsProp = so.FindProperty("acts");
        actsProp.arraySize = 3;

        // ── ACT 1 — Africa Before ─────────────────────────────────────────────
        {
            var act = actsProp.GetArrayElementAtIndex(0);
            act.FindPropertyRelative("actName").stringValue = "Africa Before";
            act.FindPropertyRelative("actStartDelay").floatValue = 0f;

            var subs = act.FindPropertyRelative("subDisplays");
            subs.arraySize = 4;

            SetSubDisplay(subs.GetArrayElementAtIndex(0), "The Great Empires",
                new[]
                {
                    "Before we get into anything else, I need to set the record straight.",
                    "Africa was not waiting to be discovered.",
                    "It had been building empires for thousands of years.",
                    "The Mali Empire under Mansa Musa was the wealthiest nation on earth.",
                    "Not just in Africa. In the entire world.",
                    "When he traveled to Mecca in 1324, he brought 60,000 people with him.",
                    "He gave away so much gold he crashed the Egyptian economy just passing through.",
                    "The Kingdom of Kush built more pyramids than Egypt.",
                    "Most people have never heard of them. That is not an accident."
                },
                new[] { 3.5f, 2.5f, 3.0f, 3.5f, 2.5f, 3.5f, 4.0f, 3.0f, 3.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(1), "Architecture & Cities",
                new[]
                {
                    "Timbuktu was not a small desert town.",
                    "It was the intellectual capital of the world.",
                    "Scholars came from Europe and Asia to study there.",
                    "Great Zimbabwe was a stone city of 18,000 people, built without mortar.",
                    "The Great Mosque of Djenne has stood for over 700 years.",
                    "These were not primitive settlements.",
                    "These were cities."
                },
                new[] { 2.5f, 2.5f, 3.0f, 3.5f, 3.0f, 2.0f, 1.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(2), "Arts & Culture",
                new[]
                {
                    "African art was never just decoration.",
                    "Every object in this room carried meaning.",
                    "Masks were not costumes.",
                    "When someone put one on they were understood to become the spirit it represented.",
                    "The portrait heads here come from the Yoruba city of Ife, dating back to the 12th century.",
                    "When Europeans first saw them they refused to believe Africans made them.",
                    "That says a lot more about Europeans than it does about the art."
                },
                new[] { 2.0f, 2.5f, 1.5f, 4.0f, 4.0f, 3.5f, 3.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(3), "Trade & Wealth",
                new[]
                {
                    "West Africa alone produced more than half the world's gold supply.",
                    "African trade routes stretched across the continent and into Europe and Asia.",
                    "This was not an isolated continent.",
                    "It was the center of global commerce.",
                    "Colonisation arrived and tried to rewrite all of that.",
                    "It could not erase what was built here."
                },
                new[] { 3.5f, 3.5f, 2.0f, 2.0f, 2.5f, 2.5f });

            SetQuiz(act,
                question: "Who was considered the wealthiest person in all of human history?",
                opt1: "Mansa Musa",
                opt2: "Julius Caesar",
                opt3: "N/A",
                correctIndex: 0,
                correctSub: "Correct! Mansa Musa of the Mali Empire is considered the wealthiest person in history.",
                wrongSub: "Not quite. Mansa Musa of the Mali Empire is considered the wealthiest person in history.");
        }

        // ── ACT 2 — Enslavement & Resistance ─────────────────────────────────
        {
            var act = actsProp.GetArrayElementAtIndex(1);
            act.FindPropertyRelative("actName").stringValue = "Enslavement & Resistance";
            act.FindPropertyRelative("actStartDelay").floatValue = 0f;

            var subs = act.FindPropertyRelative("subDisplays");
            subs.arraySize = 4;

            SetSubDisplay(subs.GetArrayElementAtIndex(0), "The Slave Trade",
                new[]
                {
                    "After everything you just saw, this is what came next.",
                    "The Transatlantic Slave Trade.",
                    "Between 12 and 15 million African people taken by force.",
                    "Packed onto ships like cargo and shipped across an ocean.",
                    "An estimated 2 million did not survive the crossing.",
                    "Those who arrived were sold, separated, stripped of their names.",
                    "This did not just hurt individuals. It collapsed entire civilisations."
                },
                new[] { 3.0f, 2.0f, 3.0f, 3.0f, 3.0f, 3.5f, 3.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(1), "Plantation Life",
                new[]
                {
                    "For those who survived the crossing, this was their reality.",
                    "Working from before sunrise to after dark with nothing in return.",
                    "Families split apart and sold without warning.",
                    "But they were never just victims.",
                    "They built entire cultures within those conditions.",
                    "Held onto their music, their language, their identity.",
                    "Resistance never stopped. Not for a single day."
                },
                new[] { 3.0f, 3.5f, 2.5f, 2.0f, 2.5f, 2.5f, 2.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(2), "The Underground Railroad",
                new[]
                {
                    "Harriet Tubman escaped slavery in 1849.",
                    "And then she went back. Thirteen times.",
                    "Each trip south was a potential death sentence.",
                    "She went anyway.",
                    "The Underground Railroad was a secret network of safe houses and brave people.",
                    "A lantern in the dark meant the path ahead was safe.",
                    "She never lost a single passenger.",
                    "Not one."
                },
                new[] { 2.5f, 2.5f, 2.5f, 1.5f, 3.5f, 3.0f, 2.0f, 1.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(3), "Freedom & Emancipation",
                new[]
                {
                    "The end of slavery required a civil war.",
                    "Over 180,000 Black men fought for a country that still legally owned them.",
                    "In 1863 Lincoln signed the Emancipation Proclamation.",
                    "On paper, people were free.",
                    "In practice, the fight had just changed shape.",
                    "Jim Crow laws kept segregation firmly in place for another century.",
                    "Rosa Parks sat down in 1955 and refused to move.",
                    "One word. No. And eventually the law changed."
                },
                new[] { 2.5f, 4.0f, 2.5f, 2.0f, 2.5f, 3.5f, 3.0f, 2.5f });

            SetQuiz(act,
                question: "True or False: Harriet Tubman never lost a passenger on the Underground Railroad.",
                opt1: "True",
                opt2: "False",
                opt3: "N/A",
                correctIndex: 0,
                correctSub: "Correct! Harriet Tubman completed 13 missions and never lost a single passenger.",
                wrongSub: "Actually true. Harriet Tubman completed 13 missions and never lost a single passenger.");
        }

        // ── ACT 3 — Black Excellence ──────────────────────────────────────────
        {
            var act = actsProp.GetArrayElementAtIndex(2);
            act.FindPropertyRelative("actName").stringValue = "Black Excellence";
            act.FindPropertyRelative("actStartDelay").floatValue = 0f;

            var subs = act.FindPropertyRelative("subDisplays");
            subs.arraySize = 5;

            SetSubDisplay(subs.GetArrayElementAtIndex(0), "The Harlem Renaissance",
                new[]
                {
                    "After slavery ended, millions of Black Americans moved North.",
                    "And in Harlem, something nobody expected happened.",
                    "A full cultural explosion.",
                    "Langston Hughes. Zora Neale Hurston. Aaron Douglas.",
                    "They gave the Black experience a voice the whole world heard.",
                    "Jazz was born in this era.",
                    "It did not just change Black music. It changed all music."
                },
                new[] { 3.0f, 2.5f, 1.5f, 2.5f, 3.0f, 2.0f, 3.0f });

            SetSubDisplay(subs.GetArrayElementAtIndex(1), "Inventors & Innovators",
                new[]
                {
                    "A lot of things you use every day exist because of Black inventors.",
                    "Garrett Morgan gave us the traffic light in 1923.",
                    "Lewis Latimer made Edison's lightbulb actually work long term.",
                    "Madam C.J. Walker built an empire from nothing.",
                    "First self-made female millionaire in American history.",
                    "Mark Dean co-created the IBM personal computer.",
                    "Mae Jemison went to space.",
                    "All of this while doors were being slammed in their faces.",
                    "They built anyway."
                },
                new[] { 3.5f, 2.5f, 3.0f, 2.5f, 2.5f, 2.5f, 1.5f, 3.0f, 1.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(2), "Athletes",
                new[]
                {
                    "Jesse Owens won four gold medals at the 1936 Olympics.",
                    "In front of Hitler. Directly proving him wrong.",
                    "Jackie Robinson broke baseball's colour barrier in 1947.",
                    "He did it knowing the hostility he would face every single day.",
                    "Muhammad Ali became champion of the world.",
                    "And used that platform to say things nobody else would say.",
                    "These were not just athletes. They were statements."
                },
                new[] { 3.0f, 2.5f, 2.5f, 3.5f, 2.0f, 3.0f, 2.5f });

            SetSubDisplay(subs.GetArrayElementAtIndex(3), "Leaders & Trailblazers",
                new[]
                {
                    "Nelson Mandela spent 27 years in prison for his beliefs.",
                    "He came out and led his country anyway.",
                    "Rosa Parks refused to move and sparked a 381 day boycott.",
                    "Frederick Douglass taught himself to read in secret and changed history.",
                    "George Floyd's death in 2020 sparked a global reckoning.",
                    "Millions of people in every country took to the streets.",
                    "His name became a turning point."
                },
                new[] { 3.0f, 2.5f, 3.0f, 3.5f, 3.0f, 3.0f, 2.0f });

            SetSubDisplay(subs.GetArrayElementAtIndex(4), "The Modern Era",
                new[]
                {
                    "This is where the story is right now.",
                    "Barack Obama became the 44th President of the United States.",
                    "Ketanji Brown Jackson sits on the Supreme Court.",
                    "Kendrick Lamar won the Pulitzer Prize. First rapper ever.",
                    "Simone Biles is the most decorated gymnast in history.",
                    "None of this was given.",
                    "It was built on every person you have seen in this museum.",
                    "The story is not over. It never has been."
                },
                new[] { 2.0f, 3.0f, 2.5f, 3.0f, 2.5f, 1.5f, 3.5f, 2.5f });

            SetQuiz(act,
                question: "True or False: Kendrick Lamar was the first rapper to win a Pulitzer Prize.",
                opt1: "True",
                opt2: "False",
                opt3: "N/A",
                correctIndex: 0,
                correctSub: "Correct! Kendrick Lamar won the Pulitzer Prize for Music in 2018.",
                wrongSub: "Actually true. Kendrick Lamar won the Pulitzer Prize for Music in 2018.");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mm);
        Debug.Log("[MuseumActsPopulator] Successfully populated 3 acts on " + mm.gameObject.name);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SetSubDisplay(SerializedProperty prop, string displayName, string[] subtitles, float[] delays)
    {
        prop.FindPropertyRelative("displayName").stringValue = displayName;
        prop.FindPropertyRelative("voiceLines").arraySize = 0;
        // spot left unassigned — assign manually in Inspector
        SetStringArray(prop.FindPropertyRelative("subtitles"), subtitles);
        SetFloatArray(prop.FindPropertyRelative("transitionDelays"), delays);
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
