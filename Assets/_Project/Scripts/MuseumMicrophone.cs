using System;
using UnityEngine;
using TMPro;

namespace MicrophoneInput
{
    public class MuseumMicrophone : MonoBehaviour
    {
        // ── Data types ────────────────────────────────────────────────────────────

        [Serializable]
        public class SubDisplay
        {
            public string displayName;
            [Tooltip("Where the NPC walks to and stands while talking about this display.")]
            public Transform spot;
            [Tooltip("Direction the NPC faces by default at this display. Leave null to keep arrival direction.")]
            public Transform defaultLookAt;
            public AudioClip[] voiceLines;
            [TextArea(2, 4)] public string[] subtitles;
            public float[] transitionDelays;
        }

        [Serializable]
        public class MuseumAct
        {
            public string actName;
            [Tooltip("Forcefield activated when this act starts, deactivated when the next begins.")]
            public GameObject forcefield;
            [Min(0f)] public float actStartDelay = 0f;
            public SubDisplay[] subDisplays;

            [Header("Quiz (shown after the last sub-display)")]
            public bool hasQuiz;
            public QuizData quiz;
        }

        [Serializable]
        public class QuizData
        {
            [TextArea(1, 2)] public string questionText;
            public string option1;
            public string option2;
            public string option3;
            [Tooltip("0 = option1, 1 = option2, 2 = option3")]
            [Range(0, 2)] public int correctOptionIndex;

            [Header("NPC Quiz Intro Line (plays before the quiz appears)")]
            public AudioClip quizIntroClip;
            [TextArea(1, 3)] public string quizIntroSubtitle;

            [Header("NPC Response Lines")]
            public AudioClip correctClip;
            [TextArea(1, 3)] public string correctSubtitle;
            public AudioClip wrongClip;
            [TextArea(1, 3)] public string wrongSubtitle;
        }

        // ── Singleton ─────────────────────────────────────────────────────────────

        public static MuseumMicrophone Instance;

        // ── Tour completion flag ──────────────────────────────────────────────────

        /// <summary>Set to true when the guide finishes the last sub-display of the last act.</summary>
        public static bool TourComplete { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Dialogue Output")]
        public MicrophoneInputSystem micInputSystemA;
        public Transform mouthObjectA;
        public TMP_Text subtitleTextTarget;
        public bool audiblePlayback = true;
        [Tooltip("Enable 3D spatial audio (volume drops with distance). Disable for 2D audio heard at full volume regardless of position.")]
        public bool useSpatialAudio = true;

        [Header("Spatial Audio")]
        [Range(0.1f, 3f)] public float spatialVoiceVolume = 1f;
        [Range(0.1f, 50f)] public float spatialVoiceMinDistance = 3f;
        [Range(1f, 100f)] public float spatialVoiceMaxDistance = 20f;

        [Header("Intro Greeting")]
        [Tooltip("The NPC walks to this object before saying intro lines.")]
        public GameObject introWalkTarget;
        public AudioClip[] introVoiceLines;
        [TextArea(2, 4)] public string[] introSubtitles;
        public float[] introTransitionDelays;

        [Header("Museum Acts")]
        [SerializeField] private MuseumAct[] acts;
        [SerializeField] private bool clearSubtitleWhenSequenceEnds = true;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private AudioClip[] activeVoiceLines;
        private string[] activeSubtitles;
        private float[] activeTransitionDelays;
        private bool isDialoguePlaying;
        private MicrophoneInputSystem playbackSystem;

        private int _currentActIndex = -1;
        private bool _isLastSubDisplayOfAct;
        private bool _respondingToQuiz;
        private bool _playingQuizIntro;

        // ── Public API ────────────────────────────────────────────────────────────

        public bool IsDialoguePlaying => isDialoguePlaying;
        public int VoiceLineCount => activeVoiceLines != null ? activeVoiceLines.Length : 0;
        public int ActCount => acts != null ? acts.Length : 0;

        public bool HasAct(int actIndex) =>
            acts != null && actIndex >= 0 && actIndex < acts.Length && acts[actIndex] != null;

        public int GetSubDisplayCount(int actIndex)
        {
            if (!HasAct(actIndex) || acts[actIndex].subDisplays == null) return 0;
            return acts[actIndex].subDisplays.Length;
        }

        public Transform GetSubDisplaySpot(int actIndex, int subIndex)
        {
            if (!HasAct(actIndex)) return null;
            var subs = acts[actIndex].subDisplays;
            if (subs == null || subIndex < 0 || subIndex >= subs.Length) return null;
            return subs[subIndex].spot;
        }

        public GameObject GetIntroWalkTarget() => introWalkTarget;

        public float GetActStartDelay(int actIndex) =>
            HasAct(actIndex) ? Mathf.Max(0f, acts[actIndex].actStartDelay) : 0f;

        public GameObject GetActForcefield(int actIndex) =>
            HasAct(actIndex) ? acts[actIndex].forcefield : null;

        public MuseumAct GetAct(int actIndex) =>
            HasAct(actIndex) ? acts[actIndex] : null;

        public Transform GetSubDisplayDefaultLookAt(int actIndex, int subIndex)
        {
            if (!HasAct(actIndex)) return null;
            var subs = acts[actIndex].subDisplays;
            if (subs == null || subIndex < 0 || subIndex >= subs.Length) return null;
            return subs[subIndex].defaultLookAt;
        }

        /// <summary>Plays the NPC's intro greeting lines.</summary>
        public bool PlayIntroLines()
        {
            if (introVoiceLines == null || introVoiceLines.Length == 0) return false;
            _currentActIndex = -1;
            _isLastSubDisplayOfAct = false;
            return PlayLines(introVoiceLines, introSubtitles, introTransitionDelays);
        }

        /// <summary>Plays the voice lines for a specific sub-display of an act.</summary>
        public bool PlaySubDisplayDialogue(int actIndex, int subIndex)
        {
            if (!HasAct(actIndex)) return false;
            var subs = acts[actIndex].subDisplays;
            if (subs == null || subIndex < 0 || subIndex >= subs.Length) return false;
            var sub = subs[subIndex];
            if (sub.voiceLines == null || sub.voiceLines.Length == 0) return false;

            _currentActIndex = actIndex;
            _isLastSubDisplayOfAct = subIndex == subs.Length - 1;
            return PlayLines(sub.voiceLines, sub.subtitles, sub.transitionDelays);
        }

        // ── MicrophoneInputSystem data accessors ──────────────────────────────────

        public AudioClip GetVoiceLine(int index)
        {
            if (activeVoiceLines == null || activeVoiceLines.Length == 0) return null;
            return activeVoiceLines[Mathf.Clamp(index, 0, activeVoiceLines.Length - 1)];
        }

        public bool ShouldMoveMouth(int index) => true;

        public string GetSubtitle(int index)
        {
            if (activeSubtitles == null || activeSubtitles.Length == 0) return string.Empty;
            return activeSubtitles[Mathf.Clamp(index, 0, activeSubtitles.Length - 1)];
        }

        public float GetTransitionDelay(int index)
        {
            if (activeTransitionDelays == null || activeTransitionDelays.Length == 0) return 0f;
            return Mathf.Max(0f, activeTransitionDelays[Mathf.Clamp(index, 0, activeTransitionDelays.Length - 1)]);
        }

        /// <summary>Called by MicrophoneInputSystem when the last voice line of a sequence finishes.</summary>
        public void BeginActTransition()
        {
            if (clearSubtitleWhenSequenceEnds)
                ClearSubtitleText();

            // Check quiz intro FIRST — PlayResponseLine sets _respondingToQuiz,
            // so _playingQuizIntro must win the priority check.
            if (_playingQuizIntro)
            {
                _playingQuizIntro = false;
                _respondingToQuiz = false;
                ShowQuizUI();
                return;
            }

            if (_respondingToQuiz)
            {
                _respondingToQuiz = false;
                isDialoguePlaying = false;
                CheckTourComplete();
                return;
            }

            // If this was the last sub-display of an act with a quiz, play the intro line
            // (if one is set) then show the quiz. Find QuizUI even if inactive.
            if (MuseumQuizUI.Instance == null)
                MuseumQuizUI.Instance = FindFirstObjectByType<MuseumQuizUI>(FindObjectsInactive.Include);

            if (_isLastSubDisplayOfAct &&
                _currentActIndex >= 0 &&
                HasAct(_currentActIndex) &&
                acts[_currentActIndex].hasQuiz &&
                acts[_currentActIndex].quiz != null &&
                MuseumQuizUI.Instance != null)
            {
                var quiz = acts[_currentActIndex].quiz;
                if (quiz.quizIntroClip != null)
                {
                    _playingQuizIntro = true;
                    PlayResponseLine(quiz.quizIntroClip, quiz.quizIntroSubtitle);
                }
                else
                {
                    ShowQuizUI();
                }
                return;
            }

            isDialoguePlaying = false;
            CheckTourComplete();
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            ResolvePlaybackSystem();
        }

        private void Start()
        {
            if (playbackSystem != null && mouthObjectA != null)
                playbackSystem.targetObject = mouthObjectA;
            ClearSubtitleText();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ShowQuizUI()
        {
            MuseumQuizUI.Instance.ShowQuiz(acts[_currentActIndex].quiz, OnQuizAnswered);
        }

        private void CheckTourComplete()
        {
            if (_isLastSubDisplayOfAct && _currentActIndex == ActCount - 1)
                TourComplete = true;
        }

        private void OnQuizAnswered(bool correct)
        {
            if (_currentActIndex < 0 || !HasAct(_currentActIndex))
            {
                isDialoguePlaying = false;
                return;
            }

            var quiz = acts[_currentActIndex].quiz;
            AudioClip clip = correct ? quiz.correctClip : quiz.wrongClip;
            string sub = correct ? quiz.correctSubtitle : quiz.wrongSubtitle;

            if (clip != null)
                PlayResponseLine(clip, sub);
            else
            {
                isDialoguePlaying = false;
                CheckTourComplete();
            }
        }

        private void PlayResponseLine(AudioClip clip, string subtitle)
        {
            activeVoiceLines = new AudioClip[] { clip };
            activeSubtitles = new string[] { subtitle ?? string.Empty };
            activeTransitionDelays = new float[] { 0f };
            _respondingToQuiz = true;
            isDialoguePlaying = true;

            ResolvePlaybackSystem();
            if (playbackSystem == null) { _respondingToQuiz = false; isDialoguePlaying = false; return; }
            playbackSystem.SetLine(0);
        }

        private bool PlayLines(AudioClip[] clips, string[] subtitles, float[] delays)
        {
            ResolvePlaybackSystem();
            if (playbackSystem == null)
            {
                Debug.LogWarning("MuseumMicrophone: No MicrophoneInputSystem assigned for playback.");
                return false;
            }

            int count = clips.Length;
            activeVoiceLines = clips;
            activeSubtitles = subtitles != null ? subtitles : Array.Empty<string>();
            activeTransitionDelays = BuildFloatArray(delays, count, 0f);

            if (activeSubtitles.Length < count)
            {
                string[] padded = new string[count];
                for (int i = 0; i < count; i++)
                    padded[i] = i < activeSubtitles.Length ? activeSubtitles[i] : string.Empty;
                activeSubtitles = padded;
            }

            isDialoguePlaying = true;
            playbackSystem.SetLine(0);
            return true;
        }

        private void ResolvePlaybackSystem()
        {
            if (micInputSystemA == null)
                micInputSystemA = GetComponent<MicrophoneInputSystem>() ?? FindFirstObjectByType<MicrophoneInputSystem>();
            playbackSystem = micInputSystemA;
            if (playbackSystem != null)
            {
                playbackSystem.playOnStart = false;
                playbackSystem.autoAdvance = true;
                if (mouthObjectA != null)
                    playbackSystem.targetObject = mouthObjectA;
            }
        }

        private void ClearSubtitleText()
        {
            if (subtitleTextTarget != null)
                subtitleTextTarget.text = string.Empty;
        }

        private static float[] BuildFloatArray(float[] source, int length, float defaultValue)
        {
            if (length <= 0) return Array.Empty<float>();
            float[] result = new float[length];
            for (int i = 0; i < length; i++)
                result[i] = source != null && i < source.Length ? Mathf.Max(0f, source[i]) : defaultValue;
            return result;
        }
    }
}
