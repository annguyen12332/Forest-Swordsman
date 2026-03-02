using TMPro;
using UnityEngine;

public class EconomyManager : Singleton<EconomyManager>
{
    private int currentGold = 0;

    // Kéo "Gold Amount Text" trên HUD vào đây trong Inspector
    [SerializeField] private TMP_Text hudGoldText;

    // Kéo "Gold Amount Text" (hoặc text xu) trong InventoryPanel vào đây
    [SerializeField] private TMP_Text inventoryGoldText;

    public int CurrentGold => currentGold;

    public void UpdateCurrentGold()
    {
        currentGold += 1;
        RefreshGoldUI();
    }

    private void RefreshGoldUI()
    {
        string goldStr = currentGold.ToString("D3");

        // Cập nhật số xu trên HUD
        if (hudGoldText != null)
            hudGoldText.text = goldStr;

        // Cập nhật số xu trong Inventory Panel
        if (inventoryGoldText != null)
            inventoryGoldText.text = goldStr;
    }
}
