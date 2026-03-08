using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInventory : Singleton<ActiveInventory>
{
    private int activeSlotIndexNum = 0;
    public int ActiveSlotIndexNum => activeSlotIndexNum;

    private PlayerControls playerControls;

    protected override void Awake()
    {
        playerControls = new PlayerControls(); // khởi tạo trước base.Awake()
        base.Awake();
    }

    private void Start()
    {
        playerControls.Inventory.Keyboard.performed += ctx => ToggleActiveSlot((int)ctx.ReadValue<float>());
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

        // Tắt Highlight của tất cả các ô
        foreach (Transform inventorySlot in this.transform)
        {
            if (inventorySlot.childCount > 0)
            {
                // Giả định child index 0 là khung Highlight (Active Hình vuông trắng)
                inventorySlot.GetChild(0).gameObject.SetActive(false);
            }
        }

        // Bật Highlight của ô được chọn
        Transform targetSlot = this.transform.GetChild(indexNum);
        if (targetSlot.childCount > 0)
        {
            targetSlot.GetChild(0).gameObject.SetActive(true);
        }

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
            if (slot.IsEmpty())
            {
                slot.SetWeaponInfo(weaponInfo, amount);
                ChangeActiveWeapon();
                return;
            }
        }
    }

    public void AddHeartItem(HeartItemInfo heartItemInfo, int amount = 1)
    {
        // Stack vào slot đang có tim cùng loại
        foreach (Transform child in transform)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot.GetHeartItemInfo() == heartItemInfo)
            {
                slot.AddToCount(amount);
                if (HeartHUD.Instance != null) HeartHUD.Instance.AddHeart(amount);
                return;
            }
        }

        // Tìm slot rỗng
        foreach (Transform child in transform)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot.IsEmpty())
            {
                slot.SetHeartItem(heartItemInfo, amount);
                if (HeartHUD.Instance != null) HeartHUD.Instance.AddHeart(amount);
                return;
            }
        }
    }

    public bool UseHeartItem()
    {
        // Tìm slot đầu tiên có tim
        foreach (Transform child in transform)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot.GetHeartItemInfo() != null && slot.GetCount() > 0)
            {
                slot.UseItem();                                             // luôn trừ count
                if (HeartHUD.Instance != null) HeartHUD.Instance.UseHeart(); // cập nhật HUD
                PlayerHealth.Instance.HealPlayer();                        // hồi máu
                return true;
            }
        }
        return false;
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
