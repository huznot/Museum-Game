using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace MicrophoneInput
{
    public class MicrophoneManager : MonoBehaviour
    {
        public static MicrophoneManager Instance;
        [Header("Voice Clip Source")]
        [Tooltip("Audio clip (e.g., imported MP3) used instead of a live microphone.")]
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private bool loopClip = true;

        [Header("Mouth Swap")]
        [Tooltip("MicrophoneInputSystem for the first character.")]
        public MicrophoneInputSystem micInputSystemA;
        [Tooltip("MicrophoneInputSystem for the second character.")]
        public MicrophoneInputSystem micInputSystemB;
        [Tooltip("If true, dialogue playback stays on a single system and only the mouth target swaps.")]
        public bool useSingleDialoguePlayer = true;
        [Tooltip("Current/default mouth object (first character).")]
        public Transform mouthObjectA;
        [Tooltip("Second mouth object (for the new character).")]
        public Transform mouthObjectB;
        [Tooltip("Object that becomes active when the new character spawns.")]
        public GameObject spawnObject;
        [Tooltip("Key to switch to the second mouth when spawn is active.")]
        public KeyCode swapKey = KeyCode.E;

        [Header("Act 3 Gate + Arm")]
        [Tooltip("Index to pause AFTER (0-based). Gate blocks next line.")]
        public int act3GateHoldAfterIndex = 21;
        [Tooltip("Index of the gated line that resumes after trigger (0-based).")]
        public int act3GateLineIndex = 22;
        [Tooltip("Index to pause AFTER (0-based) until lever pull.")]
        public int act3LeverGateHoldAfterIndex = 28;
        [Tooltip("Index of the lever-gated line that resumes after trigger (0-based).")]
        public int act3LeverGateLineIndex = 29;
        [Tooltip("Subtitle index to trigger Act 3 music (0-based).")]
        public int act3MusicTriggerIndex = 48;
        [Tooltip("Delay used to hold at the gate index until trigger is hit.")]
        public float act3GateHoldDelaySeconds = 9999f;
        [Tooltip("Delay used to hold at the lever gate index until lever is pulled.")]
        public float act3LeverGateHoldDelaySeconds = 9999f;
        [Tooltip("Arm transform to animate during the gated line.")]
        public Transform act3Arm;
        public Vector3 act3ArmRaisedEuler = new Vector3(48.7684441f, 72.762558f, 18.7180843f);
        [Tooltip("How long to keep the arm raised after the trigger fires.")]
        public float act3ArmRaiseDuration = 1.5f;
        [Tooltip("Optional explicit lowered rotation. If false, uses the arm's initial local rotation.")]
        public bool useAct3ArmLoweredEuler = false;
        public Vector3 act3ArmLoweredEuler = Vector3.zero;
        public float act3ArmRotateSpeed = 5f;

        [Header("Lower Arm Talk Motion")]
        [Tooltip("Lower arm transform to animate while mouth A is moving.")]
        public Transform lowerArm;
        public Vector3 lowerArmTalkEuler = new Vector3(55.2605629f, 330.796387f, 78.1034241f);
        public Vector3 lowerArmRestEuler = new Vector3(17.4775105f, 339.959503f, 43.5801315f);
        public float lowerArmRotateSpeed = 8f;
        [Tooltip("Extra subtle motion while talking.")]
        public Vector3 lowerArmTalkWiggleEuler = new Vector3(1.5f, 1.0f, 1.8f);
        public float lowerArmTalkWiggleSpeed = 3.0f;

        [Header("Dialogue Data")]
        [Tooltip("All voicelines for the dialogue system (MP3/OGG/WAV).")]
        public AudioClip[] voiceLines;
        [Tooltip("If false for a line, mouth animation is suppressed while that clip plays.")]
        public bool[] mouthShouldMove;
        [Tooltip("Subtitle text shown while the matching voiceline plays.")]
        public string[] subtitles;
        [Tooltip("Seconds to wait before transitioning from this line to the next (per index).")]
        public float[] transitionDelays;
        [Tooltip("TMP text object that will receive subtitles (optional).")]
        public TMPro.TMP_Text subtitleTextTarget;
        [Tooltip("Play audio audibly (true) or muted (false) while still driving animations.")]
        public bool audiblePlayback = true;

        [Header("Lifetime")]
        [Tooltip("If true, this manager survives scene loads.")]
        public bool persistAcrossScenes = false;

        [Header("Act Transitions")]
        public Act currentAct = Act.Act1;
        [Tooltip("Seconds to wait after the final line before fading out.")]
        public float act1DelayAfterLastLine = 2f;
        [Tooltip("Seconds to wait after the final line before fading out.")]
        public float act2DelayAfterLastLine = 2f;
        [Tooltip("Seconds to wait after the final line before fading out.")]
        public float act3DelayAfterLastLine = 2f;
        [Tooltip("Scene to load when Act 1 finishes.")]
        public string act1NextScene = "Flashback";
        [Tooltip("Scene to load when Act 2 finishes.")]
        public string act2NextScene = "Act2";
        [Tooltip("Scene to load when Act 3 finishes.")]
        public string act3NextScene = "";
        [Tooltip("UI Image used to fade (alpha) to black.")]
        public Image fadeImage;
        [Tooltip("Seconds to hold on black before loading the next scene.")]
        public float blackHoldTime = 1f;
        [Tooltip("Seconds to fade to black when using Fade mode.")]
        public float fadeToBlackTime = 0.4f;
        public BlackScreenMode blackScreenMode = BlackScreenMode.Snap;

        [Header("Act 3 End Override")]
        [Tooltip("If true, Act 3 uses a hard cut (no fade/black) to the end scene.")]
        public bool act3UseHardCut = true;
        [Tooltip("Scene to load after Act 3 ends when hard cut is enabled.")]
        public string act3EndScene = "Credits";
        [Tooltip("Seconds to wait after the last Act 3 line before hard cut.")]
        public float act3EndDelaySeconds = 3f;

        [Header("Scene Audio Fade")]
        [Tooltip("Fade all scene AudioSources out before loading, and in on scene start.")]
        public bool fadeSceneAudio = true;
        [Tooltip("Seconds to fade out all scene audio before loading next scene.")]
        public float sceneAudioFadeOutTime = 0.8f;
        [Tooltip("Seconds to fade in all scene audio on scene start.")]
        public float sceneAudioFadeInTime = 0.8f;

        [Header("Background Music")]
        [Tooltip("Optional background music for this scene.")]
        public AudioClip backgroundMusicClip;
        [Range(0f, 1f)] public float backgroundMusicVolume = 0.7f;
        public bool playBackgroundMusicOnStart = true;
        public bool loopBackgroundMusic = true;

        [Header("Act 3 Music")]
        [Tooltip("Music clip to start after the trigger subtitle index in Act 3.")]
        public AudioClip act3MusicClip;
        [Range(0f, 1f)] public float act3MusicVolume = 0.8f;
        public bool act3MusicLoop = true;
        [Tooltip("If true, Act 3 music persists into the next scene.")]
        public bool act3MusicPersistAcrossScenes = true;

        [Header("Dialogue Spatial Audio")]
        [Tooltip("3D voice volume multiplier for spatialized lines (e.g., Tariq).")]
        [Range(0.1f, 3f)] public float spatialVoiceVolume = 1f;
        [Tooltip("3D voice min distance (closer = louder).")]
        [Range(0.1f, 50f)] public float spatialVoiceMinDistance = 5f;
        [Tooltip("3D voice max distance (farther = quieter).")]
        [Range(1f, 200f)] public float spatialVoiceMaxDistance = 40f;

        public AudioClip MicClip => voiceClip;
        public bool LoopClip => loopClip;
        public int VoiceLineCount => voiceLines != null && voiceLines.Length > 0 ? voiceLines.Length : (voiceClip != null ? 1 : 0);

        private bool isTransitioning;
        private AudioSource backgroundMusicSource;
        private bool usingSecondMouth;
        private AudioSource act3AudioSource;
        private bool act3GateWaiting;
        private bool act3GateReady;
        private bool act3LeverGateWaiting;
        private bool act3LeverGateReady;
        private string lastNonEmptySubtitle;
        private Quaternion act3ArmBaseRotation;
        private bool act3ArmBaseCached;
        private bool act3ArmTriggered;
        private float act3ArmTimer;
        private bool act3MusicStarted;
        private AudioSource act3MusicSource;
        private AudioSource micASource;
        private Quaternion lowerArmBaseRotation;
        private bool lowerArmBaseCached;
        private bool outOfFuelMusicCueTriggered;
        private MicrophoneInputSystem playbackSystem;
        private MicrophoneInputSystem dialogueController;

        public bool OutOfFuelMusicCueTriggered => outOfFuelMusicCueTriggered;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            if (voiceClip == null)
                Debug.LogWarning("No voice clip assigned on MicrophoneManager. Assign an MP3/AudioClip in the Inspector.");

            InitializeDialogueController();
        }

        void Start()
        {
            if (playBackgroundMusicOnStart && backgroundMusicClip != null)
                EnsureBackgroundMusic();

            if (fadeSceneAudio && sceneAudioFadeInTime > 0f)
                StartCoroutine(FadeInAllSceneAudio());

            // Default mouth target
            if (playbackSystem == micInputSystemB)
                ApplyMouthTarget(playbackSystem, mouthObjectB != null ? mouthObjectB : mouthObjectA, micInputSystemB);
            else
                ApplyMouthTarget(playbackSystem, mouthObjectA, micInputSystemA);

            if (currentAct == Act.Act3)
            {
                ResolveAct3GateIndices();
                ResolveAct3LeverGateIndices();
                EnsureAct3GateDelay();
                EnsureAct3LeverGateDelay();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!usingSecondMouth &&
                spawnObject != null &&
                spawnObject.activeInHierarchy &&
                Input.GetKeyDown(swapKey))
            {
                usingSecondMouth = true;
                if (useSingleDialoguePlayer)
                {
                    if (playbackSystem != null)
                        ApplyMouthTarget(playbackSystem, mouthObjectB, micInputSystemB);
                }
                else
                {
                    MicrophoneInputSystem previousSystem = GetAct3MicSystem();
                    ApplyMouthTarget(micInputSystemB, mouthObjectB, micInputSystemB);
                    if (micInputSystemB != null && previousSystem != null && micInputSystemB != previousSystem)
                    {
                        micInputSystemB.SetPlaybackSuppressed(false);
                        bool wasPlaying = previousSystem.SuspendPlayback();
                        micInputSystemB.TransferPlaybackFrom(previousSystem, wasPlaying);
                        previousSystem.SetPlaybackSuppressed(true);
                        playbackSystem = micInputSystemB;
                    }
                }
            }

            UpdateAct3GateAndArm();
            UpdateLowerArmTalkMotion();
            UpdateVisualMouthsFromPlayback();
        }

        public void SetVoiceClip(AudioClip clip, bool loop = true)
        {
            voiceClip = clip;
            loopClip = loop;
        }

        public AudioClip GetVoiceLine(int index)
        {
            if (voiceLines != null && voiceLines.Length > 0)
            {
                int clamped = Mathf.Clamp(index, 0, voiceLines.Length - 1);
                return voiceLines[clamped];
            }
            return voiceClip;
        }

        public bool ShouldMoveMouth(int index)
        {
            if (mouthShouldMove != null && mouthShouldMove.Length > 0)
            {
                int clamped = Mathf.Clamp(index, 0, mouthShouldMove.Length - 1);
                return mouthShouldMove[clamped];
            }
            return true;
        }

        public string GetSubtitle(int index)
        {
            if (subtitles != null && subtitles.Length > 0)
            {
                int clamped = Mathf.Clamp(index, 0, subtitles.Length - 1);
                return subtitles[clamped];
            }
            return string.Empty;
        }

        public float GetTransitionDelay(int index)
        {
            if (transitionDelays != null && transitionDelays.Length > 0)
            {
                int clamped = Mathf.Clamp(index, 0, transitionDelays.Length - 1);
                return Mathf.Max(0f, transitionDelays[clamped]);
            }
            return 0f;
        }

        public void BeginActTransition()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            StartCoroutine(HandleActTransition());
        }

        private void ApplyMouthTarget(MicrophoneInputSystem system, Transform target, MicrophoneInputSystem visualSource = null)
        {
            if (system == null) return;
            if (target == null) return;
            system.targetObject = target;

            if (visualSource != null)
            {
                system.voiceTargetMode = visualSource.voiceTargetMode;
                system.sensitivity = visualSource.sensitivity;
                system.smoothSpeed = visualSource.smoothSpeed;
                system.highFrequencyMultiplier = visualSource.highFrequencyMultiplier;
                system.angleAxis = visualSource.angleAxis;
                system.minAngle = visualSource.minAngle;
                system.maxAngle = visualSource.maxAngle;
                system.positionAxis = visualSource.positionAxis;
                system.minPosition = visualSource.minPosition;
                system.maxPosition = visualSource.maxPosition;
                system.blendShapeName = visualSource.blendShapeName;
                system.skinnedMesh = visualSource.skinnedMesh;
                system.textTarget = visualSource.textTarget;
                system.spriteRenderer = visualSource.spriteRenderer;
                system.mouthSprites = visualSource.mouthSprites;
                system.spriteSwapThreshold = visualSource.spriteSwapThreshold;
                system.spriteSwapHysteresis = visualSource.spriteSwapHysteresis;
                system.spriteBaseOpenLimit = visualSource.spriteBaseOpenLimit;
                system.spriteTransientBoost = visualSource.spriteTransientBoost;
                system.spriteToneVariation = visualSource.spriteToneVariation;
                system.volumeAttack = visualSource.volumeAttack;
                system.volumeRelease = visualSource.volumeRelease;
                system.adaptiveNormalizationSpeed = visualSource.adaptiveNormalizationSpeed;
                system.normalizedVolumeCurve = visualSource.normalizedVolumeCurve;
                system.peakBlend = visualSource.peakBlend;
                system.noiseFloorTrackSpeed = visualSource.noiseFloorTrackSpeed;
                system.noiseGateStrength = visualSource.noiseGateStrength;
                system.dynamicRangeCompression = visualSource.dynamicRangeCompression;
                system.speechOpenThreshold = visualSource.speechOpenThreshold;
                system.speechCloseThreshold = visualSource.speechCloseThreshold;
            }

            if (system.voiceTargetMode == MicrophoneInputSystem.VoiceTargetMode.BlendShape)
            {
                SkinnedMeshRenderer mesh = system.skinnedMesh;
                if (mesh == null)
                    mesh = target.GetComponentInChildren<SkinnedMeshRenderer>();
                if (mesh == null) mesh = target.GetComponent<SkinnedMeshRenderer>();
                if (mesh != null) system.skinnedMesh = mesh;
            }

            if (system.voiceTargetMode == MicrophoneInputSystem.VoiceTargetMode.SpriteSwap)
            {
                SpriteRenderer sprite = system.spriteRenderer;
                if (sprite == null)
                    sprite = target.GetComponentInChildren<SpriteRenderer>();
                if (sprite == null) sprite = target.GetComponent<SpriteRenderer>();
                if (sprite != null) system.spriteRenderer = sprite;
            }
        }

        private void UpdateVisualMouthsFromPlayback()
        {
            if (!useSingleDialoguePlayer) return;
            if (playbackSystem == null) return;

            float volume = playbackSystem.GetCurrentVolume();
            int lineIndex = playbackSystem.GetCurrentLineIndex();
            bool shouldMove = ShouldMoveMouth(lineIndex);
            float driveVolume = shouldMove ? volume : 0f;

            if (usingSecondMouth)
            {
                if (micInputSystemB != null)
                    micInputSystemB.SetExternalVolume(driveVolume);
                if (micInputSystemA != null)
                    micInputSystemA.SetExternalVolume(0f);
            }
            else
            {
                if (micInputSystemA != null)
                    micInputSystemA.SetExternalVolume(driveVolume);
                if (micInputSystemB != null)
                    micInputSystemB.SetExternalVolume(0f);
            }
        }

        private void InitializeDialogueController()
        {
            ResolveMicSystemsIfMissing();

            if (!useSingleDialoguePlayer)
            {
                playbackSystem = micInputSystemA != null ? micInputSystemA : micInputSystemB;
                if (micInputSystemA != null)
                    micInputSystemA.SetPlaybackSuppressed(playbackSystem != micInputSystemA);
                if (micInputSystemB != null)
                    micInputSystemB.SetPlaybackSuppressed(playbackSystem != micInputSystemB);
                return;
            }

            MicrophoneInputSystem source = micInputSystemA != null ? micInputSystemA : micInputSystemB;
            if (source == null)
            {
                Debug.LogWarning("MicrophoneManager: No MicrophoneInputSystem found. Assign micInputSystemA/B in the inspector.");
                playbackSystem = null;
                return;
            }

            dialogueController = GetComponent<MicrophoneInputSystem>();
            if (dialogueController == null)
                dialogueController = gameObject.AddComponent<MicrophoneInputSystem>();

            CopyDialogueSettings(source, dialogueController);
            dialogueController.SetPlaybackSuppressed(false);

            if (micInputSystemA != null && micInputSystemA != dialogueController)
                micInputSystemA.SetPlaybackSuppressed(true);
            if (micInputSystemB != null && micInputSystemB != dialogueController)
                micInputSystemB.SetPlaybackSuppressed(true);

            playbackSystem = dialogueController;
        }

        private void ResolveMicSystemsIfMissing()
        {
            if (micInputSystemA == null || micInputSystemB == null)
            {
                MicrophoneInputSystem[] systems = FindObjectsByType<MicrophoneInputSystem>(FindObjectsSortMode.None);
                if (systems != null && systems.Length > 0)
                {
                    if (micInputSystemA == null) micInputSystemA = systems[0];
                    if (micInputSystemB == null && systems.Length > 1) micInputSystemB = systems[1];
                }
            }
        }

        private void CopyDialogueSettings(MicrophoneInputSystem source, MicrophoneInputSystem target)
        {
            if (source == null || target == null) return;

            target.voiceTargetMode = source.voiceTargetMode;
            target.sensitivity = source.sensitivity;
            target.smoothSpeed = source.smoothSpeed;
            target.highFrequencyMultiplier = source.highFrequencyMultiplier;
            target.angleAxis = source.angleAxis;
            target.minAngle = source.minAngle;
            target.maxAngle = source.maxAngle;
            target.positionAxis = source.positionAxis;
            target.minPosition = source.minPosition;
            target.maxPosition = source.maxPosition;
            target.blendShapeName = source.blendShapeName;
            target.skinnedMesh = source.skinnedMesh;
            target.textTarget = source.textTarget;
            target.spriteRenderer = source.spriteRenderer;
            target.mouthSprites = source.mouthSprites;
            target.spriteSwapThreshold = source.spriteSwapThreshold;
            target.spriteSwapHysteresis = source.spriteSwapHysteresis;
            target.spriteBaseOpenLimit = source.spriteBaseOpenLimit;
            target.spriteTransientBoost = source.spriteTransientBoost;
            target.spriteToneVariation = source.spriteToneVariation;
            target.volumeAttack = source.volumeAttack;
            target.volumeRelease = source.volumeRelease;
            target.adaptiveNormalizationSpeed = source.adaptiveNormalizationSpeed;
            target.normalizedVolumeCurve = source.normalizedVolumeCurve;
            target.peakBlend = source.peakBlend;
            target.noiseFloorTrackSpeed = source.noiseFloorTrackSpeed;
            target.noiseGateStrength = source.noiseGateStrength;
            target.dynamicRangeCompression = source.dynamicRangeCompression;
            target.speechOpenThreshold = source.speechOpenThreshold;
            target.speechCloseThreshold = source.speechCloseThreshold;
            target.subtitleTextTarget = source.subtitleTextTarget;
            target.playOnStart = source.playOnStart;
            target.autoAdvance = source.autoAdvance;
        }

        private void EnsureAct3GateDelay()
        {
            if (transitionDelays == null || transitionDelays.Length == 0) return;
            if (act3GateHoldAfterIndex < 0 || act3GateHoldAfterIndex >= transitionDelays.Length) return;
            transitionDelays[act3GateHoldAfterIndex] = Mathf.Max(0.1f, act3GateHoldDelaySeconds);
        }

        private void EnsureAct3LeverGateDelay()
        {
            if (transitionDelays == null || transitionDelays.Length == 0) return;
            if (act3LeverGateHoldAfterIndex < 0 || act3LeverGateHoldAfterIndex >= transitionDelays.Length) return;
            transitionDelays[act3LeverGateHoldAfterIndex] = Mathf.Max(0.1f, act3LeverGateHoldDelaySeconds);
        }

        private void ResolveAct3GateIndices()
        {
            if (subtitles == null || subtitles.Length == 0) return;

            int targetIndex = FindSubtitleIndexContaining("bring light to a village");
            if (targetIndex >= 0)
            {
                act3GateHoldAfterIndex = targetIndex;
                act3GateLineIndex = Mathf.Clamp(targetIndex + 1, 0, subtitles.Length - 1);
            }
        }

        private void ResolveAct3LeverGateIndices()
        {
            if (subtitles == null || subtitles.Length == 0) return;

            int targetIndex = FindSubtitleIndexContaining("two sides of the same coin");
            if (targetIndex >= 0)
            {
                act3LeverGateHoldAfterIndex = targetIndex;
                act3LeverGateLineIndex = Mathf.Clamp(targetIndex + 1, 0, subtitles.Length - 1);
            }
        }

        private int FindSubtitleIndexContaining(string fragment)
        {
            if (string.IsNullOrEmpty(fragment)) return -1;
            for (int i = 0; i < subtitles.Length; i++)
            {
                string line = subtitles[i];
                if (string.IsNullOrEmpty(line)) continue;
                if (line.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
            return -1;
        }

        private void UpdateAct3GateAndArm()
        {
            if (currentAct != Act.Act3) return;
            MicrophoneInputSystem act3MicSystem = GetAct3MicSystem();
            if (act3MicSystem == null) return;

            if (act3AudioSource == null)
                act3AudioSource = act3MicSystem.GetComponent<AudioSource>();

            string currentSubtitle = GetCurrentSubtitleText();
            if (!string.IsNullOrEmpty(currentSubtitle))
                lastNonEmptySubtitle = currentSubtitle;

            if (!outOfFuelMusicCueTriggered && IsOutOfFuelMusicCueSubtitle(lastNonEmptySubtitle))
                outOfFuelMusicCueTriggered = true;

            if (!act3GateWaiting && IsSubtitleIndex(act3GateHoldAfterIndex))
                act3GateWaiting = true;

            if (CarTriggerEnabler.Act3DialogueGate)
                act3GateReady = true;

            if (act3GateWaiting && act3GateReady && act3AudioSource != null && !act3AudioSource.isPlaying)
            {
                act3MicSystem.NextLine();
                act3GateWaiting = false;
            }

            if (!act3LeverGateWaiting && IsSubtitleIndex(act3LeverGateHoldAfterIndex))
                act3LeverGateWaiting = true;

            if (LeverPullIK.Act3LeverPulled)
                act3LeverGateReady = true;

            if (act3LeverGateWaiting && act3LeverGateReady && act3AudioSource != null && !act3AudioSource.isPlaying)
            {
                act3MicSystem.NextLine();
                act3LeverGateWaiting = false;
            }

            if (CarTriggerEnabler.Act3DialogueGate && !act3ArmTriggered)
            {
                act3ArmTriggered = true;
                act3ArmTimer = Mathf.Max(0f, act3ArmRaiseDuration);
            }

            if (!act3MusicStarted && IsSubtitleIndex(act3MusicTriggerIndex))
            {
                StartAct3Music();
            }

            UpdateAct3ArmPose();
        }

        private string GetCurrentSubtitleText()
        {
            if (subtitleTextTarget != null) return subtitleTextTarget.text;
            MicrophoneInputSystem act3MicSystem = GetAct3MicSystem();
            if (act3MicSystem != null && act3MicSystem.subtitleTextTarget != null)
                return act3MicSystem.subtitleTextTarget.text;
            return string.Empty;
        }

        private bool IsSubtitleIndex(int index)
        {
            if (subtitles == null || subtitles.Length == 0) return false;
            if (index < 0 || index >= subtitles.Length) return false;
            return lastNonEmptySubtitle == subtitles[index];
        }

        private int GetCurrentSubtitleIndex()
        {
            if (subtitles == null || subtitles.Length == 0) return -1;
            for (int i = 0; i < subtitles.Length; i++)
            {
                if (subtitles[i] == lastNonEmptySubtitle)
                    return i;
            }
            return -1;
        }

        private bool IsOutOfFuelMusicCueSubtitle(string subtitle)
        {
            if (string.IsNullOrEmpty(subtitle)) return false;

            return subtitle.IndexOf("Anyways, Ford", System.StringComparison.OrdinalIgnoreCase) >= 0
                || subtitle.IndexOf("bring light to a village", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateAct3ArmPose()
        {
            if (act3Arm == null) return;

            if (!act3ArmBaseCached)
            {
                act3ArmBaseRotation = act3Arm.localRotation;
                act3ArmBaseCached = true;
            }

            if (act3ArmTimer > 0f)
                act3ArmTimer -= Time.deltaTime;

            bool shouldRaise = act3ArmTimer > 0f;

            Quaternion target = shouldRaise
                ? Quaternion.Euler(act3ArmRaisedEuler)
                : (useAct3ArmLoweredEuler ? Quaternion.Euler(act3ArmLoweredEuler) : act3ArmBaseRotation);

            act3Arm.localRotation = Quaternion.Lerp(
                act3Arm.localRotation,
                target,
                Time.deltaTime * Mathf.Max(0.01f, act3ArmRotateSpeed)
            );

        }

        private void UpdateLowerArmTalkMotion()
        {
            if (lowerArm == null) return;

            if (!lowerArmBaseCached)
            {
                lowerArmBaseRotation = lowerArm.localRotation;
                lowerArmBaseCached = true;
            }

            string currentSubtitle = GetCurrentSubtitleText();
            if (!string.IsNullOrEmpty(currentSubtitle))
                lastNonEmptySubtitle = currentSubtitle;

            bool mouthAMoving = !usingSecondMouth;
            if (mouthAMoving)
            {
                AudioSource activeVoiceSource = null;
                if (playbackSystem != null)
                    activeVoiceSource = playbackSystem.GetComponent<AudioSource>();

                if (activeVoiceSource == null)
                {
                    if (micASource == null && micInputSystemA != null)
                        micASource = micInputSystemA.GetComponent<AudioSource>();
                    activeVoiceSource = micASource;
                }

                int idx = GetCurrentSubtitleIndex();
                if (idx >= 0 && mouthShouldMove != null && mouthShouldMove.Length > 0)
                    mouthAMoving = mouthShouldMove[Mathf.Clamp(idx, 0, mouthShouldMove.Length - 1)];

                if (activeVoiceSource != null && !activeVoiceSource.isPlaying)
                    mouthAMoving = false;
            }

            Quaternion target = mouthAMoving
                ? Quaternion.Euler(lowerArmTalkEuler)
                : Quaternion.Euler(lowerArmRestEuler);

            if (mouthAMoving)
            {
                Vector3 wiggle = new Vector3(
                    Mathf.Sin(Time.time * lowerArmTalkWiggleSpeed) * lowerArmTalkWiggleEuler.x,
                    Mathf.Sin(Time.time * (lowerArmTalkWiggleSpeed * 1.3f)) * lowerArmTalkWiggleEuler.y,
                    Mathf.Sin(Time.time * (lowerArmTalkWiggleSpeed * 1.7f)) * lowerArmTalkWiggleEuler.z
                );
                target = target * Quaternion.Euler(wiggle);
            }

            lowerArm.localRotation = Quaternion.Lerp(
                lowerArm.localRotation,
                target,
                Time.deltaTime * Mathf.Max(0.01f, lowerArmRotateSpeed)
            );
        }

        private MicrophoneInputSystem GetAct3MicSystem()
        {
            if (playbackSystem != null) return playbackSystem;
            if (micInputSystemA != null) return micInputSystemA;
            return micInputSystemB;
        }

        private void StartAct3Music()
        {
            if (act3MusicClip == null) return;
            if (act3MusicSource == null)
            {
                GameObject holder = new GameObject("Act3Music");
                act3MusicSource = holder.AddComponent<AudioSource>();
                act3MusicSource.playOnAwake = false;
                act3MusicSource.loop = act3MusicLoop;
                act3MusicSource.spatialBlend = 0f;
            }

            if (act3MusicPersistAcrossScenes)
                DontDestroyOnLoad(act3MusicSource.gameObject);

            act3MusicSource.clip = act3MusicClip;
            act3MusicSource.volume = Mathf.Clamp01(act3MusicVolume);
            act3MusicSource.Play();
            act3MusicStarted = true;
        }

        private IEnumerator HandleActTransition()
        {
            if (currentAct == Act.Act3 && act3UseHardCut)
            {
                if (act3EndDelaySeconds > 0f)
                    yield return new WaitForSeconds(act3EndDelaySeconds);

                if (!string.IsNullOrWhiteSpace(act3EndScene))
                    SceneManager.LoadScene(act3EndScene);
                yield break;
            }

            float delay = GetActDelay();
            if (delay > 0f) yield return new WaitForSeconds(delay);

            string sceneName = GetActNextScene();
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                if (fadeSceneAudio && sceneAudioFadeOutTime > 0f)
                    yield return FadeAllSceneAudio(0f, sceneAudioFadeOutTime);

                if (fadeImage != null)
                {
                    if (blackScreenMode == BlackScreenMode.Fade)
                        yield return FadeToBlack(fadeToBlackTime);
                    else
                        SetFadeAlpha(1f);
                }

                if (blackHoldTime > 0f)
                    yield return new WaitForSeconds(blackHoldTime);

                SceneManager.LoadScene(sceneName);
            }
        }

        private float GetActDelay()
        {
            switch (currentAct)
            {
                case Act.Act1: return Mathf.Max(0f, act1DelayAfterLastLine);
                case Act.Act2: return Mathf.Max(0f, act2DelayAfterLastLine);
                case Act.Act3: return Mathf.Max(0f, act3DelayAfterLastLine);
                default: return 0f;
            }
        }

        private string GetActNextScene()
        {
            switch (currentAct)
            {
                case Act.Act1: return act1NextScene;
                case Act.Act2: return act2NextScene;
                case Act.Act3: return act3NextScene;
                default: return string.Empty;
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeImage == null) return;
            Color c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        }

        private IEnumerator FadeToBlack(float time)
        {
            if (fadeImage == null) yield break;
            float t = 0f;
            Color c = fadeImage.color;
            float startA = c.a;
            while (t < time)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(startA, 1f, t / Mathf.Max(0.0001f, time));
                fadeImage.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
            fadeImage.color = new Color(c.r, c.g, c.b, 1f);
        }

        private void EnsureBackgroundMusic()
        {
            if (backgroundMusicSource == null)
                backgroundMusicSource = gameObject.AddComponent<AudioSource>();

            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.loop = loopBackgroundMusic;
            backgroundMusicSource.spatialBlend = 0f;
            backgroundMusicSource.clip = backgroundMusicClip;
            backgroundMusicSource.volume = Mathf.Clamp01(backgroundMusicVolume);
            backgroundMusicSource.Play();
        }

        private IEnumerator FadeAllSceneAudio(float toVolume, float time)
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            if (sources == null || sources.Length == 0)
                yield break;

            float[] fromVolumes = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++)
                fromVolumes[i] = sources[i] != null ? sources[i].volume : 0f;

            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / time);
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] == null) continue;
                    sources[i].volume = Mathf.Lerp(fromVolumes[i], toVolume, lerp);
                }
                yield return null;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) continue;
                sources[i].volume = toVolume;
            }
        }

        private IEnumerator FadeInAllSceneAudio()
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            if (sources == null || sources.Length == 0)
                yield break;

            float[] targetVolumes = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) { targetVolumes[i] = 0f; continue; }
                targetVolumes[i] = sources[i].volume;
                sources[i].volume = 0f;
            }

            float t = 0f;
            while (t < sceneAudioFadeInTime)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / sceneAudioFadeInTime);
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] == null) continue;
                    sources[i].volume = Mathf.Lerp(0f, targetVolumes[i], lerp);
                }
                yield return null;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) continue;
                sources[i].volume = targetVolumes[i];
            }
        }

        public enum Act { Act1, Act2, Act3 }
        public enum BlackScreenMode { Snap, Fade }

#if UNITY_EDITOR
private static readonly string[] Act3Subtitles = new[]
{
    "",
    "[Tariq] …Don’t tell me that’s what I think it is.",
    "[Lance] We’re out of gas.",
    "[Tariq] Of course we are.",
    "[Tariq] Middle of nowhere. Again.",
    "[Lance] You sound like this isn’t new for you.",
    "[Tariq] It isn’t.",
    "[Lance] Should be enough to reach the turbine.",
    "[Tariq] Back home, when the generator died… nobody panicked anymore.",
    "[Tariq] Panic takes energy. People saved what little they had.",
    "[Tariq] Power would come and go.",
    "[Tariq] Every day for hours.",
    "[Tariq] The rest of the time… you just endure.",
    "[Lance] People shouldn’t have to live like that.",
    "[Tariq] They do live like that. Every day.",
    "[Tariq] And what changes? Speeches. Reports. Promises.",
    "[Lance] That’s why I’m here. Not to study the problem. To fix it.",
    "[Tariq] You know what hurts the most?",
    "[Tariq] Not the darkness.",
    "[Tariq] It’s getting used to it.",
    "[Tariq] Anyways, Ford.",
    "[Tariq] Let’s go bring light to a village.",
    // wait until car reaches that point
    "[Tariq] That’s the turbine. Just up there to the left.",
    "[Tariq] Pull over here on the side, we’ll walk.",
    "[Lance] Alright. Let’s do this.",
    "[Tariq] You know… you and I are opposites.",
    "[Tariq] You still believe the world can be fixed.",
    "[Tariq] I stopped believing a long time ago.",
    "[Tariq] But somehow… we ended up here anyway.",
    "[Tariq] Two sides of the same coin…",
    "[Lance] Let there be light, ey?",
    "[Tariq] It’s funny… a man from Canada, standing in the Sahara, bringing light to my people.",
    "[Tariq] The world is smaller than I thought.",
    "[Lance] It sure is.",
    "[Tariq] You know, Ford, I spent years telling myself hope was dangerous.",
    "[Tariq] That it leads to disappointment.",
    "[Tariq] That it’s safer to expect nothing.",
    "[Tariq] Have you ever heard the story of Pandora’s box?",
    "[Lance] Not really.",
    "[Tariq] The story says the world was once given a box that held every evil inside it.",
    "[Tariq] Greed. Hunger. War. Suffering.",
    "[Tariq] And when the box was opened… all of it escaped into the world.",
    "[Tariq] But there was one thing left at the bottom.",
    "[Tariq] Hope.",
    "[Tariq] Some say hope was a gift.",
    "[Tariq] Others say it was the cruelest thing of all.",
    "[Lance] Why cruel?",
    "[Tariq] Because hope makes you believe things can change…",
    "[Tariq] Even when everything you’ve lived through tells you they won’t.",
    "[Tariq] I spent years trying to kill that part of me.",
    "[Tariq] The part that still hoped.",
    "[Tariq] But standing here… seeing those lights turn on…",
    "[Tariq] Maybe hope isn’t weakness.",
    "[Tariq] Maybe it’s the only reason any of this works.",
    "[Tariq] You’ve shown me that hope is a powerful thing.",
    "[Tariq] So don’t ever lose it, Lance."
};

private static readonly float[] Act3TransitionDelays = new[]
{
    3.5f, 0f, 0.6f, 0.5f, 0.7f, 0.9f, 3f, 1.3f,
    0.2f, 0.3f, 0.4f, 0.6f, 0.5f, 0.7f, 0.4f,
    0.5f, 0.6f, 0.4f, 0.3f, 0.6f, 1.2f, 1.1f,
    1.3f, 1.2f, 12f, 0.4f, 0.5f, 1.6f, 1.4f,
    1.6f, 1.8f, 1.2f, 1.9f, 1.4f, 1.0f, 1.5f,
    1.2f, 1.1f, 1.4f, 1.2f, 1.4f, 1.6f, 1.2f,
    1.1f, 0.5f, 1.0f, 1.6f, 4f, 1.5f, 1.4f,
    1.6f, 1.8f, 1.5f, 1.4f, 1.6f, 2.2f
};

private void OnValidate()
{
    if (currentAct != Act.Act3) return;

    subtitles = Act3Subtitles;

    mouthShouldMove = new bool[Act3Subtitles.Length];
    for (int i = 0; i < Act3Subtitles.Length; i++)
    {
        mouthShouldMove[i] = Act3Subtitles[i].StartsWith("[Tariq]");
    }

    transitionDelays = new float[Act3Subtitles.Length];
    for (int i = 0; i < transitionDelays.Length; i++)
    {
        transitionDelays[i] = i < Act3TransitionDelays.Length
            ? Act3TransitionDelays[i]
            : 1.2f; // safe fallback
    }
}
#endif
    }
}
