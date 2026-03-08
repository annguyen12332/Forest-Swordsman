using UnityEngine;
using UnityEngine.SceneManagement;
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
        // Ghi lại scene đang chơi trước khi lưu. Bỏ qua MainMenu để không làm hỏng nút Continue.
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "MainMenu")
        {
            Data.lastSceneName = currentScene;
        }

        string json = JsonUtility.ToJson(Data, true);
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

    // Trả về true nếu tồn tại save file và có scene hợp lệ
    public bool HasSave()
    {
        return File.Exists(saveFilePath) && !string.IsNullOrEmpty(Data.lastSceneName) && Data.lastSceneName != "MainMenu";
    }

    // Reset toàn bộ dữ liệu (dùng khi New Game)
    public void ResetData()
    {
        Data = new GameData();
        if (File.Exists(saveFilePath))
            File.Delete(saveFilePath);
        Debug.Log("Đã xóa save cũ, bắt đầu game mới.");
    }

    private void OnApplicationQuit()
    {
        SaveGame(); // Tự động lưu game khi tắt hẳn cửa sổ
    }
}
