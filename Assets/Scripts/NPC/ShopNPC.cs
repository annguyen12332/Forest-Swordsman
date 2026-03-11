using UnityEngine;
using UnityEngine.InputSystem;

public class ShopNPC : MonoBehaviour
{
    private bool isPlayerInRange = false;

    private void Update()
    {
        // Khi người chơi vào vùng Trigger và ấn F
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[ShopNPC] Player pressed F!");
            if (ShopManager.Instance != null)
            {
                if (!ShopManager.Instance.IsOpen)
                {
                    ShopManager.Instance.OpenShop();
                }
                else
                {
                    ShopManager.Instance.CloseShop();
                }
            }
            else
            {
                Debug.LogError("[ShopNPC] ShopManager.Instance is NULL! Have you created the ShopManager GameObject in the scene?");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[ShopNPC] Player entered Trigger zone!");
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[ShopNPC] Player left Trigger zone!");
            isPlayerInRange = false;
            
            // Tự đóng UI nếu đi ra ngoài xa
            if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
            {
                ShopManager.Instance.CloseShop();
            }
        }
    }
}
