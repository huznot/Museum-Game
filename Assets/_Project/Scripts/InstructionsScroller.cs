using TMPro;
using UnityEngine;

public class InstructionsScroller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text instructionsText;

    [Header("Instructions")]
    [TextArea(2, 6)]
    [SerializeField] private string[] instructions;
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool wrap = false;

    private int _index;

    private void OnEnable()
    {
        _index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, instructions.Length - 1));
        UpdateText();
    }

    private void Update()
    {
        if (instructions == null || instructions.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move(-1);
        }
    }

    public void MoveNext()
    {
        Move(1);
    }

    public void MovePrevious()
    {
        Move(-1);
    }

    private void Move(int direction)
    {
        if (instructions == null || instructions.Length == 0)
            return;

        int nextIndex = _index + direction;
        if (wrap)
        {
            if (nextIndex < 0)
                nextIndex = instructions.Length - 1;
            else if (nextIndex >= instructions.Length)
                nextIndex = 0;
        }
        else
        {
            nextIndex = Mathf.Clamp(nextIndex, 0, instructions.Length - 1);
        }

        if (nextIndex == _index)
            return;

        _index = nextIndex;
        UpdateText();
    }

    private void UpdateText()
    {
        if (instructionsText == null)
            return;

        if (instructions == null || instructions.Length == 0)
        {
            instructionsText.text = string.Empty;
            return;
        }

        _index = Mathf.Clamp(_index, 0, instructions.Length - 1);
        instructionsText.text = instructions[_index];
    }
}
