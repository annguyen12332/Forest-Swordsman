using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveWeapon : Singleton<ActiveWeapon>
{
    public MonoBehaviour CurrentActiveWeapon { get; private set; }
    private PlayerControls playerControls;
    private float timeBetweenAttacks;
    private bool attackButtonDown, isAttacking = false;


    protected override void Awake()
    {
        playerControls = new PlayerControls(); // khởi tạo trước base.Awake()
        base.Awake();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    private void OnDestroy()
    {
        playerControls?.Dispose();
        playerControls = null;
    }

    private void Start()
    {
        playerControls.Combat.Attack.started += _ => StartAttacking();
        playerControls.Combat.Attack.canceled += _ => StopAttacking();
        // AttackCooldown();
    }

    private void Update()
    {
        Attack();
    }

    public void NewWeapon(MonoBehaviour newWeapon)
    {
        CurrentActiveWeapon = newWeapon;

        // LOAD LEVEL VŨ KHÍ TỪ JSON KHI TRANG BỊ
        if (SaveManager.Instance != null && newWeapon is IWeapon weaponInterface)
        {
             string wName = weaponInterface.GetWeaponInfo().name;
             var wData = SaveManager.Instance.Data.weaponsData.Find(w => w.weaponName == wName);
             if (wData != null)
             {
                 weaponInterface.GetWeaponInfo().weaponUpgradeLevel = wData.upgradeLevel;
             }
        }

        // --- BUG FIX ---
        // Ensure that any stuck weapon colliders from previous weapons (e.g., interrupted attacks during Save & Quit) are turned off
        if (PlayerController.Instance != null && PlayerController.Instance.GetWeaponCollider() != null)
        {
            PlayerController.Instance.GetWeaponCollider().gameObject.SetActive(false);
        }

        AttackCooldown();
        timeBetweenAttacks = (CurrentActiveWeapon as IWeapon).GetWeaponInfo().weaponCooldown;
    }

    public void WeaponNull()
    {
        CurrentActiveWeapon = null;
        if (PlayerController.Instance != null && PlayerController.Instance.GetWeaponCollider() != null)
        {
            PlayerController.Instance.GetWeaponCollider().gameObject.SetActive(false);
        }
    }

    private void AttackCooldown()
    {
        isAttacking = true;
        StopAllCoroutines();
        StartCoroutine(TimeBetweenAttacksRoutine());
    }

    private IEnumerator TimeBetweenAttacksRoutine()
    {
        yield return new WaitForSeconds(timeBetweenAttacks);
        isAttacking = false;
    }

    private void StartAttacking()
    {
        // Không bắt đầu attack khi hành trang đang mở
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen) return;
        attackButtonDown = true;
    }

    private void StopAttacking()
    {
        attackButtonDown = false;
    }

    private void Attack()
    {
        // Không attack khi hành trang đang mở
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen) return;

        if (attackButtonDown && !isAttacking && CurrentActiveWeapon)
        {
            AttackCooldown();
            (CurrentActiveWeapon as IWeapon).Attack();
        }
    }
}
