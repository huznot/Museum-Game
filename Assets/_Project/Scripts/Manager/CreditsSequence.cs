using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreditsSequence : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform container;
    [SerializeField] private List<GameObject> entries = new List<GameObject>();

    [Header("Timing")]
    [SerializeField] private float defaultDuration = 5f;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float betweenDelay = 2f;
    [SerializeField] private float[] durations = new float[0];
    [SerializeField] private bool loop = false;

    [Header("Block 5 Text Sequence")]
    [Tooltip("0-based index in Entries that should play the mini sequence.")]
    [SerializeField] private int block5Index = 4;
    [SerializeField] private TMP_Text block5Text1;
    [SerializeField] private TMP_Text block5Text2;
    [SerializeField] private TMP_Text block5Text3;
    [SerializeField] private float block5TextSwapDuration = 2f;
    [SerializeField] private string[] block5Text1Values = new string[]
    {
        "TITLE SCREEN",
        "FLASHBACK",
        "DRIVE",
        "ENDING"
    };
    [SerializeField] private string[] block5Text2Values = new string[0];
    [SerializeField] private string[] block5Text3Values = new string[0];

    private Coroutine running;

    private void Awake()
    {
        if (entries.Count == 0 && container != null)
        {
            for (int i = 0; i < container.childCount; i++)
            {
                entries.Add(container.GetChild(i).gameObject);
            }
        }
    }

    private void OnEnable()
    {
        StartSequence();
    }

    private void OnDisable()
    {
        StopSequence();
    }

    public void StartSequence()
    {
        if (running != null)
        {
            StopCoroutine(running);
        }

        running = StartCoroutine(Sequence());
    }

    public void StopSequence()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        SetAllActive(false);
    }

    private IEnumerator Sequence()
    {
        if (entries.Count == 0)
        {
            yield break;
        }

        do
        {
            SetAllActive(false);
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                SetAllActive(false);

                if (i == block5Index && block5Text1Values.Length > 0)
                {
                    if (entries[i] != null)
                    {
                        entries[i].SetActive(true);
                    }

                    yield return PlayBlock5TextSequence();
                }
                else if (entries[i] != null)
                {
                    entries[i].SetActive(true);
                }

                if (i != block5Index || block5Text1Values.Length == 0)
                {
                    float wait = GetDuration(i);
                    yield return new WaitForSeconds(wait);
                }

                SetAllActive(false);
                if (betweenDelay > 0f && (loop || i < entries.Count - 1))
                {
                    yield return new WaitForSeconds(betweenDelay);
                }
            }
        }
        while (loop);

        SetAllActive(false);
        running = null;
    }

    private float GetDuration(int index)
    {
        if (durations != null && durations.Length > 0)
        {
            if (index < durations.Length)
            {
                return Mathf.Max(0f, durations[index]);
            }

            return Mathf.Max(0f, durations[durations.Length - 1]);
        }

        return Mathf.Max(0f, defaultDuration);
    }

    private void SetAllActive(bool active)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                entries[i].SetActive(active);
            }
        }
    }

    private IEnumerator PlayBlock5TextSequence()
    {
        float duration = Mathf.Max(0f, block5TextSwapDuration);
        int steps = Mathf.Max(block5Text1Values.Length, block5Text2Values.Length);

        for (int i = 0; i < steps; i++)
        {
            if (block5Text1 != null && block5Text1Values.Length > 0)
            {
                block5Text1.text = GetSequenceValue(block5Text1Values, i);
            }

            if (block5Text2 != null && block5Text2Values.Length > 0)
            {
                block5Text2.text = GetSequenceValue(block5Text2Values, i);
            }

            if (block5Text3 != null && block5Text3Values.Length > 0)
            {
                block5Text3.text = GetSequenceValue(block5Text3Values, i);
            }

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }
    }

    private string GetSequenceValue(string[] values, int index)
    {
        if (values == null || values.Length == 0)
        {
            return string.Empty;
        }

        if (index < values.Length)
        {
            return values[index];
        }

        return values[values.Length - 1];
    }
}
