using UnityEngine;

/// <summary>
/// ScriptableObject storing boss stats. Create via Assets → Create → Boss → Boss Stats.
/// </summary>
[CreateAssetMenu(fileName = "BossStats", menuName = "Boss/Boss Stats")]
public class BossStatsSO : ScriptableObject
{
  [Header("Health")]
  public int maxHealth = 20;

  [Header("Movement")]
  public float moveSpeed = 2f;
  public float chaseRange = 6f;

  [Header("Attack")]
  public float attackRange = 1.2f;
  public int attackDamage = 2;
  public float attackCooldown = 1.5f;

  [Header("Summon")]
  public float summonCooldown = 8f;
  public float firstSummonDelay = 4f;

  [Header("Knockback")]
  public float knockbackThrust = 10f;
}
