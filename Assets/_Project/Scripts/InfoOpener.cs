using UnityEngine;
using UnityTutorial.PlayerControl;
using TMPro;

public class InfoOpener : MonoBehaviour
{
    public float interactRange = 3f;
    public PopupController popupController;

    [Header("Content")]
    public TMP_Text titleText;
    public TMP_Text infoText;
    public string titleContent;
    [TextArea] public string infoContent;

    private Transform player;
    private GameObject ePrompt;

    void Start()
    {
        Transform eChild = transform.Find("E");
        if (eChild != null)
            ePrompt = eChild.gameObject;

        FlashbackPlayerController controller = FindFirstObjectByType<FlashbackPlayerController>();
        if (controller != null)
            player = controller.transform;
    }

    void Update()
    {
        if (player == null || ePrompt == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inRange = dist <= interactRange;

        ePrompt.SetActive(inRange);

        if (inRange && Input.GetKeyDown(KeyCode.E))
        {
            if (popupController != null)
            {
                if (titleText != null) titleText.text = titleContent;
                if (infoText != null) infoText.text = infoContent;
                popupController.OpenPopup();
            }
        }
    }
}
