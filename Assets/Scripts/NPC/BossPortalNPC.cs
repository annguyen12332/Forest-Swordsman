using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NPC in the Town scene that shows dialogue "Help me save my world"
/// and teleports the player to Scene_Boss when they press F.
///
/// Setup:
///  1. Create a GameObject in Town scene, add this script.
///  2. Add a CircleCollider2D (IsTrigger = true) for interaction range.
///  3. Assign interactHintObject  → a child object with a "Press [F]" label.
///  4. Assign dialogueObject      → a child object with "Help me save my world" text.
///  5. Set bossSceneName to "Scene_Boss" (default already set).
/// </summary>
public class BossPortalNPC : MonoBehaviour
{
    [Header("Scene to Load")]
    [SerializeField] private string bossSceneName = "Scene_Boss";

    [Header("UI Objects (child GameObjects)")]
    [Tooltip("Object that shows 'Press [F]' hint when player is nearby")]
    [SerializeField] private GameObject interactHintObject;
    [Tooltip("Object that shows NPC dialogue text")]
    [SerializeField] private GameObject dialogueObject;

    [Header("Timing")]
    [SerializeField] private float sceneLoadDelay = 1f;

    private bool isPlayerInRange;
    private bool isLoading;

    private void Start()
    {
        if (interactHintObject != null) interactHintObject.SetActive(false);
        if (dialogueObject     != null) dialogueObject.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerInRange && !isLoading && Input.GetKeyDown(KeyCode.F))
            StartCoroutine(LoadBossSceneRoutine());
    }

    private IEnumerator LoadBossSceneRoutine()
    {
        isLoading = true;

        if (dialogueObject != null) dialogueObject.SetActive(true);
        if (interactHintObject != null) interactHintObject.SetActive(false);

        UIFade.Instance?.FadeToBlack();
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(bossSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = true;
        if (interactHintObject != null) interactHintObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
        if (interactHintObject != null) interactHintObject.SetActive(false);
        if (dialogueObject     != null) dialogueObject.SetActive(false);
        isLoading = false;
    }
}
