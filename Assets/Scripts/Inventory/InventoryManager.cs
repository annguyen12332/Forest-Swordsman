using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : Singleton<InventoryManager>
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform inventoryGrid; // kéo InventoryGrid vào đây

    public static bool IsInventoryOpen { get; private set; } = false;
    public bool IsOpen => IsInventoryOpen;

    protected override void Awake()
    {
        base.Awake();
        IsInventoryOpen = false;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

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
        IsInventoryOpen = !IsInventoryOpen;
        inventoryPanel.SetActive(IsInventoryOpen);

        if (IsInventoryOpen && EconomyManager.Instance != null)
            EconomyManager.Instance.RefreshGoldUI();
    }

    public void CloseInventory()
    {
        if (inventoryPanel == null) return;
        IsInventoryOpen = false;
        inventoryPanel.SetActive(false);
    }

    // ─── Heart Item management (dùng InventoryGrid slots) ────────────────────

    public void AddHeartItem(HeartItemInfo heartItemInfo, int amount = 1)
    {
        if (inventoryGrid == null) { Debug.LogWarning("InventoryManager: inventoryGrid chưa gán!"); return; }

        // Stack vào slot đã có tim cùng loại
        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null) continue;
            if (slot.GetHeartItemInfo() == heartItemInfo)
            {
                slot.AddToCount(amount);
                if (HeartHUD.Instance != null) HeartHUD.Instance.AddHeart(amount);
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
                return;
            }
        }
        Debug.LogWarning("InventoryManager: Không còn slot trống trong hành trang!");
    }

    public void UseHeartItem()
    {
        if (inventoryGrid == null) return;

        foreach (Transform child in inventoryGrid)
        {
            InventorySlot slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null) continue;
            if (slot.GetHeartItemInfo() != null && slot.GetCount() > 0)
            {
                slot.UseItem();
                if (HeartHUD.Instance != null) HeartHUD.Instance.UseHeart();
                if (PlayerHealth.Instance != null) PlayerHealth.Instance.HealPlayer();
                return;
            }
        }
        Debug.Log("[InventoryManager] Không có tim trong hành trang.");
    }
}

