using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Core health component for all bosses.
/// Replaces EnemyHealth. Supports 3-phase system, invincibility frames, and boss death events.
/// Attach alongside Flash, Knockback, PickupSpawner on each boss prefab.
/// </summary>
public class BossHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int startingHealth = 150;
    [SerializeField] private float knockbackThrust = 8f;

    [Header("Level Scaling")]
    [SerializeField] private int healthScalingFactor = 30; // HP gained per player level above 1

    [Header("Rewards")]
    [SerializeField] private int xpReward = 300; // XP awarded on death (higher than normal enemies)

    [Header("Phase Thresholds (fraction of max HP)")]
    [SerializeField] private float phase2Threshold = 0.66f;
    [SerializeField] private float phase3Threshold = 0.33f;

    [Header("Death")]
    [SerializeField] private GameObject deathVFXPrefab;

    // ── Public state ──────────────────────────────────────────────────────────
    public int  CurrentHealth  => currentHealth;
    public int  MaxHealth      => startingHealth;
    public float HPPercent     => startingHealth > 0 ? (float)currentHealth / startingHealth : 0f;
    public int  CurrentPhase   { get; private set; } = 1;
    public bool IsDead         { get; private set; }

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fires when HP crosses a phase threshold. Parameter = new phase (2 or 3).</summary>
    public event Action<int> OnPhaseChanged;
    /// <summary>Fires once when the boss object is about to be destroyed.</summary>
    public event Action      OnBossDied;

    // ── Private ───────────────────────────────────────────────────────────────
    private int  currentHealth;
    private bool isInvincible;
    private Knockback        knockback;
    private Flash            flash;
    private BossHealthBarUI  bossUI;

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        flash     = GetComponent<Flash>();
    }

    private void Start()
    {
        // Scale HP based on player level (same pattern as EnemyHealth)
        int playerLevel = (PlayerLevel.Instance != null) ? PlayerLevel.Instance.CurrentLevel : 1;
        startingHealth = startingHealth + (playerLevel - 1) * healthScalingFactor;

        currentHealth = startingHealth;
        // FindObjectOfType with includeInactive=true only finds objects whose parents are active. 
        // We use Resources.FindObjectsOfTypeAll to ensure we can find it even if the whole canvas is off.
        var uiElements = Resources.FindObjectsOfTypeAll<BossHealthBarUI>();
        if (uiElements.Length > 0)
        {
            bossUI = uiElements[0];
        }
        bossUI?.RegisterBoss(GetInstanceID(), startingHealth);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (IsDead || isInvincible || damage <= 0) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        bossUI?.UpdateBossHealth(GetInstanceID(), currentHealth);

        knockback?.GetKnockedBack(PlayerController.Instance.transform, knockbackThrust);
        if (flash != null) StartCoroutine(flash.FlashRoutine());

        CheckPhaseTransition();

        if (currentHealth <= 0)
            StartCoroutine(DieRoutine());
    }

    /// <summary>Call from boss AI to grant/remove invincibility frames (e.g. phase transitions).</summary>
    public void SetInvincible(bool invincible) => isInvincible = invincible;

    // ── Internals ─────────────────────────────────────────────────────────────

    private void CheckPhaseTransition()
    {
        if (CurrentPhase == 1 && HPPercent <= phase2Threshold)
        {
            CurrentPhase = 2;
            OnPhaseChanged?.Invoke(2);
        }
        else if (CurrentPhase == 2 && HPPercent <= phase3Threshold)
        {
            CurrentPhase = 3;
            OnPhaseChanged?.Invoke(3);
        }
    }

    private IEnumerator DieRoutine()
    {
        if (IsDead) yield break;
        IsDead = true;

        if (flash != null)
            yield return new WaitForSeconds(flash.GetRestoreMatTime());

        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // Boss drops more items than regular enemies
        GetComponent<PickupSpawner>()?.DropBossItems();

        // Grant XP reward
        if (PlayerLevel.Instance != null)
            PlayerLevel.Instance.AddXP(xpReward);

        // BossHealthBarUI tracks all bosses; only hides when the last one dies.
        bossUI?.UnregisterBoss(GetInstanceID());

        OnBossDied?.Invoke();
        Destroy(gameObject);
    }
}
