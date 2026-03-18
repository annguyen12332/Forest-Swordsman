using UnityEngine;
using System.IO;

[System.Serializable]
public class DifficultySettings
{
    public float baseHpMultiplier;
    public float hpIncreasePerLevel;
    public float attackMultiplier;
}

[System.Serializable]
public class DifficultyConfigData
{
    public DifficultySettings easy = new DifficultySettings { baseHpMultiplier = 0.8f, hpIncreasePerLevel = 1.0f, attackMultiplier = 0.8f };
    public DifficultySettings normal = new DifficultySettings { baseHpMultiplier = 1.0f, hpIncreasePerLevel = 2.0f, attackMultiplier = 1.0f };
    public DifficultySettings hard = new DifficultySettings { baseHpMultiplier = 1.5f, hpIncreasePerLevel = 3.0f, attackMultiplier = 1.5f };
}

public enum GameDifficulty { Easy, Normal, Hard }

public class DifficultyManager : MonoBehaviour
{
    private static DifficultyManager instance;
    public static DifficultyManager Instance 
    { 
        get 
        {
            if (instance == null)
            {
                // Try finding it
                instance = FindObjectOfType<DifficultyManager>();
                if (instance == null)
                {
                    // Create a new one if not found
                    GameObject obj = new GameObject("DifficultyManager");
                    instance = obj.AddComponent<DifficultyManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        } 
    }
    
    public GameDifficulty CurrentDifficulty = GameDifficulty.Normal;
    public DifficultyConfigData ConfigData;

    private const string FILE_NAME = "DifficultyConfig.json";

    private void Awake()
    {
        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
        }
        else 
        { 
            instance = this; 
            DontDestroyOnLoad(gameObject); 
            LoadConfig();
        }
    }

    public void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, FILE_NAME);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ConfigData = JsonUtility.FromJson<DifficultyConfigData>(json);
        }
        else
        {
            ConfigData = new DifficultyConfigData();
            // Fallback nếu chưa có file
        }
    }

    public void SetDifficulty(int difficultyIndex)
    {
        CurrentDifficulty = (GameDifficulty)difficultyIndex;
    }


// Lấy thông số hiện tại dựa trên độ khó đã chọn
    public DifficultySettings GetCurrentSettings()
    {
        if (ConfigData == null) LoadConfig();
        
        switch (CurrentDifficulty)
        {
            case GameDifficulty.Easy: return ConfigData.easy;
            case GameDifficulty.Hard: return ConfigData.hard;
            default: return ConfigData.normal;
        }
    }
}
