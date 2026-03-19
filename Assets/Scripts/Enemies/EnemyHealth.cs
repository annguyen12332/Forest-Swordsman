using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 3;
    [SerializeField] private int xpReward = 50; // Lượng XP rớt ra
    [SerializeField] private int healthScalingFactor = 2; // Tăng bao nhiêu máu mỗi cấp độ người chơi

    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private float knockbackThrust = 15f;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;
    private EnemyHealthBar enemyHealthBar;

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        enemyHealthBar = GetComponentInChildren<EnemyHealthBar>();
    }

    private void Start()
    {
        // Tính máu dựa trên cấp độ người chơi
        int playerLevel = (PlayerLevel.Instance != null) ? PlayerLevel.Instance.CurrentLevel : 1;
        
        float difficultyMultiplier = 1f;
        float difficultyIncreaseMultiplier = 1f;
        
        if (DifficultyManager.Instance != null)
        {
            var diffSettings = DifficultyManager.Instance.GetCurrentSettings();
            difficultyMultiplier = diffSettings.baseHpMultiplier;
            difficultyIncreaseMultiplier = diffSettings.hpIncreasePerLevel;
        }

        // SỬA LẠI: Máu gốc * nhân hệ số + (Số level tăng thêm) * (Lượng tăng ngầm định mỗi con quái * nhân hệ số json)
        int scaledHealth = Mathf.RoundToInt((startingHealth * difficultyMultiplier) + ((playerLevel - 1) * (healthScalingFactor * difficultyIncreaseMultiplier)));

        currentHealth = scaledHealth;
        enemyHealthBar?.SetMaxHealth(scaledHealth);
        
        // --- LOG KIỂM TRA ĐỘ KHÓ VÀ LEVEL ---
        string diffName = DifficultyManager.Instance != null ? DifficultyManager.Instance.CurrentDifficulty.ToString() : "N/A";
        Debug.Log($"[EnemyHealth] Spawn quái: {gameObject.name} | Độ khó: {diffName} | Level Player: {playerLevel} | Máu lúc này: {scaledHealth}");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        enemyHealthBar?.TakeDamage(currentHealth);
        knockback.GetKnockedBack(PlayerController.Instance.transform, knockbackThrust);
        StartCoroutine(flash.FlashRoutine());
        StartCoroutine(CheckDetectDeathRoutine());
    }

    private IEnumerator CheckDetectDeathRoutine()
    {
        yield return new WaitForSeconds(flash.GetRestoreMatTime());
        DetectDeath();
    }

    private void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            GetComponent<PickupSpawner>().DropItems();

            if (PlayerLevel.Instance != null)
            {
                PlayerLevel.Instance.AddXP(xpReward);
            }

            Destroy(gameObject);
        }
    }
}
