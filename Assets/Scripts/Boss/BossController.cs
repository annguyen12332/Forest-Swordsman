using System.Collections;
using UnityEngine;

/// <summary>
/// Core controller for the Death Boss. Handles movement, aggro, attacking,
/// summoning minions, taking damage, and death.
/// Replacement for g_02's DeathBossController – uses Vector2.Distance instead of A*.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Flash))]
[RequireComponent(typeof(Knockback))]
public class BossController : MonoBehaviour
{
  [SerializeField] private BossStatsSO stats;
  [SerializeField] private GameObject minionPrefab;
  [SerializeField] private Transform minionSpawnPoint;
  [SerializeField] private GameObject deathVFXPrefab;

  private enum BossState { Idle, Chasing, Attacking, Summoning, Dead }

  private BossState state = BossState.Idle;
  private Rigidbody2D rb;
  private SpriteRenderer spriteRenderer;
  private Animator animator;
  private Flash flash;
  private Knockback knockback;
  private EnemyHealthBar healthBar;

  private int currentHealth;
  private float nextAttackTime;
  private float nextSummonTime;

  // Animator parameter hashes
  private static readonly int ANIM_ATTACK  = Animator.StringToHash("Attack_1");
  private static readonly int ANIM_SUMMON  = Animator.StringToHash("Summon");
  private static readonly int ANIM_DEATH   = Animator.StringToHash("Death");
  private static readonly int ANIM_WALKING = Animator.StringToHash("Walking");

  private void Awake()
  {
    rb             = GetComponent<Rigidbody2D>();
    flash          = GetComponent<Flash>();
    knockback      = GetComponent<Knockback>();
    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    animator       = GetComponentInChildren<Animator>();
    healthBar      = GetComponentInChildren<EnemyHealthBar>();
  }

  private void Start()
  {
    currentHealth  = stats.maxHealth;
    nextSummonTime = Time.time + stats.firstSummonDelay;
    healthBar?.SetMaxHealth(stats.maxHealth);
  }

  private void Update()
  {
    if (state == BossState.Dead) { return; }
    if (PlayerController.Instance == null)  { return; }

    UpdateState();
  }

  private void FixedUpdate()
  {
    if (state == BossState.Dead || knockback.GettingKnockedBack) { return; }

    if (state == BossState.Chasing)
    {
      MoveTowardPlayer();
    }
    else
    {
      rb.linearVelocity = Vector2.zero;
    }
  }

  // ── State machine ────────────────────────────────────────────────────

  private void UpdateState()
  {
    float dist = DistanceToPlayer();

    if (dist > stats.chaseRange)
    {
      SetState(BossState.Idle);
      return;
    }

    // Summon takes priority over attack when off cooldown
    if (Time.time >= nextSummonTime && state != BossState.Summoning)
    {
      StartCoroutine(SummonRoutine());
      return;
    }

    if (dist <= stats.attackRange && Time.time >= nextAttackTime)
    {
      StartCoroutine(AttackRoutine());
      return;
    }

    if (dist <= stats.chaseRange && state != BossState.Attacking && state != BossState.Summoning)
    {
      SetState(BossState.Chasing);
    }
  }

  private void SetState(BossState newState)
  {
    state = newState;
    animator?.SetBool(ANIM_WALKING, newState == BossState.Chasing);
  }

  // ── Movement ─────────────────────────────────────────────────────────

  private void MoveTowardPlayer()
  {
    Vector2 dir = (PlayerController.Instance.transform.position - transform.position).normalized;
    rb.MovePosition(rb.position + dir * (stats.moveSpeed * Time.fixedDeltaTime));

    // Flip sprite based on movement direction
    if (dir.x < 0) { spriteRenderer.flipX = true; }
    else if (dir.x > 0) { spriteRenderer.flipX = false; }
  }

  // ── Combat ────────────────────────────────────────────────────────────

  private IEnumerator AttackRoutine()
  {
    SetState(BossState.Attacking);
    animator?.SetTrigger(ANIM_ATTACK);
    nextAttackTime = Time.time + stats.attackCooldown;

    yield return new WaitForSeconds(stats.attackCooldown * 0.5f);
    SetState(BossState.Chasing);
  }

  private IEnumerator SummonRoutine()
  {
    SetState(BossState.Summoning);
    animator?.SetTrigger(ANIM_SUMMON);
    nextSummonTime = Time.time + stats.summonCooldown;

    yield return new WaitForSeconds(0.8f);
    SpawnMinion();

    yield return new WaitForSeconds(0.5f);
    SetState(BossState.Chasing);
  }

  private void SpawnMinion()
  {
    if (minionPrefab == null || minionSpawnPoint == null) { return; }
    Instantiate(minionPrefab, minionSpawnPoint.position, Quaternion.identity);
  }

  // ── Damage & Death ────────────────────────────────────────────────────

  /// <summary>Called by DamageSource or weapon hitbox.</summary>
  public void TakeDamage(int damage)
  {
    if (state == BossState.Dead) { return; }

    currentHealth -= damage;
    healthBar?.TakeDamage(currentHealth);
    knockback.GetKnockedBack(PlayerController.Instance.transform, stats.knockbackThrust);
    StartCoroutine(flash.FlashRoutine());

    if (currentHealth <= 0)
    {
      StartCoroutine(DieRoutine());
    }
  }

  private IEnumerator DieRoutine()
  {
    state = BossState.Dead;
    rb.linearVelocity = Vector2.zero;
    animator?.SetTrigger(ANIM_DEATH);

    if (deathVFXPrefab != null)
    {
      Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
    }

    yield return new WaitForSeconds(1.2f);
    Destroy(gameObject);
  }

  // ── Helpers ───────────────────────────────────────────────────────────

  private float DistanceToPlayer()
  {
    if (PlayerController.Instance == null) { return float.MaxValue; }
    return Vector2.Distance(transform.position, PlayerController.Instance.transform.position);
  }

  /// <summary>Called from BossAttackHitbox when player enters melee range.</summary>
  public void OnPlayerInMeleeRange(bool inRange)
  {
    if (state == BossState.Dead) { return; }
    if (inRange && Time.time >= nextAttackTime)
    {
      StartCoroutine(AttackRoutine());
    }
  }

  /// <summary>Deals melee damage to player. Called via Animation Event on attack frame.</summary>
  public void DealMeleeDamage()
  {
    if (PlayerController.Instance == null) { return; }

    float dist = DistanceToPlayer();
    if (dist <= stats.attackRange + 0.3f)
    {
      PlayerHealth.Instance.TakeDamage(stats.attackDamage, transform);
    }
  }
}
