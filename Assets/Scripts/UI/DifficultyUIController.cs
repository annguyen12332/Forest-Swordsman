using UnityEngine;
using TMPro; // Sử dụng TextMeshPro

public class DifficultyUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown difficultyDropdown;

    private void Start()
    {
        // Tự động tìm TMP_Dropdown nếu chưa được gán
        if (difficultyDropdown == null)
        {
            difficultyDropdown = GetComponent<TMP_Dropdown>();
        }

        if (difficultyDropdown != null)
        {
            // Thiết lập giá trị ban đầu khớp với độ khó hiện lưu trong hệ thống manager (Easy = 0, Normal = 1, Hard = 2)
            difficultyDropdown.value = (int)DifficultyManager.Instance.CurrentDifficulty;
            
            // Lắng nghe sự kiện thay đổi Dropdown từ người chơi
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }
    }

    private void OnDifficultyChanged(int index)
    {
        // Gọi thay đổi thông số trên hệ thống chung
        DifficultyManager.Instance.SetDifficulty(index);
    }
}
