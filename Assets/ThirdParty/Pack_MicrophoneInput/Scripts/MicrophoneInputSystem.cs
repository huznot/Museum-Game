using System.Collections;
using UnityEngine;
using TMPro;
namespace MicrophoneInput
{
    public class MicrophoneInputSystem : MonoBehaviour
    {
        [Header("Voice Target Options")]
        public VoiceTargetMode voiceTargetMode = VoiceTargetMode.Angle;
        public Transform targetObject;
        [Range(0f, 100f)] public float sensitivity = 100f;
        public float smoothSpeed = 5f;
        [Header("Frequency Sensitivity")]
        [Range(0f, 2f)]
        public float highFrequencyMultiplier = 1.2f;


        public enum Axis { X, Y, Z }

        [Header("Angle Mode")]
        public Axis angleAxis = Axis.X;
        public float minAngle = 0f;
        public float maxAngle = -50f;

        [Header("Position Mode")]
        public Axis positionAxis = Axis.X;
        public float minPosition = -0.5f;
        public float maxPosition = 0.5f;


        [Header("BlendShape Mode")]
        public string blendShapeName;
        public SkinnedMeshRenderer skinnedMesh;

        [Header("Intensity Mode")]
        public TextMeshPro textTarget;

        [Header("SpriteSwap Mode")]
        public SpriteRenderer spriteRenderer;
        public Sprite[] mouthSprites;
        [Range(0f, 100f)]
        [Tooltip("Minimum volume required before the mouth can open.")]
        public float spriteSwapThreshold = 8f;
        [Range(0f, 100f)]
        [Tooltip("Extra margin to prevent chatter around the threshold.")]
        public float spriteSwapHysteresis = 4f;
        [Range(0.5f, 1f)]
        [Tooltip("Sustained speech openness limit before transient boosts are added.")]
        public float spriteBaseOpenLimit = 0.82f;
        [Range(0f, 0.6f)]
        [Tooltip("How much quick transients can boost openness.")]
        public float spriteTransientBoost = 0.25f;
        [Range(0f, 0.4f)]
        [Tooltip("Small tone-based variation amount for brighter/darker phonemes.")]
        public float spriteToneVariation = 0.12f;
        [Header("Sprite Timing")]
        [Tooltip("If enabled, sprite mouth updates are capped to a target update rate.")]
        public bool limitSpriteUpdateRate = true;
        [Range(8f, 60f)]
        [Tooltip("Sprite mouth update rate cap. Lower values reduce visual jitter.")]
        public float spriteUpdateRate = 24f;
        [Range(1f, 40f)]
        [Tooltip("How quickly sprite openness follows the audio drive.")]
        public float spriteDriveResponse = 18f;
        [Range(0f, 0.2f)]
        [Tooltip("Minimum seconds between sprite index changes.")]
        public float spriteMinChangeInterval = 0.025f;
        [Range(1f, 3f)]
        [Tooltip("Non-linear openness curve. Higher values spend more time in mid/closed shapes.")]
        public float spriteOpenCurve = 1.6f;
        [Range(0.6f, 1f)]
        [Tooltip("Drive threshold required to use the widest-open sprite frame.")]
        public float spriteMaxFrameThreshold = 0.9f;
        [Range(0f, 1f)]
        [Tooltip("Transient requirement to allow widest-open frame near the threshold.")]
        public float spriteMaxFramePeakRequirement = 0.2f;

        [Header("Dynamics")]
        [Range(0.1f, 20f)]
        [Tooltip("How fast the mouth reacts when volume rises.")]
        public float volumeAttack = 10f;
        [Range(0.1f, 20f)]
        [Tooltip("How fast the mouth closes when volume falls.")]
        public float volumeRelease = 6f;
        [Range(0.1f, 10f)]
        [Tooltip("Adaptive normalization tracking speed.")]
        public float adaptiveNormalizationSpeed = 2f;
        [Range(0.4f, 1.4f)]
        [Tooltip("Normalized volume curve. Lower values emphasize low-mid movement.")]
        public float normalizedVolumeCurve = 0.8f;
        [Range(0f, 1f)]
        [Tooltip("Blend between RMS loudness and fast peak loudness.")]
        public float peakBlend = 0.35f;
        [Range(0.1f, 10f)]
        [Tooltip("How fast the adaptive noise floor tracks room/clip baseline.")]
        public float noiseFloorTrackSpeed = 2f;
        [Range(1f, 3f)]
        [Tooltip("How strongly quiet baseline is removed before mouth movement.")]
        public float noiseGateStrength = 1.25f;
        [Range(0.5f, 4f)]
        [Tooltip("Dynamic range shaping so syllables read clearly without maxing out.")]
        public float dynamicRangeCompression = 1.2f;
        [Range(0f, 30f)]
        [Tooltip("Absolute speech energy needed before the mouth can start opening.")]
        public float speechOpenThreshold = 6f;
        [Range(0f, 30f)]
        [Tooltip("Close threshold for speech gate hysteresis (prevents chatter).")]
        public float speechCloseThreshold = 3f;
        [Tooltip("Apply noise-floor speech gating to prerecorded dialogue playback. Disable for more expressive lip-sync.")]
        public bool useSpeechGateForPlayback = false;

        [Header("Dialogue Control")]
        [Tooltip("TMP text to display the current subtitle (optional).")]
        public TMP_Text subtitleTextTarget;
        [Tooltip("Start playback on Awake using the first entry.")]
        public bool playOnStart = true;
        [Tooltip("Automatically advance to the next line after clip end + transition delay.")]
        public bool autoAdvance = true;

        private AudioSource audioSource;
        private float currentVolume;
        private int blendShapeIndex = -1;
        private VoiceTargetMode lastMode = VoiceTargetMode.Angle;
        private float[] sampleBuffer = new float[256];
        private float[] spectrumBuffer = new float[256];
        private float[] previousSpectrumBuffer = new float[256];
        private int currentIndex;
        private Coroutine advanceRoutine;
        private bool spriteGateOpen;
        private bool spatializeCurrentLine;
        private bool inDelay;
        private float delayEndTime;
        private bool suppressPlayback;
        private bool useExternalVolume;
        private float externalVolume;
        private float adaptivePeakVolume = 30f;
        private float adaptiveNoiseFloor = 0f;
        private float spectralCentroid01;
        private float spectralFlux01;
        private float speechEnergy01;
        private bool speechGateOpen;
        private int lastSpriteIndex = -1;
        private float spriteDrive01;
        private float spriteUpdateTimer;
        private float lastSpriteChangeTime;
        public enum VoiceTargetMode { Angle, BlendShape, Intensity, Position, SpriteSwap }



        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.mute = !IsAudiblePlaybackEnabled();
            audioSource.spatialBlend = 0f; // 2D by default

            ResolveTargetBindingsIfMissing();

            if (voiceTargetMode == VoiceTargetMode.BlendShape && skinnedMesh != null)
            {
                blendShapeIndex = skinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeName);
            }

            if (playOnStart && HasClips())
            {
                if (!suppressPlayback)
                    PlayCurrentLine();
            }
            else if (!HasClips())
            {
                Debug.LogWarning("No voicelines assigned. Fill MicrophoneManager.voiceLines or set a single voiceClip.");
            }

        }

        void Update()
        {
            if (voiceTargetMode != lastMode)
            {
                lastMode = voiceTargetMode;
                ResolveTargetBindingsIfMissing();

                if (voiceTargetMode == VoiceTargetMode.BlendShape && skinnedMesh != null)
                {
                    blendShapeIndex = skinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeName);
                    Debug.Log("BlendShape Index: " + blendShapeIndex);
                }
            }

            UpdateMicVolume();
            UpdateSpatializedVoicePosition();
            AnimateVoiceTarget();
        }

        public void PlayCurrentLine()
        {
            if (suppressPlayback) return;
            if (!HasClips()) return;
            currentIndex = Mathf.Clamp(currentIndex, 0, GetVoiceLineCount() - 1);
            adaptivePeakVolume = 30f;
            adaptiveNoiseFloor = 0f;
            speechEnergy01 = 0f;
            speechGateOpen = false;
            audioSource.clip = GetVoiceLineForCurrentManager(currentIndex);
            audioSource.time = 0f;
            audioSource.loop = false;
            ConfigureSpatialAudioForCurrentLine();
            audioSource.Play();
            if (advanceRoutine != null) StopCoroutine(advanceRoutine);
            if (autoAdvance) advanceRoutine = StartCoroutine(WaitAndAdvance());
            UpdateSubtitle();
        }

        public void NextLine()
        {
            if (suppressPlayback) return;
            if (!HasClips()) return;
            if (currentIndex < GetVoiceLineCount() - 1) currentIndex++;
            PlayCurrentLine();
        }

        public int GetCurrentLineIndex() => currentIndex;

        public float GetCurrentPlaybackTime()
        {
            if (audioSource == null) return 0f;
            return audioSource.time;
        }

        public bool IsPlaying()
        {
            return audioSource != null && audioSource.isPlaying;
        }

        public int GetCurrentTimeSamples()
        {
            if (audioSource == null) return 0;
            return audioSource.timeSamples;
        }

        public bool SuspendPlayback()
        {
            bool wasPlaying = audioSource != null && audioSource.isPlaying;
            if (advanceRoutine != null) StopCoroutine(advanceRoutine);
            if (audioSource != null) audioSource.Pause();
            return wasPlaying;
        }

        public void SetExternalVolume(float volume)
        {
            useExternalVolume = true;
            externalVolume = Mathf.Clamp(volume, 0f, 100f);
        }

        public void ClearExternalVolume()
        {
            useExternalVolume = false;
        }

        public float GetCurrentVolume()
        {
            return currentVolume;
        }

        public void SetPlaybackSuppressed(bool suppressed)
        {
            suppressPlayback = suppressed;
            if (!suppressPlayback) return;
            if (advanceRoutine != null)
            {
                StopCoroutine(advanceRoutine);
                advanceRoutine = null;
            }
            if (audioSource != null)
                audioSource.Stop();
        }

        public bool IsInDelay()
        {
            return inDelay;
        }

        public float GetRemainingDelay()
        {
            if (!inDelay) return 0f;
            return Mathf.Max(0f, delayEndTime - Time.time);
        }

        public void TransferPlaybackFrom(MicrophoneInputSystem other, bool wasPlaying)
        {
            if (other == null) return;
            bool wasInDelay = other.IsInDelay();
            float remainingDelay = other.GetRemainingDelay();
            currentIndex = Mathf.Clamp(other.GetCurrentLineIndex(), 0, GetVoiceLineCount() > 0 ? GetVoiceLineCount() - 1 : 0);
            if (audioSource == null) return;

            if (advanceRoutine != null)
            {
                StopCoroutine(advanceRoutine);
                advanceRoutine = null;
            }
            audioSource.Stop();

            if (wasInDelay && remainingDelay > 0f)
            {
                ClearSubtitle();
                inDelay = true;
                delayEndTime = Time.time + remainingDelay;
                advanceRoutine = StartCoroutine(WaitRemainingDelayAndAdvance());
                return;
            }

            audioSource.clip = GetVoiceLineForCurrentManager(currentIndex);
            audioSource.time = 0f;
            audioSource.loop = false;
            ConfigureSpatialAudioForCurrentLine();
            UpdateSubtitle();

            int timeSamples = Mathf.Max(0, other.GetCurrentTimeSamples());
            if (audioSource.clip != null)
                timeSamples = Mathf.Min(timeSamples, audioSource.clip.samples);
            audioSource.timeSamples = timeSamples;

            if (wasPlaying)
            {
                audioSource.Play();
                audioSource.timeSamples = timeSamples;
                if (advanceRoutine != null) StopCoroutine(advanceRoutine);
                if (autoAdvance) advanceRoutine = StartCoroutine(WaitAndAdvance());
            }
            else
            {
                audioSource.Pause();
            }
        }

        private IEnumerator WaitRemainingDelayAndAdvance()
        {
            while (Time.time < delayEndTime)
                yield return null;
            inDelay = false;

            if (currentIndex < GetVoiceLineCount() - 1)
            {
                currentIndex++;
                PlayCurrentLine();
            }
            else
            {
                ClearSubtitle();
                BeginTransitionForCurrentManager();
            }
        }

        public void SetLine(int index)
        {
            if (suppressPlayback) return;
            currentIndex = Mathf.Clamp(index, 0, GetVoiceLineCount() > 0 ? GetVoiceLineCount() - 1 : 0);
            PlayCurrentLine();
        }

        public float GetCurrentTransitionDelay()
        {
            if (MuseumMicrophone.Instance != null) return MuseumMicrophone.Instance.GetTransitionDelay(currentIndex);
            if (MicrophoneManager.Instance != null) return MicrophoneManager.Instance.GetTransitionDelay(currentIndex);
            return 0f;
        }

        private bool HasClips()
        {
            if (MuseumMicrophone.Instance != null) return MuseumMicrophone.Instance.VoiceLineCount > 0;
            return MicrophoneManager.Instance != null && MicrophoneManager.Instance.VoiceLineCount > 0;
        }

        private int GetVoiceLineCount()
        {
            if (MuseumMicrophone.Instance != null) return MuseumMicrophone.Instance.VoiceLineCount;
            return MicrophoneManager.Instance != null ? MicrophoneManager.Instance.VoiceLineCount : 0;
        }

        private IEnumerator WaitAndAdvance()
        {
            // Wait for current clip to finish
            while (audioSource != null && audioSource.isPlaying)
                yield return null;

            float delay = GetCurrentTransitionDelay();
            if (delay > 0f)
            {
                ClearSubtitle();
                inDelay = true;
                delayEndTime = Time.time + delay;
                while (Time.time < delayEndTime)
                    yield return null;
                inDelay = false;
            }

            if (currentIndex < GetVoiceLineCount() - 1)
            {
                currentIndex++;
                PlayCurrentLine();
            }
            else
            {
                ClearSubtitle();
                BeginTransitionForCurrentManager();
            }
        }

        private void ClearSubtitle()
        {
            TMP_Text target = subtitleTextTarget;
            if (target == null) target = GetSubtitleTarget();
            if (target == null) return;

            target.text = string.Empty;
        }

        private void ConfigureSpatialAudioForCurrentLine()
        {
            if (audioSource == null) return;
            spatializeCurrentLine = ShouldUseSpatialAudio();
            audioSource.spatialBlend = spatializeCurrentLine ? 1f : 0f;
            if (spatializeCurrentLine)
            {
                audioSource.minDistance = GetSpatialVoiceMinDistance();
                audioSource.maxDistance = GetSpatialVoiceMaxDistance();
                audioSource.volume = Mathf.Max(0f, GetSpatialVoiceVolume());
            }
            else
            {
                audioSource.volume = 1f;
            }
            UpdateSpatializedVoicePosition();
        }

        private void UpdateSpatializedVoicePosition()
        {
            if (!spatializeCurrentLine || audioSource == null) return;
            Transform sourceTarget = targetObject != null ? targetObject : transform;
            audioSource.transform.position = sourceTarget.position;
        }

        private bool ShouldUseSpatialAudio()
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.useSpatialAudio;
            }

            if (MicrophoneManager.Instance == null) return false;
            string subtitle = MicrophoneManager.Instance.GetSubtitle(currentIndex);
            return !string.IsNullOrEmpty(subtitle) && subtitle.StartsWith("[Tariq]");
        }

        void UpdateMicVolume()
        {
            if (useExternalVolume)
            {
                float externalTarget = Mathf.Clamp(externalVolume, 0f, 100f);
                // In single-dialogue-player mode this value is already the processed driver volume.
                float externalRate = externalTarget >= currentVolume ? volumeAttack : volumeRelease;
                currentVolume = Mathf.Lerp(currentVolume, externalTarget, GetDampFactor(externalRate));
                spectralCentroid01 = 0f;
                spectralFlux01 = 0f;
                speechEnergy01 = externalTarget / 100f;
                return;
            }

            if (audioSource == null || !audioSource.clip) return;

            if (!audioSource.isPlaying)
            {
                currentVolume = Mathf.Lerp(currentVolume, 0f, GetDampFactor(volumeRelease));
                spectralCentroid01 = 0f;
                spectralFlux01 = 0f;
                speechEnergy01 = 0f;
                speechGateOpen = false;
                return;
            }

            int sampleCount = Mathf.Min(sampleBuffer.Length, audioSource.clip.samples);
            if (sampleCount <= 0) return;
            if (sampleCount != sampleBuffer.Length) sampleBuffer = new float[sampleCount];

            int currentSample = audioSource.timeSamples;
            int startSample = Mathf.Clamp(currentSample - sampleCount, 0, audioSource.clip.samples - sampleCount);
            bool hasSampleData = false;
            if (audioSource.clip != null)
            {
                hasSampleData = audioSource.clip.GetData(sampleBuffer, startSample);
            }

            if (!hasSampleData)
            {
                // Build-time import/compression can make clip sample extraction unavailable.
                // Fallback to output samples so mouth animation still tracks audible speech.
                audioSource.GetOutputData(sampleBuffer, 0);
            }

            float sum = 0f;
            float peak = 0f;
            foreach (var s in sampleBuffer)
            {
                sum += s * s;
                float abs = Mathf.Abs(s);
                if (abs > peak) peak = abs;
            }
            float rmsVolume = Mathf.Sqrt(sum / sampleBuffer.Length) * 100f * sensitivity;
            float peakVolume = peak * 100f * sensitivity;
            float volume = Mathf.Lerp(rmsVolume, peakVolume, Mathf.Clamp01(peakBlend));

            // Frequency analysis
            audioSource.GetSpectrumData(spectrumBuffer, 0, FFTWindow.BlackmanHarris);

            // Some build/import combinations can return near-zero output data for analysis.
            // If that happens, derive a loudness estimate from spectrum energy instead.
            if (!hasSampleData && volume <= 0.01f)
            {
                float spectrumEnergy = 0f;
                for (int i = 0; i < spectrumBuffer.Length; i++)
                    spectrumEnergy += spectrumBuffer[i];

                float spectrumDerivedVolume = Mathf.Sqrt(Mathf.Max(0f, spectrumEnergy)) * 2200f * sensitivity;
                volume = Mathf.Max(volume, spectrumDerivedVolume);
            }

            float lowFreqEnergy = 0f;
            float highFreqEnergy = 0f;

            for (int i = 0; i < spectrumBuffer.Length; i++)
            {
                float freq = i * AudioSettings.outputSampleRate / 2 / spectrumBuffer.Length;
                if (freq < 1000f) lowFreqEnergy += spectrumBuffer[i];
                else highFreqEnergy += spectrumBuffer[i];
            }

            bool isHighFreqDominant = highFreqEnergy > lowFreqEnergy;

            // Volume + frequency weighting
            float adjustedVolume = volume;

            if (isHighFreqDominant)
                adjustedVolume *= highFrequencyMultiplier;

            bool animateMouth = ShouldMoveMouthForCurrentManager(currentIndex);
            if (!animateMouth) adjustedVolume = 0f;

            adjustedVolume = Mathf.Clamp(adjustedVolume, 0f, 100f);

            float speechOnlyVolume = adjustedVolume;
            if (useSpeechGateForPlayback)
            {
                float floorSpeed = adjustedVolume <= adaptiveNoiseFloor
                    ? noiseFloorTrackSpeed * 1.4f
                    : noiseFloorTrackSpeed * 0.2f;
                adaptiveNoiseFloor = Mathf.Lerp(adaptiveNoiseFloor, adjustedVolume, GetDampFactor(floorSpeed));
                float gatedFloor = adaptiveNoiseFloor * Mathf.Max(1f, noiseGateStrength);
                speechOnlyVolume = Mathf.Max(0f, adjustedVolume - gatedFloor);

                float openThreshold = Mathf.Max(0f, speechOpenThreshold);
                float closeThreshold = Mathf.Clamp(speechCloseThreshold, 0f, openThreshold);
                if (!speechGateOpen)
                {
                    if (speechOnlyVolume >= openThreshold) speechGateOpen = true;
                }
                else
                {
                    if (speechOnlyVolume <= closeThreshold) speechGateOpen = false;
                }

                if (!speechGateOpen)
                    speechOnlyVolume = 0f;
            }
            else
            {
                speechGateOpen = speechOnlyVolume > 0.001f;
            }

            // Adaptive normalization keeps loud clips from pinning the mouth at max.
            float safeInput = Mathf.Max(0.0001f, speechOnlyVolume);
            if (safeInput > adaptivePeakVolume)
                adaptivePeakVolume = safeInput;
            else
                adaptivePeakVolume = Mathf.Lerp(adaptivePeakVolume, safeInput, GetDampFactor(adaptiveNormalizationSpeed));

            float normalized = speechOnlyVolume / Mathf.Max(8f, adaptivePeakVolume);
            float comp = Mathf.Max(0.01f, dynamicRangeCompression);
            normalized = Mathf.Log(1f + normalized * comp) / Mathf.Log(1f + comp);
            normalized = Mathf.Clamp01(Mathf.Pow(normalized, Mathf.Max(0.01f, normalizedVolumeCurve)));
            float targetVolume = normalized * 100f;
            float smoothingRate = targetVolume >= currentVolume ? volumeAttack : volumeRelease;
            currentVolume = Mathf.Lerp(currentVolume, targetVolume, GetDampFactor(smoothingRate));
            speechEnergy01 = normalized;

            float weightedIndexSum = 0f;
            float energySum = 0f;
            float fluxSum = 0f;
            for (int i = 0; i < spectrumBuffer.Length; i++)
            {
                float e = spectrumBuffer[i];
                energySum += e;
                weightedIndexSum += i * e;
                fluxSum += Mathf.Abs(e - previousSpectrumBuffer[i]);
                previousSpectrumBuffer[i] = e;
            }

            if (energySum > 0.000001f)
                spectralCentroid01 = Mathf.Clamp01((weightedIndexSum / energySum) / (spectrumBuffer.Length - 1));
            else
                spectralCentroid01 = 0f;

            spectralFlux01 = Mathf.Clamp01(fluxSum * 250f * Mathf.Clamp01(speechEnergy01 * 1.25f));
        }

        private float GetDampFactor(float speed)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0.0001f, speed) * Time.deltaTime);
        }

        private bool IsAudiblePlaybackEnabled()
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.audiblePlayback;
            }

            return MicrophoneManager.Instance == null || MicrophoneManager.Instance.audiblePlayback;
        }

        private AudioClip GetVoiceLineForCurrentManager(int index)
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.GetVoiceLine(index);
            }

            return MicrophoneManager.Instance != null ? MicrophoneManager.Instance.GetVoiceLine(index) : null;
        }

        private bool ShouldMoveMouthForCurrentManager(int index)
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.ShouldMoveMouth(index);
            }

            return MicrophoneManager.Instance == null || MicrophoneManager.Instance.ShouldMoveMouth(index);
        }

        private string GetSubtitleForCurrentManager(int index)
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.GetSubtitle(index);
            }

            return MicrophoneManager.Instance != null ? MicrophoneManager.Instance.GetSubtitle(index) : string.Empty;
        }

        private TMP_Text GetSubtitleTarget()
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.subtitleTextTarget;
            }

            return MicrophoneManager.Instance != null ? MicrophoneManager.Instance.subtitleTextTarget : null;
        }

        private void BeginTransitionForCurrentManager()
        {
            if (MuseumMicrophone.Instance != null)
            {
                MuseumMicrophone.Instance.BeginActTransition();
                return;
            }

            if (MicrophoneManager.Instance != null)
            {
                MicrophoneManager.Instance.BeginActTransition();
            }
        }

        private float GetSpatialVoiceVolume()
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.spatialVoiceVolume;
            }

            return MicrophoneManager.Instance != null ? MicrophoneManager.Instance.spatialVoiceVolume : 1f;
        }

        private float GetSpatialVoiceMinDistance()
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.spatialVoiceMinDistance;
            }

            return MicrophoneManager.Instance != null ? MicrophoneManager.Instance.spatialVoiceMinDistance : 5f;
        }

        private float GetSpatialVoiceMaxDistance()
        {
            if (MuseumMicrophone.Instance != null)
            {
                return MuseumMicrophone.Instance.spatialVoiceMaxDistance;
            }

            return MicrophoneManager.Instance != null ? MicrophoneManager.Instance.spatialVoiceMaxDistance : 40f;
        }

        private void ResolveTargetBindingsIfMissing()
        {
            if (targetObject == null) return;

            if (voiceTargetMode == VoiceTargetMode.BlendShape && skinnedMesh == null)
            {
                skinnedMesh = targetObject.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMesh == null) skinnedMesh = targetObject.GetComponent<SkinnedMeshRenderer>();
            }

            if (voiceTargetMode == VoiceTargetMode.SpriteSwap && spriteRenderer == null)
            {
                spriteRenderer = targetObject.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer == null) spriteRenderer = targetObject.GetComponent<SpriteRenderer>();
            }
        }

        void UpdateSubtitle()
        {
            TMP_Text target = subtitleTextTarget;
            if (target == null) target = GetSubtitleTarget();
            if (target == null) return;

            target.text = GetSubtitleForCurrentManager(currentIndex);
        }

        void AnimateVoiceTarget()
        {
            switch (voiceTargetMode)
            {
                case VoiceTargetMode.Angle:
                    if (targetObject != null)
                    {
                        float angle = Mathf.Lerp(minAngle, maxAngle, Mathf.Clamp01(currentVolume / 100f));
                        Vector3 current = targetObject.localEulerAngles;

                        // Normalize angles to -180..180 for smoother transition
                        if (current.x > 180f) current.x -= 360f;
                        if (current.y > 180f) current.y -= 360f;
                        if (current.z > 180f) current.z -= 360f;

                        switch (angleAxis)
                        {
                            case Axis.X: current.x = angle; break;
                            case Axis.Y: current.y = angle; break;
                            case Axis.Z: current.z = angle; break;
                        }

                        Quaternion rot = Quaternion.Euler(current);
                        targetObject.localRotation = Quaternion.Lerp(targetObject.localRotation, rot, Time.deltaTime * smoothSpeed);
                    }
                    break;


                case VoiceTargetMode.BlendShape:
                    if (skinnedMesh != null && blendShapeIndex >= 0)
                    {
                        skinnedMesh.SetBlendShapeWeight(blendShapeIndex, Mathf.Clamp(currentVolume, 0f, 100f));
                    }
                    break;

                case VoiceTargetMode.Intensity:
                    if (textTarget != null)
                    {
                        textTarget.text = Mathf.Clamp(currentVolume, 0f, 100f).ToString("F0");
                    }
                    break;
                case VoiceTargetMode.Position:
                    if (targetObject != null)
                    {
                        float pos = Mathf.Lerp(minPosition, maxPosition, Mathf.Clamp01(currentVolume / 100f));
                        Vector3 current = targetObject.localPosition;

                        switch (positionAxis)
                        {
                            case Axis.X: current.x = pos; break;
                            case Axis.Y: current.y = pos; break;
                            case Axis.Z: current.z = pos; break;
                        }

                        targetObject.localPosition = Vector3.Lerp(targetObject.localPosition, current, Time.deltaTime * smoothSpeed);
                    }
                    break;
                case VoiceTargetMode.SpriteSwap:
                    if (spriteRenderer != null && mouthSprites != null && mouthSprites.Length > 0)
                    {
                        // When externally driven (single dialogue player), manager already gates who can talk.
                        bool allowMouthMove = useExternalVolume
                            ? externalVolume > 0.001f
                            : ShouldMoveMouthForCurrentManager(currentIndex);
                        bool externallyClosed = useExternalVolume && externalVolume <= 0.001f;
                        if (!allowMouthMove || externallyClosed)
                        {
                            spriteGateOpen = false;
                            spriteDrive01 = 0f;
                            spriteUpdateTimer = 0f;
                            lastSpriteIndex = 0;
                            spriteRenderer.sprite = mouthSprites[0];
                            break;
                        }

                        float volumeClamped = Mathf.Clamp(currentVolume, 0f, 100f);

                        if (!spriteGateOpen)
                        {
                            if (volumeClamped >= spriteSwapThreshold + spriteSwapHysteresis)
                                spriteGateOpen = true;
                        }
                        else
                        {
                            if (volumeClamped <= Mathf.Max(0f, spriteSwapThreshold - spriteSwapHysteresis))
                                spriteGateOpen = false;
                        }

                        float gatedVolume = spriteGateOpen ? volumeClamped : 0f;
                        float volume01 = gatedVolume / 100f;
                        float baseDrive = volume01 * Mathf.Clamp(spriteBaseOpenLimit, 0.5f, 1f);
                        float transientDrive = spectralFlux01 * Mathf.Max(0f, spriteTransientBoost);
                        float toneDrive = ((spectralCentroid01 - 0.5f) * 2f) * Mathf.Max(0f, spriteToneVariation);
                        float expressiveDrive = Mathf.Clamp01(baseDrive + transientDrive + toneDrive);
                        spriteDrive01 = Mathf.Lerp(spriteDrive01, expressiveDrive, GetDampFactor(spriteDriveResponse));

                        if (limitSpriteUpdateRate)
                        {
                            float updateInterval = 1f / Mathf.Max(1f, spriteUpdateRate);
                            spriteUpdateTimer += Time.deltaTime;
                            if (spriteUpdateTimer < updateInterval)
                                break;
                            spriteUpdateTimer = 0f;
                        }

                        int maxIndex = mouthSprites.Length - 1;
                        int highestNormalIndex = Mathf.Max(0, maxIndex - 1);
                        float mappedDrive = Mathf.Pow(Mathf.Clamp01(spriteDrive01), Mathf.Max(1f, spriteOpenCurve));
                        int index = Mathf.RoundToInt(mappedDrive * highestNormalIndex);
                        index = Mathf.Clamp(index, 0, highestNormalIndex);

                        float maxThreshold = Mathf.Clamp01(spriteMaxFrameThreshold);
                        bool allowMaxFrame = mappedDrive >= maxThreshold
                            || (mappedDrive >= maxThreshold - 0.08f && spectralFlux01 >= Mathf.Clamp01(spriteMaxFramePeakRequirement));
                        if (allowMaxFrame)
                            index = maxIndex;

                        // Prevent rapid flutter between top two frames at strong sustained loudness.
                        if (lastSpriteIndex == maxIndex && index == highestNormalIndex && mappedDrive >= maxThreshold * 0.9f)
                            index = maxIndex;

                        if (index != lastSpriteIndex && (Time.time - lastSpriteChangeTime) < spriteMinChangeInterval)
                            index = lastSpriteIndex;

                        spriteRenderer.sprite = mouthSprites[index];
                        if (index != lastSpriteIndex)
                            lastSpriteChangeTime = Time.time;
                        lastSpriteIndex = index;
                    }
                    break;


            }
        }


    }
}
