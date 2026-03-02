using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    private int damageAmount;

    private void Start() {
        if (damageAmount == 0) {
            MonoBehaviour currentActiveWeapon = ActiveWeapon.Instance.CurrentActiveWeapon;
            if (currentActiveWeapon != null) {
                damageAmount = (currentActiveWeapon as IWeapon).GetWeaponInfo().weaponDamage;
            }
        }
    }

    public void SetDamage(int damage) {
        damageAmount = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
        }
    }
}
