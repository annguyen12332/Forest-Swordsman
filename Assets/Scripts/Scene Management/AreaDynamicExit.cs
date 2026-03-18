using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger that transitions the player to another map.
/// Supports two modes:
///   1. Dynamic (same Unity Scene): swaps map content via MapLoader using JSON.
///   2. Cross-scene: loads a different Unity Scene (legacy, uses SceneManager).
/// </summary>
public class AreaDynamicExit : MonoBehaviour
{
  [Header("Target Map (Dynamic Mode)")]
  [Tooltip("Map ID to load – matches filename in Resources/Maps/ without .json extension.")]
  [SerializeField] private string targetMapId;

  [Tooltip("Name of the entrance in the target map where the player will spawn.")]
  [SerializeField] private string targetEntranceName;

  [Header("Cross-Scene Override (leave empty for dynamic mode)")]
  [Tooltip("If set, loads this Unity Scene instead of dynamically swapping map content.")]
  [SerializeField] private string targetSceneName;

  private bool isTransitioning = false;

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (isTransitioning) return;
    if (!other.gameObject.GetComponent<PlayerController>()) return;

    isTransitioning = true;
    StartCoroutine(TransitionRoutine());
  }

  private IEnumerator TransitionRoutine()
  {
    // Store transition name for legacy AreaEntrance support
    SceneManagement.Instance?.SetTransitionName(targetEntranceName);

    bool useCrossScene = !string.IsNullOrEmpty(targetSceneName);

    if (useCrossScene)
    {
      // Cross-scene: traditional Unity Scene load with fade
      UIFade.Instance?.FadeToBlack();
      yield return new WaitForSeconds(1f);
      SceneManager.LoadScene(targetSceneName);
    }
    else
    {
      // Dynamic: swap map content in-place via MapLoader
      if (MapLoader.Instance == null)
      {
        Debug.LogError("[AreaDynamicExit] MapLoader.Instance is null. Add MapLoader to the scene.");
        isTransitioning = false;
        yield break;
      }

      if (string.IsNullOrEmpty(targetMapId))
      {
        Debug.LogError("[AreaDynamicExit] targetMapId is not set.");
        isTransitioning = false;
        yield break;
      }

      MapLoader.Instance.LoadMap(targetMapId, targetEntranceName);
      yield return new WaitForSeconds(1.5f);
      isTransitioning = false;
    }
  }
}
