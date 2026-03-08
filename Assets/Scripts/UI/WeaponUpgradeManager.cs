using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct UpgradeLevelData
{
    public int level;
    public int goldCost;
    public int gemCost;
    [Range(0f, 100f)] public float successRatePercent;
    public int extraDamage; 
}

public class WeaponUpgradeManager : MonoBehaviour
{
    public static WeaponUpgradeManager Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private Image gemIconImage;
    [SerializeField] private TMP_Text weaponLevelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    public List<UpgradeLevelData> upgradeLevels;
    
    // Cấp độ tối đa vũ khí có thể đạt được (Dựa vào số lượng phần tử cấu hình)
    public int MaxLevel => upgradeLevels.Count; 

    // Biến lưu trạng thái hiện tại (Giả lập: Bạn có thể lưu biến này vào PlayerPrefs hoặc lưu thẳng vào SO Weapon)
    // Cấp độ đã được chuyển sang WeaponInfo của từng vũ khí. 
    
    private bool hasGemPlaced = false;
    private GemItemInfo currentGemPlaced; // Loại đá được đặt vào (Để sau này trừ)

    public bool IsOpen => upgradePanel != null && upgradePanel.activeSelf;

    protected void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseUpgradeUI);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }
        
        // Khởi tạo bảng dữ liệu thông số nếu chưa Set ở Inspector
        if (upgradeLevels == null || upgradeLevels.Count == 0)
        {
            SetupDefaultUpgradeLevels();
        }
    }

    private void SetupDefaultUpgradeLevels()
    {
        upgradeLevels = new List<UpgradeLevelData>
        {
            new UpgradeLevelData { level = 1, goldCost = 1, gemCost = 1, successRatePercent = 100f, extraDamage = 2 },
            new UpgradeLevelData { level = 2, goldCost = 2, gemCost = 2, successRatePercent = 90f, extraDamage = 4 },
            new UpgradeLevelData { level = 3, goldCost = 3, gemCost = 3, successRatePercent = 80f, extraDamage = 6 },
            new UpgradeLevelData { level = 4, goldCost = 40, gemCost = 4, successRatePercent = 30f, extraDamage = 9 },
            new UpgradeLevelData { level = 5, goldCost = 800, gemCost = 5, successRatePercent = 20f, extraDamage = 13 },
            new UpgradeLevelData { level = 6, goldCost = 1500, gemCost = 6, successRatePercent = 10f, extraDamage = 18 },
            new UpgradeLevelData { level = 7, goldCost = 2500, gemCost = 8, successRatePercent = 5f, extraDamage = 24 },
            new UpgradeLevelData { level = 8, goldCost = 4000, gemCost = 10, successRatePercent = 2f, extraDamage = 35 } // Max level
        };
    }

    public void OpenUpgradeUI()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            hasGemPlaced = false;
            currentGemPlaced = null; 
            UpdateUI();
            
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.MoveToSide();
        }
    }

    public void CloseUpgradeUI()
    {
        try {
            if (upgradePanel != null && upgradePanel.activeSelf)
            {
                upgradePanel.SetActive(false);
                if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen)
                    InventoryManager.Instance.CloseInventory(); // Gọi đúng lệnh tắt
                    
                // Bỏ focus UI để nút A/D không bị kẹt vào Slider/Button
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                }
            }
        } catch (System.Exception e) {
            Debug.LogError("[CloseUpgradeUI] Exception: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // GỌI HÀM NÀY KHI CLICK VÀO ĐÁ TRONG INVENTORY
    public void PlaceGem(GemItemInfo gemInfo)
    {
        if (!IsOpen) return;
        
        hasGemPlaced = true;
        currentGemPlaced = gemInfo;

        if (gemIconImage != null && gemInfo != null)
        {
            gemIconImage.sprite = gemInfo.itemIcon;
            gemIconImage.color = Color.white; // Bỏ trong suốt (nếu có set transparent trước đó)
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        // 1. Cập nhật Icon Vũ Khí Đang Cầm
        IWeapon currentWeapon = null;
        if (ActiveWeapon.Instance != null && ActiveWeapon.Instance.CurrentActiveWeapon != null)
        {
            currentWeapon = ActiveWeapon.Instance.CurrentActiveWeapon as IWeapon;
        }
        else if (ActiveInventory.Instance != null)
        {
            // Trục trặc do ActiveWeapon chưa khởi tạo vũ khí (player chưa đổi vũ khí hoặc mới bật game)
            // Lép thẳng ActiveWeapon từ Inventory
            Transform weaponSlot = ActiveInventory.Instance.transform.GetChild(ActiveInventory.Instance.ActiveSlotIndexNum);
            if (weaponSlot != null && weaponSlot.childCount > 0)
            {
                currentWeapon = weaponSlot.GetChild(0).GetComponent<IWeapon>();
            }
        }

        if (currentWeapon != null && weaponIconImage != null)
        {
            Sprite wIcon = currentWeapon.GetWeaponInfo().weaponIcon;
            if (wIcon != null) 
            {
                weaponIconImage.sprite = wIcon;
                weaponIconImage.color = Color.white;
            }
            else 
            {
                weaponIconImage.color = new Color(1, 1, 1, 0); // Tàng hình nếu vũ khí k có ảnh
                Debug.LogWarning("[WeaponUpgradeManager] currentWeapon has no Icon.");
            }
        }
        else
        {
            Debug.LogWarning("[WeaponUpgradeManager] currentWeapon is NULL. Could not load icon.");
            if (weaponIconImage != null) weaponIconImage.color = new Color(1, 1, 1, 0); // Tàng hình nếu k có vũ khí
        }

        int currentWeaponLevel = 0;
        if (currentWeapon != null && currentWeapon.GetWeaponInfo() != null)
        {
            currentWeaponLevel = currentWeapon.GetWeaponInfo().weaponUpgradeLevel;
        }

        // Nếu chưa bỏ ngọc vào, để icon tàng hình
        if (!hasGemPlaced && gemIconImage != null)
        {
            gemIconImage.sprite = null;
            gemIconImage.color = new Color(1, 1, 1, 0);
        }

        // 2. Kiểm tra Max Cấp (hoặc không có vũ khí)
        if (currentWeapon == null)
        {
            weaponLevelText.text = "No Weapon Equipped";
            costText.text = "";
            statusText.text = "<color=red>Please equip a weapon to upgrade!</color>";
            statusText.gameObject.SetActive(true);
            return;
        }

        if (currentWeaponLevel >= MaxLevel)
        {
            weaponLevelText.text = $"Level: MAX ({MaxLevel})";
            costText.text = "";
            statusText.text = "<color=yellow>Weapon has reached maximum level!</color>";
            statusText.gameObject.SetActive(true);
            return;
        }

        // 3. Hiển thị thông số cho cấp tiếp theo
        UpgradeLevelData nextLevelData = upgradeLevels[currentWeaponLevel];
        weaponLevelText.text = $"Level: {currentWeaponLevel} to {nextLevelData.level}";

        // Đếm đá người chơi đang có trong túi
        int currentGemCount = InventoryManager.Instance != null ? InventoryManager.Instance.GetGemCount() : 0;
        int currentGold = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentGold : 0;

        string goldStr = currentGold >= nextLevelData.goldCost ? $"<color=white>{nextLevelData.goldCost}</color>" : $"<color=red>{nextLevelData.goldCost}</color>";
        string gemStr = currentGemCount >= nextLevelData.gemCost ? $"<color=white>{nextLevelData.gemCost}</color>" : $"<color=red>{nextLevelData.gemCost}</color>";

        costText.text = $"Requirements: {goldStr} Gold | {gemStr} Gems\nSuccess Rate: {nextLevelData.successRatePercent}%";
        statusText.text = ""; // Xoá trạng thái cũ khi update
    }

    private void OnUpgradeButtonClicked()
    {
        try {
            Debug.Log("[WeaponUpgrade] Clicked Upgrade Button.");
            // Lấy vũ khí hiện tại
            IWeapon currentWeapon = null;
            if (ActiveWeapon.Instance != null && ActiveWeapon.Instance.CurrentActiveWeapon != null)
            {
                currentWeapon = ActiveWeapon.Instance.CurrentActiveWeapon as IWeapon;
            }
            else if (ActiveInventory.Instance != null)
            {
                Transform weaponSlot = ActiveInventory.Instance.transform.GetChild(ActiveInventory.Instance.ActiveSlotIndexNum);
                if (weaponSlot != null && weaponSlot.childCount > 0)
                {
                    currentWeapon = weaponSlot.GetChild(0).GetComponent<IWeapon>();
                }
            }

            if (currentWeapon == null || currentWeapon.GetWeaponInfo() == null)
            {
                statusText.text = "<color=red>No weapon equipped!</color>";
                statusText.gameObject.SetActive(true);
                return;
            }

            int currentWeaponLevel = currentWeapon.GetWeaponInfo().weaponUpgradeLevel;

            // Check 1: Đã Max Cấp chưa?
            if (currentWeaponLevel >= MaxLevel)
            {
                statusText.text = "<color=yellow>Maximum level reached!</color>";
                statusText.gameObject.SetActive(true);
                return;
            }

            UpgradeLevelData nextLevelData = upgradeLevels[currentWeaponLevel];

            // Check 2: Đã cho đá vào mỏ đe chưa?
            if (!hasGemPlaced)
            {
                 statusText.text = "<color=orange>Please insert a Gem!</color>";
                 statusText.gameObject.SetActive(true);
                 return;
            }

            // Check 3: Check Số lượng Tiền & Đá trong túi
            int currentGold = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentGold : 0;
            int currentGemCount = InventoryManager.Instance != null ? InventoryManager.Instance.GetGemCount() : 0;

            if (currentGold < nextLevelData.goldCost)
            {
                Debug.Log($"[WeaponUpgrade] Not enough gold. Have {currentGold}, need {nextLevelData.goldCost}");
                statusText.text = "<color=red>Not enough Gold!</color>";
                statusText.gameObject.SetActive(true);
                return;
            }

            if (currentGemCount < nextLevelData.gemCost)
            {
                Debug.Log($"[WeaponUpgrade] Not enough gems. Have {currentGemCount}, need {nextLevelData.gemCost}");
                statusText.text = "<color=red>Not enough Gems!</color>";
                statusText.gameObject.SetActive(true);
                return;
            }

            Debug.Log("[WeaponUpgrade] Deducting resources...");
            // -> ĐỦ ĐIỀU KIỆN! TIẾN HÀNH TRỪ RESOURCES
            EconomyManager.Instance.UpdateCurrentGold(-nextLevelData.goldCost);
            InventoryManager.Instance.RemoveGems(nextLevelData.gemCost);

            Debug.Log("[WeaponUpgrade] Generating random roll...");
            // -> QUAY RANDOM
            float randomRoll = Random.Range(0f, 100f);
            if (randomRoll <= nextLevelData.successRatePercent)
            {
                // THÀNH CÔNG
                currentWeapon.GetWeaponInfo().weaponUpgradeLevel++;

                // LƯU CẤP ĐỘ VŨ KHÍ JSON
                if (SaveManager.Instance != null)
                {
                    string wName = currentWeapon.GetWeaponInfo().name;
                    WeaponSaveData wData = SaveManager.Instance.Data.weaponsData.Find(w => w.weaponName == wName);
                    
                    if (wData != null) wData.upgradeLevel = currentWeapon.GetWeaponInfo().weaponUpgradeLevel;
                    else SaveManager.Instance.Data.weaponsData.Add(new WeaponSaveData { weaponName = wName, upgradeLevel = currentWeapon.GetWeaponInfo().weaponUpgradeLevel });
                    
                    SaveManager.Instance.SaveGame();
                }

                statusText.text = $"<color=green>Upgrade Successful to Level {currentWeapon.GetWeaponInfo().weaponUpgradeLevel}!</color>";
                Debug.Log("[WeaponUpgrade] UPGRADE SUCCESS");
            }
            else
            {
                // THẤT BẠI
                // Luật tuỳ chọn: Nếu xịt có thể trừ cấp. Bài này ta giữ nguyên chỉ báo Thất bại
                statusText.text = "<color=red>Upgrade Failed! Materials lost.</color>";
                Debug.Log("[WeaponUpgrade] UPGRADE FAILED");
            }

            string savedStatusText = statusText.text; // Giữ lại thông báo
            statusText.gameObject.SetActive(true);
            
            Debug.Log("[WeaponUpgrade] Updating UI...");
            // Cập nhật lại Text cho lượt bấm sau (hàm này sẽ reset statusText thành rỗng)
            UpdateUI();

            // Hiển thị lại thông báo cũ
            statusText.text = savedStatusText; 
            statusText.gameObject.SetActive(true);
            
            Debug.Log("[WeaponUpgrade] Upgrade transaction complete.");
            
        } catch (System.Exception e) {
            Debug.LogError("[OnUpgradeButtonClicked] CRITICAL EXCEPTION: " + e.Message + "\n" + e.StackTrace);
        }
    }
}
