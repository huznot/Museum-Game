using System.Collections;
using UnityEngine;

public class WindTurbineExitCheat : MonoBehaviour
{
    public static bool isWindTurbineAct = true;

    [Header("Input")]
    public KeyCode exitKey = KeyCode.E;

    [Tooltip("Wait this long after pressing E before anything happens.")]
    public float exitStartDelay = 0.35f;

    [Header("Characters")]
    public GameObject insideCharacter;
    public GameObject outsideCharacter;
    public Transform outsideSpawnPoint;

    [Header("Door (Smooth Move)")]
    public Transform doorTransform;

    [Tooltip("Door open angle in local degrees (same system as CarEnterTransition).")]
    public float doorOpenAngle = 60f;

    [Tooltip("Seconds for the door to open smoothly.")]
    public float doorOpenSmoothTime = 0.25f;

    [Tooltip("Seconds for the door to close smoothly.")]
    public float doorCloseSmoothTime = 0.25f;

    [Header("Outside animation")]
    public Animator outsideAnimator;

    [Tooltip("Animator STATE name for the close-door animation.")]
    public string closeDoorStateName = "CloseDoor";

    [Tooltip("Total length of the close-door animation in seconds.")]
    public float closeDoorClipSeconds = 1.0f;

    [Tooltip("When (0-1) through the close-door anim should the door begin closing?")]
    [Range(0f, 1f)]
    public float closeStartNormalizedTime = 0.55f;

    [Header("After close-door")]
    public MonoBehaviour[] enableAfterSequence;

    private bool _used = false;
    private Coroutine _doorRoutine;
    private Quaternion _doorClosedLocalRotation;

    void Start()
    {
        if (outsideCharacter) outsideCharacter.SetActive(false);
        if (doorTransform) _doorClosedLocalRotation = doorTransform.localRotation;
    }

    void Update()
    {
        if (_used) return;

        if (isWindTurbineAct && Input.GetKeyDown(exitKey))
        {
            _used = true;
            StartCoroutine(DoExitSwap());
        }
    }

    IEnumerator DoExitSwap()
    {
        // 0) Wait after pressing E (so it doesn't feel instant)
        if (exitStartDelay > 0f)
            yield return new WaitForSeconds(exitStartDelay);

        // 1) Smoothly OPEN door
        yield return SmoothDoorTo(GetDoorOpenRotation(), doorOpenSmoothTime);

        // 2) Swap characters (right after door is open)
        if (insideCharacter) insideCharacter.SetActive(false);

        if (outsideCharacter)
        {
            if (outsideSpawnPoint)
                outsideCharacter.transform.SetPositionAndRotation(outsideSpawnPoint.position, outsideSpawnPoint.rotation);

            outsideCharacter.SetActive(true);
        }

        // 3) Play close-door anim
        if (outsideAnimator)
            outsideAnimator.Play(closeDoorStateName, 0, 0f);

        // 4) Wait until the moment in the anim where closing should start
        float closeStartTime = Mathf.Clamp01(closeStartNormalizedTime) * closeDoorClipSeconds;
        yield return new WaitForSeconds(closeStartTime);

        // 5) Smoothly CLOSE door (while the anim continues)
        StartCoroutine(SmoothDoorTo(_doorClosedLocalRotation, doorCloseSmoothTime));

        // 6) Wait out the remaining animation time
        float remaining = Mathf.Max(0f, closeDoorClipSeconds - closeStartTime);
        yield return new WaitForSeconds(remaining);

        // 7) Enable walking scripts
        foreach (var mb in enableAfterSequence)
            if (mb) mb.enabled = true;
    }

    IEnumerator SmoothDoorTo(Quaternion targetRot, float duration)
    {
        if (!doorTransform)
            yield break;

        // Cancel any previous door movement so it doesn't fight itself
        if (_doorRoutine != null)
            StopCoroutine(_doorRoutine);

        _doorRoutine = StartCoroutine(SmoothDoorRoutine(targetRot, duration));
        yield return _doorRoutine;
        _doorRoutine = null;
    }

    IEnumerator SmoothDoorRoutine(Quaternion targetRot, float duration)
    {
        if (!doorTransform)
            yield break;

        Quaternion startRot = doorTransform.localRotation;

        if (duration <= 0f)
        {
            doorTransform.localRotation = targetRot;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            // Smoothstep feels more natural than linear
            u = u * u * (3f - 2f * u);

            doorTransform.localRotation = Quaternion.Slerp(startRot, targetRot, u);

            yield return null;
        }

        doorTransform.localRotation = targetRot;
    }

    private Quaternion GetDoorOpenRotation()
    {
        if (!doorTransform)
            return Quaternion.identity;

        Vector3 e = doorTransform.localEulerAngles;
        return Quaternion.Euler(e.x, e.y, doorOpenAngle);
    }
}
