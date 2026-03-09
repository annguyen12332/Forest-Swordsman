using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerLevel : Singleton<PlayerLevel>
{
    public int CurrentLevel { get; private set; } = 1;
    public int CurrentXP { get; private set; } = 0;

    [Header("Level Settings")]
    [SerializeField] private int baseXPRequirement = 100;
    [SerializeField] private float xpMultiplierPerLevel = 1.5f;

    [Header("UI References")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TMP_Text levelText; // TextMeshPro dùng cho hiển thị Level
    
    // Tìm UI theo tên nếu chưa gán
    const string XP_SLIDER_TEXT = "XP Slider";
    const string LEVEL_TEXT_OBJ = "Level Text";

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        LoadLevelData();
        UpdateUI();
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
        // Cần gán ref về null để ép hàm UpdateUI() fetch lại UI Canvas của scene mới
        xpSlider = null;
        levelText = null;
        UpdateUI();
    }

    public void AddXP(int amount)
    {
        CurrentXP += amount;
        CheckLevelUp();
        UpdateUI();
        SaveLevelData();
    }

    private void CheckLevelUp()
    {
        while (CurrentXP >= GetRequiredXPForNextLevel())
        {
            CurrentXP -= GetRequiredXPForNextLevel();
            CurrentLevel++;

            // Khi lên cấp, tăng máu và hồi đầy máu
            PlayerHealth.Instance.IncreaseMaxHealth(1); 
            // Hiệu ứng lên cấp nếu có thể thêm sau ở đây
        }
    }

    public int GetRequiredXPForNextLevel()
    {
        return Mathf.RoundToInt(baseXPRequirement * Mathf.Pow(xpMultiplierPerLevel, CurrentLevel - 1));
    }

    private void UpdateUI()
    {
        // Tự động tìm Slider nếu mất
        if (xpSlider == null)
        {
            GameObject sliderGO = GameObject.Find(XP_SLIDER_TEXT);
            if (sliderGO != null) xpSlider = sliderGO.GetComponent<Slider>();
        }

        // Tự động tìm Text Level
        if (levelText == null)
        {
            GameObject textGO = GameObject.Find(LEVEL_TEXT_OBJ);
            if (textGO != null) levelText = textGO.GetComponent<TMP_Text>();
        }

        if (xpSlider != null)
        {
            xpSlider.maxValue = GetRequiredXPForNextLevel();
            xpSlider.value = CurrentXP;
        }

        if (levelText != null)
        {
            levelText.text = "LV." + CurrentLevel;
        }
    }

    private void LoadLevelData()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            // Nếu data là 0 (mới tạo) thì gán lại bằng 1
            CurrentLevel = SaveManager.Instance.Data.playerLevel > 0 ? SaveManager.Instance.Data.playerLevel : 1; 
            CurrentXP = SaveManager.Instance.Data.playerXP;
        }
    }

    public void SaveLevelData()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.playerLevel = CurrentLevel;
            SaveManager.Instance.Data.playerXP = CurrentXP;
            // Việc ghi file sẽ do SaveManager.SaveGame() thực hiện hoặc tự động lúc tắt game
        }
    }
}
