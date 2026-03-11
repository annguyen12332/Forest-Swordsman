using UnityEngine;
using TMPro;
using System.Collections;

public class ShopManager : Singleton<ShopManager>
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private TMP_Text shopGoldText;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    [Header("Item Data References (Gán trong Inspector)")]
    [SerializeField] private HeartItemInfo heartInfo;
    [SerializeField] private GemItemInfo gemInfo;
    [SerializeField] private WeaponInfo bombInfo;

    [Header("Shop Prices")]
    [SerializeField] private int heartPrice = 10;
    [SerializeField] private int gemPrice = 15;
    [SerializeField] private int bombPrice = 30;

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    protected override void Awake()
    {
        base.Awake();
        if (shopPanel != null) shopPanel.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
    }

    private void Start()
    {
    }

    public void OpenShop()
    {
        if (shopPanel != null) shopPanel.SetActive(true);
        RefreshGoldUI();
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    // Các hàm này sẽ được gắn vào sự kiện OnClick() của các nút BUY tương ứng
    public void BuyHeart()
    {
        if (TryBuyItem("Heart", heartPrice, heartInfo))
        {
            InventoryManager.Instance.AddHeartItem(heartInfo, 1);
        }
    }

    public void BuyGem()
    {
        if (TryBuyItem("Gem", gemPrice, gemInfo))
        {
            InventoryManager.Instance.AddGemItem(gemInfo, 1);
        }
    }

    public void BuyBomb()
    {
        if (TryBuyItem("Bomb", bombPrice, bombInfo))
        {
            ActiveInventory.Instance.AddItem(bombInfo, 1);
        }
    }

    private bool TryBuyItem(string itemTypeStr, int price, ScriptableObject itemInfo)
    {
        if (itemInfo == null)
        {
            Debug.LogWarning($"[ShopManager] Chưa gán Scriptable Object (Info) cho {itemTypeStr} trong Inspector!");
            return false;
        }

        // 1. Kiểm tra tiền
        if (EconomyManager.Instance == null || EconomyManager.Instance.CurrentGold < price)
        {
            ShowNotification("Not enough Gold!");
            return false;
        }

        // 2. Kiểm tra sức chứa của túi đồ / thanh vũ khí
        bool hasSpace = false;

        if (itemTypeStr == "Bomb")
        {
            hasSpace = CheckActiveInventorySpace(itemInfo as WeaponInfo);
        }
        else
        {
            hasSpace = CheckInventorySpace(itemInfo);
        }

        if (!hasSpace)
        {
            ShowNotification("Inventory Full!");
            return false;
        }

        // 3. Trừ tiền và hiển thị thông báo mua thành công
        EconomyManager.Instance.UpdateCurrentGold(-price);
        RefreshGoldUI();
        ShowNotification("Purchase Successful!");
        return true;
    }

    private bool CheckInventorySpace(ScriptableObject itemInfo)
    {
        if (InventoryManager.Instance == null) return false;

        // Dùng componentInChildren để scan toàn bộ slots của InventoryManager (thường là InventoryGrid)
        InventorySlot[] allSlots = InventoryManager.Instance.GetComponentsInChildren<InventorySlot>(true);
        if (allSlots == null || allSlots.Length == 0) return true; 

        bool hasEmpty = false;

        foreach (var slot in allSlots)
        {
            if (itemInfo is HeartItemInfo heartInfoObj)
            {
                if (slot.GetHeartItemInfo() == heartInfoObj) return true; // Có loại này r -> Stack
            }
            else if (itemInfo is GemItemInfo gemInfoObj)
            {
                if (slot.GetGemItemInfo() == gemInfoObj) return true; // Có loại này r -> Stack
            }

            if (slot.IsEmpty()) hasEmpty = true;
        }

        return hasEmpty;
    }

    private bool CheckActiveInventorySpace(WeaponInfo weaponInfo)
    {
        if (ActiveInventory.Instance == null) return false;

        InventorySlot[] slots = ActiveInventory.Instance.GetComponentsInChildren<InventorySlot>(true);
        if (slots == null || slots.Length == 0) return true;

        bool hasEmpty = false;

        foreach (var slot in slots)
        {
            if (slot.GetWeaponInfo() == weaponInfo) return true; // Stack
            if (slot.IsEmpty()) hasEmpty = true;
        }

        return hasEmpty;
    }

    private void RefreshGoldUI()
    {
        if (shopGoldText != null && EconomyManager.Instance != null)
        {
            shopGoldText.text = EconomyManager.Instance.CurrentGold.ToString("D3");
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;
        
        StopAllCoroutines();
        StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(2f); // Hiện thông báo trong 2 giây
        
        notificationText.gameObject.SetActive(false);
    }
}
