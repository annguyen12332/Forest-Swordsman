using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInventory : Singleton<ActiveInventory>
{
    private int activeSlotIndexNum = 0;

    private PlayerControls playerControls;

    protected override void Awake()
    {
        base.Awake();

        playerControls = new PlayerControls();
    }

    private void Start()
    {
        playerControls.Inventory.Keyboard.performed += ctx => ToggleActiveSlot((int)ctx.ReadValue<float>());
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    public void EquipStartingWeapon()
    {
        // give player sword by default
        ToggleActiveHighlight(0);
    }

    private void ToggleActiveSlot(int numValue)
    {
        ToggleActiveHighlight(numValue - 1);
    }

    private void ToggleActiveHighlight(int indexNum)
    {
        activeSlotIndexNum = indexNum;

        foreach (Transform inventorySlot in this.transform)
        {
            inventorySlot.GetChild(0).gameObject.SetActive(false);
        }

        this.transform.GetChild(indexNum).GetChild(0).gameObject.SetActive(true);

        ChangeActiveWeapon();
    }

    public void AddItem(WeaponInfo weaponInfo, int amount = 1)
    {
        // Try to find a slot that already has this weapon (for stacking)
        foreach (Transform child in transform)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot.GetWeaponInfo() == weaponInfo)
            {
                slot.AddToCount(amount);
                return;
            }
        }

        // Find first empty slot
        foreach (Transform child in transform)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot.GetWeaponInfo() == null)
            {
                slot.SetWeaponInfo(weaponInfo, amount);
                ChangeActiveWeapon(); // Update if it was the active slot
                return;
            }
        }
    }

    public bool UseItem()
    {
        Transform childTransform = transform.GetChild(activeSlotIndexNum);
        InventorySlot inventorySlot = childTransform.GetComponentInChildren<InventorySlot>();
        
        if (inventorySlot.GetWeaponInfo() != null && inventorySlot.GetCount() > 0)
        {
            inventorySlot.UseItem();
            if (inventorySlot.GetWeaponInfo() == null)
            {
                ChangeActiveWeapon();
            }
            return true;
        }
        return false;
    }

    private void ChangeActiveWeapon()
    {
        if (PlayerHealth.Instance.IsDead) { return; }

        if (ActiveWeapon.Instance.CurrentActiveWeapon != null)
        {
            Destroy(ActiveWeapon.Instance.CurrentActiveWeapon.gameObject);
        }

        Transform childTransform = transform.GetChild(activeSlotIndexNum);
        InventorySlot inventorySlot = childTransform.GetComponentInChildren<InventorySlot>();
        WeaponInfo weaponInfo = inventorySlot.GetWeaponInfo();
        GameObject weaponToSpawn;

        if (weaponInfo != null)
        {
            weaponToSpawn = weaponInfo.weaponPrefab;
        }
        else
        {
            ActiveWeapon.Instance.WeaponNull();
            return;
        }

        GameObject newWeapon = Instantiate(weaponToSpawn, ActiveWeapon.Instance.transform);

        ActiveWeapon.Instance.NewWeapon(newWeapon.GetComponent<MonoBehaviour>());
    }
}
