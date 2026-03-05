using TMPro;
using UnityEngine;

public class EconomyManager : Singleton<EconomyManager>
{
    private int currentGold = 0;

    // Kéo "Gold Amount Text" trên HUD vào đây trong Inspector
    [SerializeField] private TMP_Text hudGoldText;

    // Kéo text xu bên trong InventoryPanel vào đây trong Inspector
    [SerializeField] private TMP_Text inventoryGoldText;

    public int CurrentGold => currentGold;

    private void Start()
    {
        // Tự động tìm HUD text nếu chưa gán
        if (hudGoldText == null)
        {
            GameObject go = GameObject.Find("Gold Amount Text");
            if (go != null) hudGoldText = go.GetComponent<TMP_Text>();
            if (hudGoldText == null)
                Debug.LogWarning("EconomyManager: Không tìm thấy 'Gold Amount Text' trên HUD! Hãy gán trong Inspector.");
        }

        // Tự động tìm inventory text nếu chưa gán
        // Dùng Resources.FindObjectsOfTypeAll để tìm cả object inactive
        if (inventoryGoldText == null)
        {
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var t in allTexts)
            {
                if (t.gameObject.name == "Inventory Gold Text")
                {
                    inventoryGoldText = t;
                    break;
                }
            }
        }

        RefreshGoldUI();
    }

    public void UpdateCurrentGold(int amount = 1)
    {
        currentGold += amount;
        Debug.Log($"[EconomyManager] Nhặt xu! currentGold = {currentGold}");
        RefreshGoldUI();
    }

    public void RefreshGoldUI()
    {
        string goldStr = currentGold.ToString("D3");

        if (hudGoldText != null)
            hudGoldText.text = goldStr;

        if (inventoryGoldText != null)
            inventoryGoldText.text = goldStr;
    }
}


