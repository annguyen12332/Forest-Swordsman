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
        // Đọc static field trực tiếp — không qua Instance, không bị null
        bool inventoryOpen = InventoryManager.IsInventoryOpen;

        if (inventoryOpen)
        {
            image.enabled = false;
            Cursor.visible = true;
        }
        else
        {
            image.enabled = true;
            Cursor.visible = false;
            image.rectTransform.position = Input.mousePosition;
        }
    }
}
