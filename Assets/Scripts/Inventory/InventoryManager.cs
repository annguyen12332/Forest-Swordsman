using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform inventoryGrid; // kéo InventoryGrid vào đây

    [Header("Save Data Refs (Gán trong Inspector)")]
    public GemItemInfo saveGemRef;
    public HeartItemInfo saveHeartRef;
    private bool isLoadingData = false;

    // Luôn lấy thẳng từ Panel — không bao giờ bị lệch với thực tế
    public static bool IsInventoryOpen => Instance != null && Instance.inventoryPanel != null && Instance.inventoryPanel.activeSelf;
    public bool IsOpen => IsInventoryOpen;

    private RectTransform panelRect;
    private Vector2 originalPosition;

    protected void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        if (inventoryPanel != null)
        {
            panelRect = inventoryPanel.GetComponent<RectTransform>();
            if (panelRect != null)
                originalPosition = panelRect.anchoredPosition;

            inventoryPanel.SetActive(false);
        }

        // Tự tìm InventoryGrid nếu chưa gán
        if (inventoryGrid == null && inventoryPanel != null)
        {
            Transform grid = inventoryPanel.transform.Find("InventoryGrid");
            if (grid != null) inventoryGrid = grid;
        }

        // Tự gắn InventorySlot + InventorySlotUI vào tất cả slot con
        if (inventoryGrid != null)
        {
            foreach (Transform child in inventoryGrid)
            {
                if (child.GetComponent<InventorySlot>() == null)
                    child.gameObject.AddComponent<InventorySlot>();

                if (child.GetComponent<InventorySlotUI>() == null)
                    child.gameObject.AddComponent<InventorySlotUI>();
            }
        }
    }

    private void Start()
    {
        if (closeButton == null && inventoryPanel != null)
        {
            Transform closeTransform = inventoryPanel.transform.Find("CloseButton");
            if (closeTransform != null)
                closeButton = closeTransform.GetComponent<Button>();
        }
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);
        else
            Debug.LogWarning("InventoryManager: Không tìm thấy CloseButton!");

        // Khôi phục Gems và Hearts từ file JSON
        if (SaveManager.Instance != null)
        {
            isLoadingData = true;
            if (saveGemRef != null && SaveManager.Instance.Data.gemCount > 0)
                AddGemItem(saveGemRef, SaveManager.Instance.Data.gemCount);

            if (saveHeartRef != null && SaveManager.Instance.Data.heartCount > 0)
                AddHeartItem(saveHeartRef, SaveManager.Instance.Data.heartCount);
            isLoadingData = false;
        }
    }

    private void SaveInventoryData()
    {
        if (SaveManager.Instance == null || isLoadingData) return;
        
        int totalGems = 0;
        int totalHearts = 0;
        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot != null)
            {
                if (slot.GetGemItemInfo() != null) totalGems += slot.GetCount();
                if (slot.GetHeartItemInfo() != null) totalHearts += slot.GetCount();
            }
        }
        SaveManager.Instance.Data.gemCount = totalGems;
        SaveManager.Instance.Data.heartCount = totalHearts;
        SaveManager.Instance.SaveGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("InventoryManager: inventoryPanel chưa gán trong Inspector!");
            return;
        }
        bool nowOpen = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(nowOpen);

        if (nowOpen)
        {
            // Tự động lùi sang trái nếu Lò Rèn đang mở
            if (WeaponUpgradeManager.Instance != null && WeaponUpgradeManager.Instance.IsOpen)
                MoveToSide();
            else
                MoveToCenter();

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.RefreshGoldUI();
        }
    }

    public void MoveToSide()
    {
        if (panelRect != null)
        {
            // Dịch sang trái 500 pixels (tăng khoảng cách xa hơn)
            panelRect.anchoredPosition = originalPosition + new Vector2(-500f, 0);
        }
    }

    public void MoveToCenter()
    {
        if (panelRect != null)
        {
            panelRect.anchoredPosition = originalPosition;
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel == null) return;
        inventoryPanel.SetActive(false);

        // Bỏ focus UI để nút A/D không bị kẹt vào Slider/Button
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // ─── Heart Item management (dùng InventoryGrid slots) ────────────────────

    public void AddHeartItem(HeartItemInfo heartItemInfo, int amount = 1)
    {
        if (inventoryGrid == null) 
        { 
             // Thử tìm lại grid nếu bị lạc reference
            if (inventoryPanel != null)
            {
                Transform grid = inventoryPanel.transform.Find("InventoryGrid");
                if (grid != null) inventoryGrid = grid;
            }
        }
        
        if (inventoryGrid == null) { Debug.LogWarning("InventoryManager: inventoryGrid chưa gán!"); return; }

        Debug.Log($"[InventoryManager] Thêm {amount} tim vào hành trang.");
        // Stack vào slot đã có tim cùng loại
        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null) continue;
            if (slot.GetHeartItemInfo() == heartItemInfo)
            {
                slot.AddToCount(amount);
                if (HeartHUD.Instance != null) HeartHUD.Instance.AddHeart(amount);
                SaveInventoryData();
                return;
            }
        }

        // Tìm slot rỗng
        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null) continue;
            if (slot.IsEmpty())
            {
                slot.SetHeartItem(heartItemInfo, amount);
                if (HeartHUD.Instance != null) HeartHUD.Instance.AddHeart(amount);
                SaveInventoryData();
                return;
            }
        }
        Debug.LogWarning("InventoryManager: Không còn slot trống trong hành trang!");
    }

    public bool UseHeartItem()
    {
        if (inventoryGrid == null) return false;

        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponent<InventorySlot>();
            if (slot == null) slot = child.GetComponentInChildren<InventorySlot>();
            
            if (slot != null && slot.GetHeartItemInfo() != null && slot.GetCount() > 0)
            {
                UseHeartFromSlot(slot);
                return true;
            }
        }
        return false;
    }

    public void UseHeartFromSlot(InventorySlot slot)
    {
        if (slot == null) return;
        if (slot.GetHeartItemInfo() != null && slot.GetCount() > 0)
        {
            slot.UseItem();
            if (HeartHUD.Instance != null) HeartHUD.Instance.UseHeart();
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.HealPlayer();
            SaveInventoryData();
        }
    }

    // ─── Gem Item management ──────────────────────────────────────────────────

    public void AddGemItem(GemItemInfo gemItemInfo, int amount = 1)
    {
        if (inventoryGrid == null) { Debug.LogWarning("InventoryManager: inventoryGrid chưa gán!"); return; }

        // Stack vào slot đã có gem cùng loại
        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null) continue;
            if (slot.GetGemItemInfo() == gemItemInfo)
            {
                slot.AddToCount(amount);
                SaveInventoryData();
                return;
            }
        }

        // Tìm slot rỗng
        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null) continue;
            if (slot.IsEmpty())
            {
                slot.SetGemItem(gemItemInfo, amount);
                SaveInventoryData();
                return;
            }
        }
        Debug.LogWarning("InventoryManager: Không còn slot trống trong hành trang!");
    }

    public int GetGemCount()
    {
        int totalGem = 0;
        if (inventoryGrid == null) return 0;

        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot != null && slot.GetGemItemInfo() != null)
            {
                totalGem += slot.GetCount();
            }
        }
        return totalGem;
    }

    public void RemoveGems(int amount)
    {
        if (inventoryGrid == null) return;
        
        int remainingToRemove = amount;

        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null || slot.GetGemItemInfo() == null) continue;

            int countInSlot = slot.GetCount();
            if (countInSlot > 0)
            {
                if (countInSlot >= remainingToRemove)
                {
                    // Slot này đủ để trừ
                    for(int i = 0; i < remainingToRemove; i++) 
                         slot.UseItem(); // Trừ dần trong slot
                         
                    SaveInventoryData();
                    return; // Đã xong
                }
                else
                {
                    // Slot này không đủ, trừ hết slot này r quay tiếp slot sau
                    for (int i = 0; i < countInSlot; i++)
                         slot.UseItem();
                         
                    remainingToRemove -= countInSlot;
                }
            }
        }
    }
}

