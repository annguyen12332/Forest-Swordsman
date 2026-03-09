using System.Collections;
using UnityEngine;

/// <summary>
/// BOSS 2 — ELDER GRAPE SHAMAN
/// Identity: Cunning Arcane Mage. Elusive, teleports, rains projectiles in complex patterns.
///
/// Phase 1 (HP 100→66%): Fan-throw — 3 arcing projectiles spread in a 60° fan toward player.
/// Phase 2 (HP 66→33%): Teleport Burst — teleports to a random position near player, fires
///                       a 5-projectile 120° fan, then summons 1 Grape minion.
/// Phase 3 (HP <33%):   Arcane Rain — brief windup, launches 8 projectiles in a ring that all
///                       arc and land around the player simultaneously. Summons 2 Grape minions.
///
/// Required Components: BossHealth, EnemyPathfinding, Flash, Knockback, PickupSpawner, SpriteRenderer
/// Assign in Inspector:
///   grapeProjectilePrefab → BossGrapeProjectile prefab (arc projectile)
///   grapeMinionPrefab     → normal Grape prefab (enemy)
/// </summary>
[RequireComponent(typeof(BossHealth))]
[RequireComponent(typeof(EnemyPathfinding))]
public class BossGrapeShamanAI : MonoBehaviour
{
    // ── Phase 1 ───────────────────────────────────────────────────────────────
    [Header("Phase 1 — Fan Throw")]
    [SerializeField] private GameObject grapeProjectilePrefab;
    [SerializeField] private int   p1FanCount     = 3;
    [SerializeField] private float p1FanSpread    = 60f;
    [SerializeField] private float p1Cooldown     = 2.5f;

    // ── Phase 2 ───────────────────────────────────────────────────────────────
    [Header("Phase 2 — Teleport + Wide Fan + Summon")]
    [SerializeField] private int   p2FanCount     = 5;
    [SerializeField] private float p2FanSpread    = 120f;
    [SerializeField] private float p2Cooldown     = 2f;
    [SerializeField] private float teleportRadius = 3f;
    [SerializeField] private GameObject grapeMinionPrefab;

    // ── Phase 3 ───────────────────────────────────────────────────────────────
    [Header("Phase 3 — Arcane Rain + Double Summon")]
    [SerializeField] private int   p3RainCount    = 8;
    [SerializeField] private float p3RainRadius   = 3.5f;
    [SerializeField] private float p3Cooldown     = 1.8f;
    [SerializeField] private int   p3MinionCount  = 2;

    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float attackRange = 6f;

    // ── Private state ─────────────────────────────────────────────────────────
    private BossHealth       bossHealth;
    private EnemyPathfinding pathfinding;
    private SpriteRenderer   spriteRenderer;
    private bool             isAttacking;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        bossHealth     = GetComponent<BossHealth>();
        pathfinding    = GetComponent<EnemyPathfinding>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()  => bossHealth.OnPhaseChanged += HandlePhaseChange;
    private void OnDisable() => bossHealth.OnPhaseChanged -= HandlePhaseChange;

    private void Start()
    {
        BossSequenceManager.Instance?.ShowBossBanner(1);
    }

    private void Update()
    {
        if (bossHealth.IsDead || PlayerController.Instance == null) return;

        if (!isAttacking)
        {
            Vector2 dir = (PlayerController.Instance.transform.position - transform.position).normalized;
            pathfinding.MoveTo(dir);
            spriteRenderer.flipX = PlayerController.Instance.transform.position.x < transform.position.x;

            float dist = Vector2.Distance(transform.position, PlayerController.Instance.transform.position);
            if (dist <= attackRange)
                StartCoroutine(AttackRoutine());
        }
    }

    // ── Attack coroutine dispatcher ───────────────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        pathfinding.StopMoving();

        switch (bossHealth.CurrentPhase)
        {
            case 1:
                yield return StartCoroutine(FanThrowRoutine(p1FanCount, p1FanSpread));
                yield return new WaitForSeconds(p1Cooldown);
                break;

            case 2:
                yield return StartCoroutine(TeleportRoutine());
                yield return StartCoroutine(FanThrowRoutine(p2FanCount, p2FanSpread));
                SummonMinions(1);
                yield return new WaitForSeconds(p2Cooldown);
                break;

            default: // Phase 3
                yield return StartCoroutine(ArcaneRainRoutine());
                SummonMinions(p3MinionCount);
                yield return new WaitForSeconds(p3Cooldown);
                break;
        }

        isAttacking = false;
    }

    // ── Phase 1 / 2 — Fan Throw ───────────────────────────────────────────────

    private IEnumerator FanThrowRoutine(int count, float totalSpread)
    {
        if (PlayerController.Instance == null) yield break;

        Vector2 toPlayer   = PlayerController.Instance.transform.position - transform.position;
        float   center     = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        float   halfSpread = totalSpread / 2f;
        float   step       = count > 1 ? totalSpread / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? center - halfSpread + i * step : center;
            float rad   = angle * Mathf.Deg2Rad;
            Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * 0.5f;

            if (grapeProjectilePrefab != null)
                Instantiate(grapeProjectilePrefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(0.1f);
        }
    }

    // ── Phase 3 — Arcane Rain ─────────────────────────────────────────────────

    private IEnumerator ArcaneRainRoutine()
    {
        if (PlayerController.Instance == null) yield break;

        // Dramatic windup — boss stands still and "charges" the spell
        yield return new WaitForSeconds(0.8f);
        ScreenShakeManager.Instance?.ShakeScreen();

        Vector3 playerPos = PlayerController.Instance.transform.position;
        float   angleStep = 360f / p3RainCount;

        // Spawn all rain projectiles simultaneously in a ring around player
        for (int i = 0; i < p3RainCount; i++)
        {
            float   rad     = i * angleStep * Mathf.Deg2Rad;
            Vector3 landPos = playerPos + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * p3RainRadius;

            if (grapeProjectilePrefab != null)
            {
                GameObject proj = Instantiate(grapeProjectilePrefab, transform.position, Quaternion.identity);
                // Override the landing target so projectiles rain into a ring
                if (proj.TryGetComponent(out BossGrapeProjectile bgp))
                    bgp.SetTarget(landPos);
            }
        }

        yield return new WaitForSeconds(0.5f);
    }

    // ── Phase 2 — Teleport ────────────────────────────────────────────────────

    private IEnumerator TeleportRoutine()
    {
        // Disappear
        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(0.3f);

        // Reposition to a random point near the player
        Vector2 offset = Random.insideUnitCircle.normalized * teleportRadius;
        transform.position = (Vector2)PlayerController.Instance.transform.position + offset;

        // Reappear
        spriteRenderer.enabled = true;
        yield return new WaitForSeconds(0.15f);
    }

    // ── Summon ────────────────────────────────────────────────────────────────

    private void SummonMinions(int count)
    {
        if (grapeMinionPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 2.5f;
            Instantiate(grapeMinionPrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }

    // ── Phase transition ──────────────────────────────────────────────────────

    private void HandlePhaseChange(int phase)
    {
        ScreenShakeManager.Instance?.ShakeScreen();
        FindObjectOfType<BossHealthBarUI>()?.UpdatePhaseText(phase);
    }

    // ── Contact damage ────────────────────────────────────────────────────────

    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent(out PlayerHealth ph))
            ph.TakeDamage(1, transform);
    }
}
