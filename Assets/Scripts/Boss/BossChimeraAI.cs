using System.Collections;
using UnityEngine;

/// <summary>
/// BOSS 3 — FOREST CHIMERA
/// Identity: Berserker Predator. Adapts and escalates — each phase adds a new weapon.
///
/// Phase 1 "Beast Mode"   (HP 100→66%):
///   Charges directly at the player (fast dash), then throws 1 arcing projectile.
///   Pure melee aggressor — get too close and you're hit hard.
///
/// Phase 2 "Gunner Mode"  (HP 66→33%):
///   Keeps a safe distance. Fires 4 bursts of 5 bullets (80° spread) as a mid-range suppressor.
///   If player closes in, backs away before shooting.
///
/// Phase 3 "Berserk"      (HP <33%):
///   2-second invincibility on transition + screen-flash VFX.
///   Alternates rapidly between a sprint-charge AND a multi-burst volley every ~0.8 s.
///   Contact damage triples. Nobody is safe anywhere.
///
/// Required Components: BossHealth, EnemyPathfinding, Rigidbody2D, Flash, Knockback, PickupSpawner
/// Assign in Inspector:
///   p1ProjectilePrefab     → BossGrapeProjectile prefab (arc throw after charge)
///   p2BulletPrefab         → Bullet prefab  (isEnemyProjectile = true)
///   phaseTransitionVFXPrefab → particle/flash prefab for Phase 3 entry
/// </summary>
[RequireComponent(typeof(BossHealth))]
[RequireComponent(typeof(EnemyPathfinding))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossChimeraAI : MonoBehaviour
{
    // ── Phase 1 — Beast ───────────────────────────────────────────────────────
    [Header("Phase 1 — Beast (Charge + Arc Throw)")]
    [SerializeField] private float  p1ChargeSpeed    = 10f;
    [SerializeField] private float  p1ChargeDuration = 0.35f;
    [SerializeField] private float  p1ChargeCooldown = 2.2f;
    [SerializeField] private float  p1ChaseDistance  = 2.5f;  // begin charge when this close
    [SerializeField] private GameObject p1ProjectilePrefab;   // arcing projectile

    // ── Phase 2 — Gunner ──────────────────────────────────────────────────────
    [Header("Phase 2 — Gunner (Keep Distance + Multi-Burst)")]
    [SerializeField] private GameObject p2BulletPrefab;
    [SerializeField] private float  p2BulletSpeed      = 5.5f;
    [SerializeField] private int    p2BurstCount       = 4;
    [SerializeField] private int    p2ProjPerBurst     = 5;
    [SerializeField] private float  p2AngleSpread      = 80f;
    [SerializeField] private float  p2BetweenBursts    = 0.3f;
    [SerializeField] private float  p2AttackRange      = 7f;
    [SerializeField] private float  p2KeepDistance     = 4f;
    [SerializeField] private float  p2Cooldown         = 1.2f;

    // ── Phase 3 — Berserk ─────────────────────────────────────────────────────
    [Header("Phase 3 — Berserk (Everything x2)")]
    [SerializeField] private float  p3ChargeSpeed      = 14f;
    [SerializeField] private float  p3ChargeCooldown   = 0.7f;
    [SerializeField] private float  p3BulletSpeed      = 9f;
    [SerializeField] private int    p3ContactDamage    = 3;
    [SerializeField] private GameObject phaseTransitionVFXPrefab;

    // ── Shared ────────────────────────────────────────────────────────────────
    [Header("Shared")]
    [SerializeField] private int normalContactDamage = 1;

    // ── Private state ─────────────────────────────────────────────────────────
    private BossHealth       bossHealth;
    private EnemyPathfinding pathfinding;
    private SpriteRenderer   spriteRenderer;
    private Rigidbody2D      rb;

    private bool  isActing;
    private bool  p3NextIsCharge = true;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        bossHealth     = GetComponent<BossHealth>();
        pathfinding    = GetComponent<EnemyPathfinding>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb             = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()  => bossHealth.OnPhaseChanged += HandlePhaseChange;
    private void OnDisable() => bossHealth.OnPhaseChanged -= HandlePhaseChange;

    private void Start()
    {
        BossSequenceManager.Instance?.ShowBossBanner(2);
        StartCoroutine(ActionLoop());
    }

    // ── Main action loop ──────────────────────────────────────────────────────

    private IEnumerator ActionLoop()
    {
        while (!bossHealth.IsDead)
        {
            if (!isActing && PlayerController.Instance != null)
            {
                switch (bossHealth.CurrentPhase)
                {
                    case 1: yield return StartCoroutine(Phase1Action()); break;
                    case 2: yield return StartCoroutine(Phase2Action()); break;
                    default: yield return StartCoroutine(Phase3Action()); break;
                }
            }
            yield return null;
        }
    }

    // ── Phase 1: Sprint → Charge → Throw ─────────────────────────────────────

    private IEnumerator Phase1Action()
    {
        isActing = true;

        // Chase player until close enough to charge
        while (PlayerController.Instance != null &&
               Vector2.Distance(transform.position, PlayerController.Instance.transform.position) > p1ChaseDistance)
        {
            Vector2 dir = (PlayerController.Instance.transform.position - transform.position).normalized;
            pathfinding.MoveTo(dir);
            FlipToPlayer();
            yield return null;
        }

        pathfinding.StopMoving();
        yield return new WaitForSeconds(0.15f); // brief windup crouch

        // Charge dash
        yield return StartCoroutine(ChargeDash(p1ChargeSpeed, p1ChargeDuration));

        // Arc throw after charge
        if (p1ProjectilePrefab != null)
            Instantiate(p1ProjectilePrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(p1ChargeCooldown);
        isActing = false;
    }

    // ── Phase 2: Back away → Burst shoot ─────────────────────────────────────

    private IEnumerator Phase2Action()
    {
        isActing = true;

        // Back away if player is too close
        while (PlayerController.Instance != null &&
               Vector2.Distance(transform.position, PlayerController.Instance.transform.position) < p2KeepDistance)
        {
            Vector2 backDir = (transform.position - PlayerController.Instance.transform.position).normalized;
            pathfinding.MoveTo(backDir);
            FlipToPlayer();
            yield return null;
        }

        pathfinding.StopMoving();

        float dist = PlayerController.Instance != null
            ? Vector2.Distance(transform.position, PlayerController.Instance.transform.position)
            : float.MaxValue;

        if (dist <= p2AttackRange)
            yield return StartCoroutine(BurstShootRoutine(p2BurstCount, p2ProjPerBurst, p2AngleSpread, p2BulletSpeed, p2BetweenBursts));

        yield return new WaitForSeconds(p2Cooldown);
        isActing = false;
    }

    // ── Phase 3: Alternating charge / shoot at twice the pace ────────────────

    private IEnumerator Phase3Action()
    {
        isActing = true;

        if (p3NextIsCharge)
        {
            // Sprint briefly then dash
            if (PlayerController.Instance != null)
            {
                Vector2 dir = (PlayerController.Instance.transform.position - transform.position).normalized;
                pathfinding.MoveTo(dir);
                FlipToPlayer();
                yield return new WaitForSeconds(0.1f);
            }
            pathfinding.StopMoving();
            yield return StartCoroutine(ChargeDash(p3ChargeSpeed, p1ChargeDuration));
        }
        else
        {
            pathfinding.StopMoving();
            yield return StartCoroutine(BurstShootRoutine(2, p2ProjPerBurst, p2AngleSpread, p3BulletSpeed, 0.12f));
        }

        p3NextIsCharge = !p3NextIsCharge;
        yield return new WaitForSeconds(p3ChargeCooldown);
        isActing = false;
    }

    // ── Sub-routines ──────────────────────────────────────────────────────────

    private IEnumerator ChargeDash(float speed, float duration)
    {
        if (PlayerController.Instance == null) yield break;
        Vector2 dir     = (PlayerController.Instance.transform.position - transform.position).normalized;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        ScreenShakeManager.Instance?.ShakeScreen();
    }

    private IEnumerator BurstShootRoutine(int bursts, int perBurst, float spread, float speed, float between)
    {
        for (int i = 0; i < bursts; i++)
        {
            if (PlayerController.Instance == null) break;

            Vector2 toPlayer   = PlayerController.Instance.transform.position - transform.position;
            float   center     = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float   halfSpread = spread / 2f;
            float   step       = perBurst > 1 ? spread / (perBurst - 1) : 0f;

            for (int j = 0; j < perBurst; j++)
            {
                float angle = perBurst > 1 ? center - halfSpread + j * step : center;
                SpawnBullet(angle, speed);
            }

            yield return new WaitForSeconds(between);
        }
    }

    private void SpawnBullet(float angleDeg, float speed)
    {
        if (p2BulletPrefab == null) return;
        float rad      = angleDeg * Mathf.Deg2Rad;
        Vector2 spawnPos = (Vector2)transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 0.4f;
        GameObject bullet = Instantiate(p2BulletPrefab, spawnPos, Quaternion.identity);
        bullet.transform.right = bullet.transform.position - transform.position;
        if (bullet.TryGetComponent(out Projectile p)) p.UpdateMoveSpeed(speed);
    }

    private void FlipToPlayer()
    {
        if (spriteRenderer == null || PlayerController.Instance == null) return;
        spriteRenderer.flipX = PlayerController.Instance.transform.position.x < transform.position.x;
    }

    // ── Phase transition ──────────────────────────────────────────────────────

    private void HandlePhaseChange(int phase)
    {
        ScreenShakeManager.Instance?.ShakeScreen();
        FindObjectOfType<BossHealthBarUI>()?.UpdatePhaseText(phase);

        if (phase == 3)
            StartCoroutine(Phase3TransitionRoutine());
    }

    private IEnumerator Phase3TransitionRoutine()
    {
        // Grant invincibility during the cinematic transition
        bossHealth.SetInvincible(true);
        pathfinding.StopMoving();
        isActing = true;

        if (phaseTransitionVFXPrefab != null)
            Instantiate(phaseTransitionVFXPrefab, transform.position, Quaternion.identity);

        ScreenShakeManager.Instance?.ShakeScreen();
        yield return new WaitForSeconds(1.5f);

        bossHealth.SetInvincible(false);
        isActing = false;
    }

    // ── Contact damage ────────────────────────────────────────────────────────

    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent(out PlayerHealth ph))
        {
            int dmg = bossHealth.CurrentPhase >= 3 ? p3ContactDamage : normalContactDamage;
            ph.TakeDamage(dmg, transform);
        }
    }
}
