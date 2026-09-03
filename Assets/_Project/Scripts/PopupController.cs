using UnityEngine;
using UnityTutorial.PlayerControl;

public class PopupController : MonoBehaviour
{
    public GameObject popupPanel;

    private void Start()
    {
        if (popupPanel != null && popupPanel.activeSelf)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SetUIMode(true);
        }
    }

    public void OpenPopup()
    {
        popupPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetUIMode(true);
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        SetUIMode(false);
    }

    private void SetUIMode(bool value)
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.uiMode = value;

        var fpc = FindFirstObjectByType<FlashbackPlayerController>();
        if (fpc != null) fpc.uiMode = value;
    }
}
