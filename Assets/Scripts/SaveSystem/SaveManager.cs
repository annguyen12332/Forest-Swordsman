using UnityEngine;
using System.IO;

public class SaveManager : Singleton<SaveManager>
{
    public GameData Data = new GameData();
    private string saveFilePath;

    protected override void Awake()
    {
        base.Awake();
        saveFilePath = Application.persistentDataPath + "/savedata.json";
        LoadGame();
    }

    public void SaveGame()
    {
        // Chuyển Data thành JSON
        string json = JsonUtility.ToJson(Data, true);
        
        // Ghi vào file
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Đã lưu dữ liệu định dạng JSON tại: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            Data = JsonUtility.FromJson<GameData>(json);
            Debug.Log("Load Save Game thành công.");
        }
        else
        {
            Debug.Log("Không có file Save. Mở file Data gốc.");
            Data = new GameData();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame(); // Tự động lưu game khi tắt hẳn cửa sổ
    }
}
