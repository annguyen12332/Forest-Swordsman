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
            // Đảm bảo slot nhận click/hover
            slotImage.raycastTarget = true;
        }
    }

    // Chuột vào slot → highlight
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = highlightColor;
    }

    // Chuột rời slot → trở về màu thường
    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = normalColor;
    }

    // Click vào slot (mở rộng sau: equip item, show tooltip...)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Slot clicked: " + gameObject.name);
    }
}
