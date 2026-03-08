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
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
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
}
