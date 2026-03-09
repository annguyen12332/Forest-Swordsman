using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    private int damageAmount;

    private void Start() {
        // Khởi tạo sát thương tĩnh ban đầu (nếu cần thiết)
        UpdateDamage();
    }

    public void UpdateDamage() {
        MonoBehaviour currentActiveWeapon = ActiveWeapon.Instance.CurrentActiveWeapon;
        if (currentActiveWeapon != null) {
            // Lấy Base Damage từ vũ khí gốc
            int baseDamage = (currentActiveWeapon as IWeapon).GetWeaponInfo().weaponDamage;
            
            // Cộng thêm lượng damage tăng thêm từ Upgrade Level của chính loại vũ khí đó
            int bonusDamage = 0;
            if (WeaponUpgradeManager.Instance != null && WeaponUpgradeManager.Instance.upgradeLevels != null)
            {
                int currentLvl = (currentActiveWeapon as IWeapon).GetWeaponInfo().weaponUpgradeLevel;
                if (currentLvl > 0 && currentLvl <= WeaponUpgradeManager.Instance.upgradeLevels.Count) {
                    // CurrentLevel = 1 thì lấy Index 0 trong List
                    bonusDamage = WeaponUpgradeManager.Instance.upgradeLevels[currentLvl - 1].extraDamage;
                }
            }

            damageAmount = baseDamage + bonusDamage;
        }
    }

    public void SetDamage(int damage) {
        damageAmount = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Liên tục cập nhật lại sát thương trong trường hợp vừa nâng cấp vũ khí tại lò rèn
        UpdateDamage();

        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
            return;
        }

        BossHealth bossHealth = other.gameObject.GetComponent<BossHealth>();
        if (bossHealth != null)
        {
            bossHealth.TakeDamage(damageAmount);
        }
    }
}
