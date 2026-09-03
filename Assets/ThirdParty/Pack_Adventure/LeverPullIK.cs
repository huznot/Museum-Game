using UnityEngine;
using UnityEngine.Animations.Rigging;

public class LeverPullIK : MonoBehaviour
{
    public static bool Act3LeverPulled;
    [Header("References")]
    public Transform player;
    public Transform lever;
    public Transform leverHandle;
    public Transform ikTarget;
    public Rig rigLayer;
    public TwoBoneIKConstraint ikConstraint;

    [Header("Interaction")]
    public float activationDistance = 2.0f;
    public float ikMoveSpeed = 5.0f;
    public float delayBeforePull = 0.25f;
    public KeyCode interactKey = KeyCode.E;
    public bool requireExternalTrigger = true;

    [Header("Lever Motion")]
    public Vector3 leverUpLocalEuler = new Vector3(271.656586f, -0.0001742756f, 0.0001597851f);
    public float pullSpeed = 120.0f;

    [Header("Effects")]
    public GameObject redLight;
    public GameObject greenLight;
    public Behaviour turbineRotationScript;

    [Header("Audio")]
    public AudioSource leverAudioSource;
    public AudioClip leverPullClip;
    [Range(0f, 1f)] public float leverPullVolume = 1f;
    public AudioSource turbineAudioSource;
    public AudioClip turbineStartupClip;
    public AudioClip turbineLoopClip;
    [Range(0f, 1f)] public float turbineStartupVolume = 1f;
    [Range(0f, 1f)] public float turbineLoopVolume = 1f;
    public AudioSource distantAudioSource;
    public AudioClip distantVillageClip;
    [Range(0f, 1f)] public float distantVillageVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool isGrabbing = false;
    private bool handOnHandle = false;
    private bool isPulling = false;
    private bool leverPulled = false;
    private float grabStartTime = 0f;
    private bool effectsTriggered = false;

    private Quaternion _startLocalRot;
    private Quaternion _targetLocalRot;

    void Start()
    {
        if (player == null || lever == null || leverHandle == null || ikTarget == null || rigLayer == null)
        {
            Debug.LogError("[LeverPullIK] Missing references. Assign all required fields.");
            enabled = false;
            return;
        }

        _startLocalRot = lever.localRotation;
        _targetLocalRot = Quaternion.Euler(leverUpLocalEuler);

        EnsureAudioSources();
    }

    void Update()
    {
        float dist = Vector3.Distance(player.position, leverHandle.position);
        bool inRange = dist < activationDistance;

        if (!requireExternalTrigger && inRange && Input.GetKeyDown(interactKey) && !leverPulled)
        {
            BeginGrab();
        }

        if (!inRange)
        {
            isGrabbing = false;
            isPulling = false;
            handOnHandle = false;
        }

        float targetWeight = isGrabbing ? 1f : 0f;
        if (rigLayer != null)
            rigLayer.weight = Mathf.Lerp(rigLayer.weight, targetWeight, Time.deltaTime * 6f);
        if (ikConstraint != null)
            ikConstraint.weight = Mathf.Lerp(ikConstraint.weight, targetWeight, Time.deltaTime * 6f);

        if (isGrabbing)
        {
            Vector3 targetPos = leverHandle.position;
            ikTarget.position = Vector3.Lerp(ikTarget.position, targetPos, Time.deltaTime * ikMoveSpeed);
            ikTarget.rotation = Quaternion.Slerp(ikTarget.rotation, leverHandle.rotation, Time.deltaTime * ikMoveSpeed);

            if (!handOnHandle && Vector3.Distance(ikTarget.position, targetPos) < 0.05f)
            {
                handOnHandle = true;
                grabStartTime = Time.time;
            }

            if (handOnHandle && !isPulling && Time.time - grabStartTime >= delayBeforePull)
            {
                isPulling = true;
            }

        if (isPulling)
        {
            lever.localRotation = Quaternion.RotateTowards(lever.localRotation, _targetLocalRot, pullSpeed * Time.deltaTime);

            if (Quaternion.Angle(lever.localRotation, _targetLocalRot) <= 0.5f)
            {
                lever.localRotation = _targetLocalRot;
                isGrabbing = false;
                handOnHandle = false;
                isPulling = false;
                leverPulled = true;
                Act3LeverPulled = true;
                if (!effectsTriggered)
                {
                    effectsTriggered = true;
                    StartCoroutine(OnLeverPullComplete());
                    StartCoroutine(EnableStreetLampsAfterDelay(5f));
                    }
                }
            }
        }
    }

    public bool TriggerPull()
    {
        if (leverPulled) return false;
        if (player == null || leverHandle == null) return false;

        float dist = Vector3.Distance(player.position, leverHandle.position);
        if (dist >= activationDistance) return false;

        BeginGrab();
        return true;
    }

    private void BeginGrab()
    {
        isGrabbing = true;
        handOnHandle = false;
        isPulling = false;
        grabStartTime = Time.time;

        if (leverAudioSource != null && leverPullClip != null)
            leverAudioSource.PlayOneShot(leverPullClip, leverPullVolume);

        if (debugLogs)
            Debug.Log("[LeverPullIK] BeginGrab()", this);
    }

    private void EnsureAudioSources()
    {
        if (leverAudioSource == null && leverPullClip != null)
            leverAudioSource = gameObject.AddComponent<AudioSource>();
        if (turbineAudioSource == null && (turbineStartupClip != null || turbineLoopClip != null))
            turbineAudioSource = gameObject.AddComponent<AudioSource>();
        if (distantAudioSource == null && distantVillageClip != null)
            distantAudioSource = gameObject.AddComponent<AudioSource>();
    }

    private System.Collections.IEnumerator OnLeverPullComplete()
    {
        if (debugLogs)
            Debug.Log("[LeverPullIK] OnLeverPullComplete() start", this);

        if (redLight != null) redLight.SetActive(false);
        if (greenLight != null) greenLight.SetActive(true);
        if (turbineRotationScript != null) turbineRotationScript.enabled = true;

        if (turbineAudioSource != null)
        {
            turbineAudioSource.loop = false;
            if (turbineStartupClip != null)
            {
                if (debugLogs)
                    Debug.Log("[LeverPullIK] turbine startup", this);
                turbineAudioSource.PlayOneShot(turbineStartupClip, turbineStartupVolume);
                yield return new WaitForSeconds(turbineStartupClip.length);
            }

            if (turbineLoopClip != null)
            {
                if (debugLogs)
                    Debug.Log("[LeverPullIK] turbine loop", this);
                turbineAudioSource.clip = turbineLoopClip;
                turbineAudioSource.loop = true;
                turbineAudioSource.volume = turbineLoopVolume;
                turbineAudioSource.Play();
            }
        }

        if (distantAudioSource != null && distantVillageClip != null)
        {
            if (debugLogs)
                Debug.Log("[LeverPullIK] distant audio scheduled", this);
            yield return new WaitForSeconds(3f);
            if (debugLogs)
                Debug.Log("[LeverPullIK] distant audio play", this);
            distantAudioSource.PlayOneShot(distantVillageClip, distantVillageVolume);
        }

        if (debugLogs)
            Debug.Log("[LeverPullIK] OnLeverPullComplete() end", this);
    }

    private System.Collections.IEnumerator EnableStreetLampsAfterDelay(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);
        if (debugLogs)
            Debug.Log("[LeverPullIK] Powering street lamps on", this);
        StreetLampPower.PowerOn = true;
    }
}
