using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BlacksmithNPC : MonoBehaviour
{
    private bool isPlayerInRange = false;

    private void Update()
    {
        // Khi người chơi ở trong vùng nhận diện và ấn phím F
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[BlacksmithNPC] Player pressed F!");
            if (WeaponUpgradeManager.Instance != null && !WeaponUpgradeManager.Instance.IsOpen)
            {
                // Mở Giao diện Lò Rèn
                WeaponUpgradeManager.Instance.OpenUpgradeUI();
                
                // Mở Túi đồ luôn để chọn Đá
                if (InventoryManager.Instance != null && !InventoryManager.Instance.IsOpen)
                {
                    Debug.Log("[BlacksmithNPC] Opening Inventory and Upgrade UI.");
                    InventoryManager.Instance.ToggleInventory();
                }
            }
            else if (WeaponUpgradeManager.Instance == null)
            {
                Debug.LogError("[BlacksmithNPC] WeaponUpgradeManager.Instance is NULL!");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[BlacksmithNPC] Player entered Trigger zone!");
            isPlayerInRange = true;
            // TODO: (Tùy chọn) Hiện Hint Text "Nhấn F để Nâng cấp"
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[BlacksmithNPC] Player left Trigger zone!");
            isPlayerInRange = false;
            
            // Nếu đi xa quá thì tự tắt UI
            if (WeaponUpgradeManager.Instance != null && WeaponUpgradeManager.Instance.IsOpen)
            {
                WeaponUpgradeManager.Instance.CloseUpgradeUI();
                
                if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen)
                {
                    InventoryManager.Instance.ToggleInventory();
                }
            }
        }
    }
}
