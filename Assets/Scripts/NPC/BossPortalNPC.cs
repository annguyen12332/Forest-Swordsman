using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NPC placed in the Town scene that acts as a portal to the Boss scene.
/// When the player enters the trigger zone, a dialogue message is displayed.
/// Pressing F triggers a fade-to-black transition and loads the boss scene.
///
/// SETUP in Inspector:
///  - bossSceneName : exact name of your Boss scene (e.g. "BossScene")
///  - dialogueText  : a TextMeshProUGUI component (world-space or screen-space canvas)
///                    that sits above the NPC. Start it disabled; this script enables it.
/// </summary>
public class BossPortalNPC : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string bossSceneName = "BossScene";

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string hintLine = "Help me save my world";
    [SerializeField] private string interactLine = "[F] Enter the Boss Realm";

    private bool isPlayerInRange = false;
    private bool isTransitioning  = false;

    private void Start()
    {
        if (dialogueText != null)
            dialogueText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerInRange && !isTransitioning && Input.GetKeyDown(KeyCode.F))
        {
            isTransitioning = true;
            UIFade.Instance?.FadeToBlack();
            StartCoroutine(LoadBossSceneRoutine());
        }
    }

    private IEnumerator LoadBossSceneRoutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(bossSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (dialogueText != null)
            {
                dialogueText.gameObject.SetActive(true);
                dialogueText.text = $"{hintLine}\n{interactLine}";
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (dialogueText != null)
                dialogueText.gameObject.SetActive(false);
        }
    }
}
