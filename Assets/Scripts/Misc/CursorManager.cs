using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        // Custom cursor KHÔNG BAO GIỜ chặn click UI
        image.raycastTarget = false;

        // Tách cursor ra Canvas riêng với Sort Order cao nhất (phương án A)
        // để cursor luôn render trên tất cả các panel (pause, inventory, v.v.)
        // mà không phụ thuộc vào thứ tự sibling trong Canvas cha.
        var overrideCanvas = gameObject.GetComponent<Canvas>();
        if (overrideCanvas == null)
            overrideCanvas = gameObject.AddComponent<Canvas>();
        overrideCanvas.overrideSorting = true;
        overrideCanvas.sortingOrder    = 999;

        // GraphicRaycaster không cần thiết cho cursor (raycastTarget = false)
        // nhưng Unity yêu cầu khi Canvas là Override Sorting để Image render đúng.
        if (gameObject.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    // Dùng LateUpdate thay vì Update để cursor luôn được cập nhật SAU khi
    // PauseMenuController và InventoryManager đã xử lý trạng thái trong Update().
    // Điều này tránh việc cursor hiện 1 frame trước khi bị ẩn khi mở/đóng menu.
    private void LateUpdate()
    {
        bool inventoryOpen = InventoryManager.IsInventoryOpen;
        bool pauseOpen     = PauseMenuController.IsPauseMenuOpen;

        if (inventoryOpen || pauseOpen)
        {
            // Ẩn cursor tùy chỉnh, hiện system cursor để click được nút UI
            image.enabled = false;
            Cursor.visible = true;
        }
        else
        {
            // Gameplay: dùng cursor tùy chỉnh, ẩn system cursor
            image.enabled = true;
            Cursor.visible = false;
            image.rectTransform.position = Input.mousePosition;
        }
    }

    // Khi CursorManager bị destroy (chuyển scene), khôi phục system cursor
    // để tránh mắc kẹt ở trạng thái cursor ẩn sang scene tiếp theo.
    private void OnDestroy()
    {
        image.enabled = false;
        Cursor.visible = true;
    }
}
