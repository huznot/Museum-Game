using UnityEngine;

public class SimpleMouth : MonoBehaviour
{
    public Transform mouthRect; // Assign your black rectangle Transform

    [Header("Height Settings")]
    public float minHeight = 0.009149482f;
    public float maxHeight = 0.03172309f;

    [Header("Width Wiggle Settings")]
    public float baseWidth = 0.02f;         // Starting width of mouth
    public float widthWiggleAmount = 0.005f; // How much it can stretch horizontally
    public float widthWiggleSpeed = 2f;      // Wiggle speed

    [Header("Speaking Control")]
    public float speed = 5f;    // Mouth open/close speed
    public bool isSpeaking = false;

    void Update()
    {
        Vector3 scale = mouthRect.localScale;

        if (isSpeaking)
        {
            // Height oscillation
            scale.y = Mathf.Lerp(minHeight, maxHeight, (Mathf.Sin(Time.time * speed) + 1f) / 2f);

            // Slight width variation (optional wiggle)
            scale.x = baseWidth + Mathf.Sin(Time.time * widthWiggleSpeed) * widthWiggleAmount;
        }
        else
        {
            // Mouth closed
            scale.y = minHeight;
            scale.x = baseWidth;
        }

        mouthRect.localScale = scale;
    }
}
