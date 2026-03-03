using UnityEngine;

/// <summary>
/// Trigger zone around the boss. When player steps inside, notifies BossController
/// to start melee attack. Acts as the hitbox detection for melee range.
/// Attach to a child GameObject with a CircleCollider2D (isTrigger = true).
/// </summary>
public class BossAttackHitbox : MonoBehaviour
{
  private BossController bossController;

  private void Awake()
  {
    bossController = GetComponentInParent<BossController>();
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (!other.CompareTag("Player")) { return; }
    bossController?.OnPlayerInMeleeRange(true);
  }

  private void OnTriggerExit2D(Collider2D other)
  {
    if (!other.CompareTag("Player")) { return; }
    bossController?.OnPlayerInMeleeRange(false);
  }
}
