using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("Crosshair Images")]
    public Image circleImage;     // Your main circle sprite
    public Image dotImage;        // Your center dot sprite

    [Header("Settings")]
    public KeyCode leftClick = KeyCode.Mouse0;   // Left click
    public KeyCode rightClick = KeyCode.Mouse1;  // Right click

    [Header("Smooth Fade Settings")]
    public float fadeSpeed = 10f; // How fast the dot fades out

    private float targetAlpha;

    void Update()
    {
        // Check if either mouse button is held
        if (Input.GetKey(leftClick) || Input.GetKey(rightClick))
        {
            targetAlpha = 0f; // fade out dot
        }
        else
        {
            targetAlpha = 1f; // dot visible
        }

        // Smoothly fade the dot in/out
        Color dotColor = dotImage.color;
        dotColor.a = Mathf.Lerp(dotColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        dotImage.color = dotColor;
    }
}
