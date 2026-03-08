using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        FetchUIReferences();

        // Khôi phục lượng vàng từ Save Data
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            currentGold = SaveManager.Instance.Data.currentGold;
        }

        RefreshGoldUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hudGoldText = null;
        inventoryGoldText = null;
        FetchUIReferences();
        RefreshGoldUI();
    }

    private void FetchUIReferences()
    {
        if (hudGoldText == null || inventoryGoldText == null)
        {
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var t in allTexts)
            {
                if (hudGoldText == null && t.gameObject.name == "Gold Amount Text")
                    hudGoldText = t;
                if (inventoryGoldText == null && t.gameObject.name == "Inventory Gold Text")
                    inventoryGoldText = t;
                if (hudGoldText != null && inventoryGoldText != null)
                    break;
            }
        }
    }

    public void UpdateCurrentGold(int amount = 1)
    {
        currentGold += amount;
        Debug.Log($"[EconomyManager] Nhặt xu! currentGold = {currentGold}");
        RefreshGoldUI();

        // Lưu vàng
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.currentGold = currentGold;
            SaveManager.Instance.SaveGame();
        }
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


