using UnityEngine;
using UnityEngine.UI; // works for UI.Text
using TMPro; // works for TextMeshProUGUI

public class PulseText : MonoBehaviour
{
    public float speed = 2f; 
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;

    Graphic textGraphic; // covers both Text and TMP

    void Awake()
    {
        textGraphic = GetComponent<Graphic>();
    }

    void Update()
    {
        if (textGraphic != null)
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f; 
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            Color c = textGraphic.color;
            c.a = alpha;
            textGraphic.color = c;
        }
    }
}
