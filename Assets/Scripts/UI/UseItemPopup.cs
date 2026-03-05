using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UseItemPopup : Singleton<UseItemPopup>
{
    [SerializeField] private Button useButton;

    private RectTransform popupRect;
    private RectTransform originSlotRect; // slot đang được hover

    protected override void Awake()
    {
        base.Awake();
        popupRect = GetComponent<RectTransform>();
        gameObject.SetActive(false);

        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);
    }

    /// <summary>Hiện popup gần slot, lưu slot rect để theo dõi chuột.</summary>
    public void Show(Vector3 worldPosition, RectTransform slotRect)
    {
        originSlotRect = slotRect;
        gameObject.SetActive(true);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
            //popupRect.position = screenPos + new Vector2(60f, 0f);
        }
    }

    public void Hide()
    {
        originSlotRect = null;  
        gameObject.SetActive(false);
    }

    public bool IsMouseOver()
    {
        if (!gameObject.activeSelf) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(popupRect, Input.mousePosition);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        bool overPopup = RectTransformUtility.RectangleContainsScreenPoint(popupRect, Input.mousePosition);
        bool overSlot  = originSlotRect != null &&
                         RectTransformUtility.RectangleContainsScreenPoint(originSlotRect, Input.mousePosition);

        // Chuột ra ngoài cả popup lẫn slot → ẩn
        if (!overPopup && !overSlot)
            Hide();
    }

    private void OnUseClicked()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.UseHeartItem();
        Hide();
    }
}




