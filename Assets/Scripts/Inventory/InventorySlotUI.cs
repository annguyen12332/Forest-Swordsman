using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên mỗi Slot Image trong InventoryGrid
/// để có hiệu ứng highlight khi trỏ chuột vào
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image slotImage;

    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // vàng sáng khi hover

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            slotImage.color = normalColor;
            slotImage.raycastTarget = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = highlightColor;

        InventorySlot slot = GetComponentInChildren<InventorySlot>();

        // Chỉ hiện popup Use khi slot chứa HeartItem (Gem KHÔNG có nút Use)
        if (slot != null && slot.GetHeartItemInfo() != null && slot.GetCount() > 0)
        {
            if (UseItemPopup.Instance != null)
                UseItemPopup.Instance.Show(transform.position, GetComponent<RectTransform>());
        }
        else
        {
            // Slot trống hoặc chứa Gem / Weapon → ẩn popup
            if (UseItemPopup.Instance != null)
                UseItemPopup.Instance.Hide();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = normalColor;

        // Không hide ở đây — Update() trong UseItemPopup tự xử lý
        // (tránh trường hợp chuột đi qua popup khi di sang slot kề)
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        InventorySlot slot = GetComponentInChildren<InventorySlot>();
        Debug.Log("[InventorySlotUI] Clicked on slot! slot is null: " + (slot == null));
        if (slot == null) return;

        // Click vào slot có Heart Item → dùng ngay (không cần popup)
        if (slot.GetHeartItemInfo() != null && slot.GetCount() > 0)
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.UseHeartFromSlot(slot);
        }
        // Click vào slot Gem Item → Nếu Lò rèn đang mở, đẩy sang lò rèn
        else if (slot.GetGemItemInfo() != null && slot.GetCount() > 0)
        {
             Debug.Log("[InventorySlotUI] Clicked on Gem. WeaponUpgradeOpen: " + (WeaponUpgradeManager.Instance != null && WeaponUpgradeManager.Instance.IsOpen));
             if (WeaponUpgradeManager.Instance != null && WeaponUpgradeManager.Instance.IsOpen)
             {
                 WeaponUpgradeManager.Instance.PlaceGem(slot.GetGemItemInfo());
             }
        }
        else
        {
            if (UseItemPopup.Instance != null)
                UseItemPopup.Instance.Hide();
        }
    }
}

