using UnityEngine;
using UnityEngine.SceneManagement;
using MicrophoneInput;

/// <summary>
/// Attach to a GameObject near the museum exit.
/// Once the tour is complete (MuseumMicrophone.TourComplete), it watches whether
/// the player is within range and shows/hides a prompt UI accordingly.
/// Pressing E while in range loads MuseumCredits.
/// </summary>
public class MuseumExitPortal : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player Transform used for distance checks.")]
    public Transform player;
    [Tooltip("UI GameObject to show when the player is close enough (e.g. a 'Press E to continue' prompt).")]
    public GameObject promptUI;

    [Header("Settings")]
    [Tooltip("How close the player needs to be to see the prompt.")]
    public float activationRadius = 4f;
    public string sceneToLoad = "MuseumCredits";

    private bool _playerInRange;

    private void Update()
    {
        if (!MuseumMicrophone.TourComplete) return;
        if (player == null) return;

        bool inRange = Vector3.Distance(transform.position, player.position) <= activationRadius;

        if (inRange != _playerInRange)
        {
            _playerInRange = inRange;
            if (promptUI != null) promptUI.SetActive(_playerInRange);
        }

        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
            SceneManager.LoadScene(sceneToLoad);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
