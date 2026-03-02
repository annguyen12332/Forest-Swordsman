using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : Singleton<InventoryManager>
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeButton;

    // Static field: CursorManager đọc trực tiếp, không cần qua Instance
    public static bool IsInventoryOpen { get; private set; } = false;

    // Dùng cho các script khác qua Instance
    public bool IsOpen => IsInventoryOpen;

    protected override void Awake()
    {
        base.Awake();
        IsInventoryOpen = false;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void Start()
    {
        // Tự động tìm và wire CloseButton theo tên
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
    }

    public void CloseInventory()
    {
        if (inventoryPanel == null) return;
        IsInventoryOpen = false;
        inventoryPanel.SetActive(false);
        Debug.Log("CloseInventory() called!");
    }
}
