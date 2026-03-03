using UnityEngine;

/// <summary>
/// Placed at the entrance of the boss room. When the player walks in,
/// the boss is activated and the entrance is sealed (optional door/gate object).
/// Attach to a GameObject with a BoxCollider2D (isTrigger = true).
/// </summary>
public class BossRoomTrigger : MonoBehaviour
{
  [SerializeField] private BossController boss;
  [SerializeField] private GameObject entranceDoor;
  [SerializeField] private GameObject exitDoor;

  private bool triggered = false;

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (triggered) { return; }
    if (!other.CompareTag("Player")) { return; }

    triggered = true;
    ActivateBossRoom();
  }

  private void ActivateBossRoom()
  {
    if (boss != null)
    {
      boss.gameObject.SetActive(true);
    }

    if (entranceDoor != null)
    {
      entranceDoor.SetActive(true);
    }

    // Exit door opens only when boss is defeated (handled via Unity Event or boss death callback)
    if (exitDoor != null)
    {
      exitDoor.SetActive(false);
    }

    // Disable this trigger so it only fires once
    gameObject.SetActive(false);
  }
}
