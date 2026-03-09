using System.Collections;
using UnityEngine;

/// <summary>
/// BOSS 1 — SLIME KING
/// Identity: Bullet-hell Artillery Tank. Huge, slow, sprays bullets everywhere.
///
/// Phase 1 (HP 100→66%): Calm burst shooter — 3 bursts of 3 bullets, 45° spread.
/// Phase 2 (HP 66→33%): Enraged — 5 bursts of 5 bullets, 90° spread, doubled bullet speed.
///                       After each attack pattern, spawns 2 mini-slimes.
/// Phase 3 (HP <33%):    Berserk — 360° volley (12 bullets) every 1.5 s
///                       + Ground Slam every 5 s (AoE ring that knocks back & damages player).
///
/// Required Components on prefab: BossHealth, EnemyPathfinding, Flash, Knockback, PickupSpawner
/// Assign in Inspector: bulletPrefab (Bullet), miniSlimePrefab (Slime), slamAoePrefab (BossGroundSlamAoE prefab)
/// </summary>
[RequireComponent(typeof(BossHealth))]
[RequireComponent(typeof(EnemyPathfinding))]
public class BossSlimeKingAI : MonoBehaviour
{
    // ── Phase 1 ───────────────────────────────────────────────────────────────
    [Header("Phase 1 — Steady Burst")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float  p1BulletSpeed        = 4f;
    [SerializeField] private int    p1BurstCount         = 3;
    [SerializeField] private int    p1ProjPerBurst       = 3;
    [SerializeField] private float  p1AngleSpread        = 45f;
    [SerializeField] private float  p1TimeBetweenBursts  = 0.4f;
    [SerializeField] private float  p1RestTime           = 2.2f;

    // ── Phase 2 ───────────────────────────────────────────────────────────────
    [Header("Phase 2 — Aggressive + Summon")]
    [SerializeField] private float  p2BulletSpeed        = 7f;
    [SerializeField] private int    p2BurstCount         = 5;
    [SerializeField] private int    p2ProjPerBurst       = 5;
    [SerializeField] private float  p2AngleSpread        = 90f;
    [SerializeField] private float  p2TimeBetweenBursts  = 0.2f;
    [SerializeField] private float  p2RestTime           = 1.5f;
    [SerializeField] private GameObject miniSlimePrefab;
    [SerializeField] private int    miniSlimesPerSummon  = 2;

    // ── Phase 3 ───────────────────────────────────────────────────────────────
    [Header("Phase 3 — Berserk 360 + Ground Slam")]
    [SerializeField] private float  p3BulletSpeed        = 8f;
    [SerializeField] private int    p3VollWeight         = 12;   // bullets per volley
    [SerializeField] private float  p3RestTime           = 1.5f;
    [SerializeField] private GameObject slamAoePrefab;
    [SerializeField] private float  slamInterval         = 5f;

    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float attackRange = 5.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private BossHealth       bossHealth;
    private EnemyPathfinding pathfinding;
    private SpriteRenderer   spriteRenderer;

    private bool  isShooting;
    private bool  isSlamming;
    private float slamTimer;

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
        BossSequenceManager.Instance?.ShowBossBanner(0);
    }

    private void Update()
    {
        if (bossHealth.IsDead || PlayerController.Instance == null) return;

        if (!isSlamming)
            ChasePlayer();

        if (!isShooting)
        {
            float dist = Vector2.Distance(transform.position, PlayerController.Instance.transform.position);
            if (dist <= attackRange)
                StartCoroutine(ShootRoutine());
        }

        // Phase 3: periodic ground slam
        if (bossHealth.CurrentPhase >= 3)
        {
            slamTimer += Time.deltaTime;
            if (slamTimer >= slamInterval && !isSlamming)
            {
                slamTimer = 0f;
                StartCoroutine(GroundSlamRoutine());
            }
        }
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void ChasePlayer()
    {
        Vector2 dir = (PlayerController.Instance.transform.position - transform.position).normalized;
        pathfinding.MoveTo(dir);
        spriteRenderer.flipX = dir.x < 0;
    }

    // ── Shooting ──────────────────────────────────────────────────────────────

    private IEnumerator ShootRoutine()
    {
        isShooting = true;

        if (bossHealth.CurrentPhase >= 3)
        {
            // 360° volley
            SpawnRadialVolley(p3VollWeight, p3BulletSpeed);
            yield return new WaitForSeconds(p3RestTime);
        }
        else
        {
            int   bursts     = bossHealth.CurrentPhase == 1 ? p1BurstCount        : p2BurstCount;
            int   perBurst   = bossHealth.CurrentPhase == 1 ? p1ProjPerBurst      : p2ProjPerBurst;
            float spread     = bossHealth.CurrentPhase == 1 ? p1AngleSpread       : p2AngleSpread;
            float between    = bossHealth.CurrentPhase == 1 ? p1TimeBetweenBursts : p2TimeBetweenBursts;
            float rest       = bossHealth.CurrentPhase == 1 ? p1RestTime          : p2RestTime;
            float speed      = bossHealth.CurrentPhase == 1 ? p1BulletSpeed       : p2BulletSpeed;

            for (int i = 0; i < bursts; i++)
            {
                if (PlayerController.Instance == null) break;
                SpawnBurstAtPlayer(perBurst, spread, speed);
                yield return new WaitForSeconds(between);
            }

            // Phase 2: summon mini-slimes after the attack pattern
            if (bossHealth.CurrentPhase == 2)
                SummonMiniSlimes();

            yield return new WaitForSeconds(rest);
        }

        isShooting = false;
    }

    private void SpawnBurstAtPlayer(int count, float totalSpread, float speed)
    {
        if (bulletPrefab == null || PlayerController.Instance == null) return;

        Vector2 toPlayer   = PlayerController.Instance.transform.position - transform.position;
        float   center     = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        float   halfSpread = totalSpread / 2f;
        float   step       = count > 1 ? totalSpread / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? center - halfSpread + i * step : center;
            SpawnBullet(angle, speed);
        }
    }

    private void SpawnRadialVolley(int count, float speed)
    {
        if (bulletPrefab == null) return;
        float step = 360f / count;
        for (int i = 0; i < count; i++)
            SpawnBullet(i * step, speed);
    }

    private void SpawnBullet(float angleDeg, float speed)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 spawnPos = (Vector2)transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 0.4f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.transform.right = bullet.transform.position - transform.position;
        if (bullet.TryGetComponent(out Projectile p)) p.UpdateMoveSpeed(speed);
    }

    // ── Ground Slam (Phase 3) ─────────────────────────────────────────────────

    private IEnumerator GroundSlamRoutine()
    {
        isSlamming = true;
        pathfinding.StopMoving();

        // Short windup — boss "charges up"
        yield return new WaitForSeconds(0.6f);

        ScreenShakeManager.Instance?.ShakeScreen();
        if (slamAoePrefab != null)
            Instantiate(slamAoePrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.4f);
        isSlamming = false;
    }

    // ── Summon ────────────────────────────────────────────────────────────────

    private void SummonMiniSlimes()
    {
        if (miniSlimePrefab == null) return;
        for (int i = 0; i < miniSlimesPerSummon; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 2.5f;
            Instantiate(miniSlimePrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }

    // ── Phase transition ──────────────────────────────────────────────────────

    private void HandlePhaseChange(int phase)
    {
        ScreenShakeManager.Instance?.ShakeScreen();
        FindObjectOfType<BossHealthBarUI>()?.UpdatePhaseText(phase);
    }

    // ── Contact damage (player walks into boss body) ──────────────────────────

    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent(out PlayerHealth ph))
            ph.TakeDamage(1, transform);
    }
}
