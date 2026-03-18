using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultyUISetupWindow : EditorWindow
{
    private Transform settingsPanelParent;

    [MenuItem("Tools/Thêm UI Chỉnh Độ Khó Vào Menu (Tự Động)")]
    public static void ShowWindow()
    {
        GetWindow<DifficultyUISetupWindow>("Menu Setup Độ Khó");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cài Đặt Dropdown Độ Khó (TextMeshPro)", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("Bước 1: Mở Scene có chứa UI Main Menu hoặc Pause Menu của bạn.\n" +
                                "Bước 2: Kéo GameObject (như Settings Panel) vào ô trống bên dưới.\n" +
                                "Bước 3: Nhấn nút tạo.", MessageType.Info);

        GUILayout.Space(10);
        settingsPanelParent = (Transform)EditorGUILayout.ObjectField("Khung chứa (Parent):", settingsPanelParent, typeof(Transform), true);

        GUILayout.Space(15);

        GUI.enabled = settingsPanelParent != null; // Nút tạo chỉ bấm được nếu đã gán Parent
        if (GUILayout.Button("TẠO UI DROPDOWN ĐỘ KHÓ", GUILayout.Height(35)))
        {
            CreateDifficultyUI();
        }
        GUI.enabled = true;
    }

    private void CreateDifficultyUI()
    {
        if (settingsPanelParent == null) return;

        // Lưu log Scene để có thể Undo (Ctrl Z) nếu muốn
        Undo.RecordObject(settingsPanelParent.gameObject, "Create Difficulty Dropdown");

        // Gọi lệnh mặc định của Unity để sinh Template Dropdown loại TextMeshPro
        EditorApplication.ExecuteMenuItem("GameObject/UI/Dropdown - TextMeshPro");
        
        GameObject dropdownObject = Selection.activeGameObject;
        if (dropdownObject == null)
        {
            Debug.LogError("Không thể tạo Dropdown. Vui lòng đảm bảo bạn đã cài đặt gói TextMeshPro trong Package Manager.");
            return;
        }

        // Parent vào vị trí Settings Panel
        dropdownObject.transform.SetParent(settingsPanelParent, false);
        dropdownObject.name = "Dropdown_Setup_Difficulty";
        
        // Điều chỉnh vị trí (RectTransform)
        RectTransform rect = dropdownObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(0, 0); // Về giữa khung
        }

        // Cấu hình các lựa chọn mức độ
        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        if (dropdown != null)
        {
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("Easy (Dễ)"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("Normal (Thường)"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("Hard (Khó)"));
            
            // Setup màu sắc / thuộc tính nếu muốn
            dropdown.value = 1; // Giá trị Default là Normal
        }

        // Thêm Script tự động lắng nghe và chỉnh Difficulty Manager
        DifficultyUIController controller = dropdownObject.AddComponent<DifficultyUIController>();
        controller.difficultyDropdown = dropdown;

        // Báo cho Unity biết dữ liệu có thay đổi
        EditorUtility.SetDirty(dropdownObject);

        Debug.Log("✅ Thành công! Đã tự động tạo và cấu hình Dropdown chỉnh độ khó vào: " + settingsPanelParent.name, dropdownObject);
        
        // Cập nhật lại UI hiển thị
        Selection.activeGameObject = dropdownObject;
    }
}
