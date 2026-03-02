using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boom : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject boomProjectilePrefab;
    [SerializeField] private Transform boomSpawnPoint;

    private void Start() {
        boomSpawnPoint = GameObject.Find("SlashAnimationSpawnPoint").transform;
    }

    private void Update()
    {
        MouseFollowWithOffset();
    }

    public void Attack()
    {
        // Use ActiveInventory to decrement count and get current slot info if needed
        bool canAttack = ActiveInventory.Instance.UseItem();
        
        if (canAttack) {
            GameObject newBoom = Instantiate(boomProjectilePrefab, boomSpawnPoint.position, ActiveWeapon.Instance.transform.rotation);
            BoomProjectile projectile = newBoom.GetComponent<BoomProjectile>();
            projectile.UpdateProjectileRange(weaponInfo.weaponRange);
            projectile.SetDamage(weaponInfo.weaponDamage);
        }
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }

    private void MouseFollowWithOffset()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 playerScreenPoint = Camera.main.WorldToScreenPoint(PlayerController.Instance.transform.position);

        Vector3 direction = mousePos - playerScreenPoint;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // flip position with player
        if (mousePos.x < playerScreenPoint.x)
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, -180, angle);
        }
        else
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
