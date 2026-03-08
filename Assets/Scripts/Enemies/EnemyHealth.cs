using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 3;
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
        currentHealth = startingHealth;
        enemyHealthBar?.SetMaxHealth(startingHealth);
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
            Destroy(gameObject);
        }
    }
}
