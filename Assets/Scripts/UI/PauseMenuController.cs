using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Không dùng Singleton vì PauseMenuController gắn vào Canvas trong scene,
// phải bị destroy khi chuyển scene (giống các UI manager khác trong dự án)
public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Keyboard Navigation - nút được chọn mặc định khi mở panel")]
    [SerializeField] private Button defaultPauseButton;
    [SerializeField] private Button defaultSettingsButton;

    [Header("Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool isPaused = false;

    // CursorManager đọc field này để hiện/ẩn cursor tùy chỉnh
    public static bool IsPauseMenuOpen { get; private set; }

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ESC trong Settings → quay lại Pause, không đóng hẳn
            if (settingsPanel != null && settingsPanel.activeSelf)
                BackFromSettings();
            else if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    // Chọn button mặc định để bàn phím có thể điều hướng ngay
    private void SelectButton(Button btn)
    {
        if (btn == null) return;
        EventSystem.current?.SetSelectedGameObject(btn.gameObject);
    }

    private void Pause()
    {
        isPaused = true;
        IsPauseMenuOpen = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        SelectButton(defaultPauseButton);
    }

    public void Resume()
    {
        isPaused = false;
        IsPauseMenuOpen = false;
        pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void OpenSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
        SelectButton(defaultSettingsButton);
    }

    public void BackFromSettings()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
        SelectButton(defaultPauseButton);
    }

    // Lưu game rồi về Main Menu
    public void SaveAndReturn()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        Time.timeScale = 1f;
        isPaused = false;
        IsPauseMenuOpen = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        StartCoroutine(LoadMenuRoutine());
    }

    // Về Main Menu không lưu
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        IsPauseMenuOpen = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        StartCoroutine(LoadMenuRoutine());
    }

    private IEnumerator LoadMenuRoutine()
    {
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack();
            yield return new WaitForSeconds(1f);
        }
        SceneManager.LoadScene(mainMenuScene);
    }
}
