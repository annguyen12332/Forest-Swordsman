using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject noSavePanel;

    [Header("Keyboard Navigation - nút được chọn mặc định khi mở panel")]
    [SerializeField] private Button defaultMainButton;
    [SerializeField] private Button defaultSettingsButton;

    [Header("Scene")]
    [SerializeField] private string newGameScene = "Town";

    private void Start()
    {
        // Nếu đang có màn hình UIFade đen (ví dụ: vừa rời gameplay),
        // fade về trong suốt để main menu hiện thị đúng cách.
        if (UIFade.Instance != null && UIFade.Instance.isActiveAndEnabled)
        {
            UIFade.Instance.FadeToClear();
        }

        // Phương án X: đảm bảo system cursor luôn hiện ở Main Menu
        // (tránh bị kẹt ẩn khi chuyển từ gameplay/pause sang menu).
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        ShowMain();
    }

    private void Update()
    {
        // ESC trong Settings → quay về Main
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
                BackFromSettings();
            else if (noSavePanel != null && noSavePanel.activeSelf)
                HideNoSave();
        }
    }

    private void SelectButton(Button btn)
    {
        if (btn == null) return;
        EventSystem.current?.SetSelectedGameObject(btn.gameObject);
    }

    // Bắt đầu game mới: xóa save cũ, load scene đầu tiên
    public void NewGame()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.ResetData();

        SceneManager.LoadScene(newGameScene);
    }

    // Tiếp tục từ lần chơi trước: load scene đã lưu
    public void Continue()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            SceneManager.LoadScene(SaveManager.Instance.Data.lastSceneName);
        }
        else
        {
            ShowNoSave();
        }
    }

    public void ShowSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        SelectButton(defaultSettingsButton);
    }

    public void BackFromSettings()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        SelectButton(defaultMainButton);
    }

    public void ShowNoSave()
    {
        if (noSavePanel != null)
            noSavePanel.SetActive(true);
    }

    public void HideNoSave()
    {
        if (noSavePanel != null)
            noSavePanel.SetActive(false);
        SelectButton(defaultMainButton);
    }

    public void ShowMain()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (noSavePanel != null) noSavePanel.SetActive(false);
        mainPanel.SetActive(true);
        SelectButton(defaultMainButton);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
