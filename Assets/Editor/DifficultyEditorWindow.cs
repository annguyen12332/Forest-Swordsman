using UnityEngine;
using UnityEditor;
using System.IO;

public class DifficultyEditorWindow : EditorWindow
{
    private DifficultyConfigData configData;
    private string folderPath;
    private string filePath;

    [MenuItem("Tools/Quản Lý Độ Khó (JSON)")]
    public static void ShowWindow()
    {
        GetWindow<DifficultyEditorWindow>("Cài đặt Độ Khó");
    }

    private void OnEnable()
    {
        folderPath = Path.Combine(Application.dataPath, "StreamingAssets");
        filePath = Path.Combine(folderPath, "DifficultyConfig.json");
        LoadData();
    }

    private void LoadData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            configData = JsonUtility.FromJson<DifficultyConfigData>(json);
        }
        else
        {
            configData = new DifficultyConfigData();
        }
    }

    private void SaveData()
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string json = JsonUtility.ToJson(configData, true);
        File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();
        Debug.Log("Đã lưu cấu hình độ khó vào: " + filePath);
    }

    private void OnGUI()
    {
        GUILayout.Label("Chỉnh sửa cấu hình độ khó (JSON)", EditorStyles.boldLabel);

        if (configData == null) LoadData();

        DrawDifficultyField("Easy (Dễ)", configData.easy);
        DrawDifficultyField("Normal (Thường)", configData.normal);
        DrawDifficultyField("Hard (Khó)", configData.hard);

        GUILayout.Space(15);
        if (GUILayout.Button("LƯU VÀO JSON", GUILayout.Height(30)))
        {
            SaveData();
        }
        if (GUILayout.Button("TẢI LẠI JSON", GUILayout.Height(20)))
        {
            LoadData();
        }
    }

    private void DrawDifficultyField(string label, DifficultySettings settings)
    {
        GUILayout.Space(10);
        GUILayout.Label(label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        settings.baseHpMultiplier = EditorGUILayout.FloatField("Hệ số máu gốc", settings.baseHpMultiplier);
        settings.hpIncreasePerLevel = EditorGUILayout.FloatField("Máu tăng cộng dồn / Level", settings.hpIncreasePerLevel);
        settings.attackMultiplier = EditorGUILayout.FloatField("Hệ số sát thương", settings.attackMultiplier);
        EditorGUI.indentLevel--;
    }
}
