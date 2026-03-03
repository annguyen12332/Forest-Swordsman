using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Destroys duplicate EventSystems at runtime, keeping only the first one found.
/// Attach to the same GameObject as EventSystem (usually in Managers prefab).
/// </summary>
public class EventSystemGuard : MonoBehaviour
{
  private void Awake()
  {
    var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

    if (allEventSystems.Length <= 1) { return; }

    // Keep the first EventSystem, destroy the rest
    for (int i = 1; i < allEventSystems.Length; i++)
    {
      Debug.Log($"[EventSystemGuard] Destroyed duplicate EventSystem on '{allEventSystems[i].gameObject.name}'.");
      Destroy(allEventSystems[i].gameObject);
    }
  }
}
