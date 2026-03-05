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
        if (slot != null && slot.GetHeartItemInfo() != null && slot.GetCount() > 0)
        {
            // Slot có tim → hiện popup Use
            if (UseItemPopup.Instance != null)
                UseItemPopup.Instance.Show(transform.position, GetComponent<RectTransform>());
        }
        else
        {
            // Slot không có item → ẩn popup
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
        // Click slot không có item → ẩn popup
        if (UseItemPopup.Instance != null)
            UseItemPopup.Instance.Hide();
    }
}

