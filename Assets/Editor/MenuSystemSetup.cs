using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Tự động tạo toàn bộ hệ thống Menu cho Forest-Swordsman.
/// Menu: Tools → Menu System → ...
/// Chạy theo thứ tự hoặc dùng "▶ Run All Steps".
/// </summary>
public class MenuSystemSetup : Editor
{
    // ── Paths ────────────────────────────────────────────────────────────
    private const string UI_CANVAS_PREFAB = "Assets/Prefabs/Scene Management/UI Canvas.prefab";
    private const string MAIN_MENU_SCENE  = "Assets/Scenes/MainMenu.unity";

    // ── Color Palette ────────────────────────────────────────────────────
    private static readonly Color COLOR_BG          = new Color(0.07f, 0.05f, 0.10f, 1.00f);
    private static readonly Color COLOR_PANEL        = new Color(0.00f, 0.00f, 0.00f, 0.00f);
    private static readonly Color COLOR_OVERLAY      = new Color(0.00f, 0.00f, 0.00f, 0.78f);
    private static readonly Color COLOR_SETTINGS_BG  = new Color(0.08f, 0.08f, 0.13f, 0.97f);
    private static readonly Color COLOR_NOSAVE_BG    = new Color(0.20f, 0.04f, 0.04f, 0.97f);
    private static readonly Color COLOR_BTN_NORMAL   = new Color(0.22f, 0.22f, 0.28f, 1.00f);
    private static readonly Color COLOR_BTN_HOVER    = new Color(0.38f, 0.38f, 0.46f, 1.00f);
    private static readonly Color COLOR_BTN_PRESS    = new Color(0.13f, 0.13f, 0.18f, 1.00f);
    private static readonly Color COLOR_SLIDER_TRACK = new Color(0.20f, 0.20f, 0.20f, 1.00f);
    private static readonly Color COLOR_SLIDER_FILL  = new Color(0.40f, 0.70f, 1.00f, 1.00f);

    // =====================================================================
    // MENU ITEMS
    // =====================================================================

    [MenuItem("Tools/Menu System/▶ Run All Steps")]
    public static void RunAll()
    {
        Step1_CreateMainMenuScene();
        Step2_AddPauseMenuToUICanvas();
        Step3_UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Hoàn thành",
            "Hệ thống Menu đã được thiết lập thành công!\nXem Console để biết chi tiết.", "OK");
    }

    [MenuItem("Tools/Menu System/1. Tạo Scene MainMenu")]
    public static void Step1_CreateMainMenuScene()
    {
        if (System.IO.File.Exists(System.IO.Path.GetFullPath(MAIN_MENU_SCENE)))
        {
            if (!EditorUtility.DisplayDialog("Scene đã tồn tại",
                    $"'{MAIN_MENU_SCENE}' đã tồn tại. Ghi đè?", "Ghi đè", "Hủy"))
                return;
        }

        // ── Tạo scene mới trống ──────────────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ───────────────────────────────────────────────────────
        // Cần ít nhất 1 camera để Unity clear display, dù Canvas là Screen Space Overlay
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.05f, 0.10f, 1f);
        cam.orthographic = true;
        cam.depth = -1;
        cameraGO.AddComponent<AudioListener>();

        // ── EventSystem ──────────────────────────────────────────────────
        var eventSysGO = new GameObject("EventSystem");
        eventSysGO.AddComponent<EventSystem>();
        eventSysGO.AddComponent<StandaloneInputModule>();
        eventSysGO.AddComponent<EventSystemGuard>(); // tự hủy duplicate khi có 2 EventSystem

        // ── SaveManager (chỉ tạo riêng lẻ, KHÔNG instantiate toàn bộ Managers.prefab
        //    vì CameraController/EconomyManager trong đó cần Player/HUD không có ở MainMenu) ──
        var smGO = new GameObject("SaveManager");
        smGO.AddComponent<SaveManager>();
        Debug.Log("[MenuSetup] Đã tạo SaveManager trong scene MainMenu.");

        // ── Canvas ───────────────────────────────────────────────────────
        var canvasGO = new GameObject("Menu Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Nền toàn màn hình ────────────────────────────────────────────
        CreateStretchPanel(canvasGO, "Background", COLOR_BG);

        // ── Main Panel ───────────────────────────────────────────────────
        var mainPanel = CreateStretchPanel(canvasGO, "MainPanel", COLOR_PANEL);

        CreateLabel(mainPanel, "Title", "FOREST SWORDSMAN",
            pos: new Vector2(0, 300), size: new Vector2(700, 90), fontSize: 52);

        var btnNewGame  = CreateButton(mainPanel, "NewGame_Btn",  "New Game",  new Vector2(0,  90));
        var btnContinue = CreateButton(mainPanel, "Continue_Btn", "Continue",  new Vector2(0,  10));
        var btnSettings = CreateButton(mainPanel, "Settings_Btn", "Settings",  new Vector2(0, -70));
        var btnQuit     = CreateButton(mainPanel, "Quit_Btn",     "Quit",      new Vector2(0,-155));

        // ── Settings Panel ───────────────────────────────────────────────
        var settingsPanel = CreateStretchPanel(canvasGO, "SettingsPanel", COLOR_SETTINGS_BG);
        settingsPanel.SetActive(false);

        CreateLabel(settingsPanel, "SettingsTitle", "SETTINGS",
            pos: new Vector2(0, 230), size: new Vector2(400, 70), fontSize: 42);
        CreateLabel(settingsPanel, "VolumeLabel", "Master Volume",
            pos: new Vector2(0, 80), size: new Vector2(350, 40), fontSize: 26);

        var masterSlider = CreateSlider(settingsPanel, "MasterVolume_Slider",
            pos: new Vector2(0, 20), size: new Vector2(450, 45));

        var btnSetBack = CreateButton(settingsPanel, "SettingsBack_Btn", "Back", new Vector2(0, -120));

        var settingsCtrl = settingsPanel.AddComponent<SettingsController>();
        var soSettings = new SerializedObject(settingsCtrl);
        soSettings.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
        soSettings.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(masterSlider.onValueChanged,
            new UnityEngine.Events.UnityAction<float>(settingsCtrl.OnMasterVolumeChanged));

        // ── No-Save Panel ────────────────────────────────────────────────
        var noSavePanel = CreateCenteredPanel(canvasGO, "NoSavePanel",
            size: new Vector2(520, 220), color: COLOR_NOSAVE_BG);
        noSavePanel.SetActive(false);

        CreateLabel(noSavePanel, "NoSaveMsg",
            "Chưa có lưu trữ!\nHãy bắt đầu New Game trước.",
            pos: new Vector2(0, 40), size: new Vector2(480, 90), fontSize: 22);
        var btnNoSaveOK = CreateButton(noSavePanel, "NoSaveOK_Btn", "OK", new Vector2(0, -60));

        // ── MainMenuController ───────────────────────────────────────────
        var menuCtrl = canvasGO.AddComponent<MainMenuController>();
        var soMenu = new SerializedObject(menuCtrl);
        soMenu.FindProperty("mainPanel").objectReferenceValue     = mainPanel;
        soMenu.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        soMenu.FindProperty("noSavePanel").objectReferenceValue   = noSavePanel;
        soMenu.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(btnNewGame.onClick,  menuCtrl.NewGame);
        UnityEventTools.AddPersistentListener(btnContinue.onClick, menuCtrl.Continue);
        UnityEventTools.AddPersistentListener(btnSettings.onClick, menuCtrl.ShowSettings);
        UnityEventTools.AddPersistentListener(btnQuit.onClick,     menuCtrl.QuitGame);
        UnityEventTools.AddPersistentListener(btnSetBack.onClick,  menuCtrl.BackFromSettings);
        UnityEventTools.AddPersistentListener(btnNoSaveOK.onClick, menuCtrl.HideNoSave);

        // ── Lưu scene ────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE);
        AssetDatabase.Refresh();
        Debug.Log($"[MenuSetup] ✅ Đã tạo scene MainMenu tại: {MAIN_MENU_SCENE}");
    }

    [MenuItem("Tools/Menu System/2. Thêm Pause Menu vào UI Canvas")]
    public static void Step2_AddPauseMenuToUICanvas()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(UI_CANVAS_PREFAB);
        if (prefabRoot == null)
        {
            Debug.LogError($"[MenuSetup] Không tìm thấy UI Canvas prefab tại: {UI_CANVAS_PREFAB}");
            return;
        }

        if (prefabRoot.GetComponentInChildren<PauseMenuController>() != null)
        {
            Debug.LogWarning("[MenuSetup] PauseMenuController đã tồn tại trong UI Canvas. Bỏ qua bước này.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // ── Pause Panel ──────────────────────────────────────────────────
        var pausePanel = CreateStretchPanel(prefabRoot, "PausePanel", COLOR_OVERLAY);
        pausePanel.SetActive(false);

        CreateLabel(pausePanel, "PauseTitle", "PAUSED",
            pos: new Vector2(0, 230), size: new Vector2(300, 70), fontSize: 42);

        var btnResume     = CreateButton(pausePanel, "Resume_Btn",      "Resume",           new Vector2(0,  100));
        var btnPauseSet   = CreateButton(pausePanel, "PauseSettings_Btn","Settings",          new Vector2(0,   15));
        var btnSaveReturn = CreateButton(pausePanel, "SaveReturn_Btn",   "Save & Main Menu", new Vector2(0,  -70));
        var btnQuitMenu   = CreateButton(pausePanel, "QuitMenu_Btn",     "Quit to Menu",     new Vector2(0, -155));

        // ── Pause Settings Panel ─────────────────────────────────────────
        var pauseSetPanel = CreateStretchPanel(prefabRoot, "PauseSettingsPanel", COLOR_SETTINGS_BG);
        pauseSetPanel.SetActive(false);

        CreateLabel(pauseSetPanel, "SettingsTitle", "SETTINGS",
            pos: new Vector2(0, 230), size: new Vector2(400, 70), fontSize: 42);
        CreateLabel(pauseSetPanel, "VolumeLabel", "Master Volume",
            pos: new Vector2(0, 80), size: new Vector2(350, 40), fontSize: 26);

        var pauseSlider = CreateSlider(pauseSetPanel, "MasterVolume_Slider",
            pos: new Vector2(0, 20), size: new Vector2(450, 45));

        var btnPauseSetBack = CreateButton(pauseSetPanel, "PauseSetBack_Btn", "Back", new Vector2(0, -120));

        var pauseSettingsCtrl = pauseSetPanel.AddComponent<SettingsController>();
        var soPauseSettings = new SerializedObject(pauseSettingsCtrl);
        soPauseSettings.FindProperty("masterVolumeSlider").objectReferenceValue = pauseSlider;
        soPauseSettings.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(pauseSlider.onValueChanged,
            new UnityEngine.Events.UnityAction<float>(pauseSettingsCtrl.OnMasterVolumeChanged));

        // ── PauseMenuController lên Canvas root ──────────────────────────
        var pauseCtrl = prefabRoot.AddComponent<PauseMenuController>();
        var soPause = new SerializedObject(pauseCtrl);
        soPause.FindProperty("pausePanel").objectReferenceValue    = pausePanel;
        soPause.FindProperty("settingsPanel").objectReferenceValue = pauseSetPanel;
        soPause.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(btnResume.onClick,      pauseCtrl.Resume);
        UnityEventTools.AddPersistentListener(btnPauseSet.onClick,    pauseCtrl.OpenSettings);
        UnityEventTools.AddPersistentListener(btnSaveReturn.onClick,  pauseCtrl.SaveAndReturn);
        UnityEventTools.AddPersistentListener(btnQuitMenu.onClick,    pauseCtrl.QuitToMenu);
        UnityEventTools.AddPersistentListener(btnPauseSetBack.onClick,pauseCtrl.BackFromSettings);

        // ── Lưu prefab ───────────────────────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, UI_CANVAS_PREFAB);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.Refresh();
        Debug.Log($"[MenuSetup] ✅ Đã thêm Pause Menu vào: {UI_CANVAS_PREFAB}");
    }

    [MenuItem("Tools/Menu System/3. Cập nhật Build Settings")]
    public static void Step3_UpdateBuildSettings()
    {
        var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // Xóa entry cũ (nếu đã có, tránh trùng)
        existing.RemoveAll(s => s.path == MAIN_MENU_SCENE);

        // Thêm MainMenu vào index 0 (scene đầu tiên khởi động)
        existing.Insert(0, new EditorBuildSettingsScene(MAIN_MENU_SCENE, true));

        EditorBuildSettings.scenes = existing.ToArray();
        Debug.Log($"[MenuSetup] ✅ Đã đưa '{MAIN_MENU_SCENE}' vào Build Settings (index 0).");
    }

    // =====================================================================
    // UI HELPERS
    // =====================================================================

    /// Panel kéo dài toàn bộ parent.
    private static GameObject CreateStretchPanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    /// Panel kích thước cố định, căn giữa parent.
    private static GameObject CreateCenteredPanel(GameObject parent, string name, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    /// Text label căn giữa tại `pos`.
    private static void CreateLabel(GameObject parent, string name, string text,
        Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.font = GetBuiltinFont();
    }

    /// Nút bấm cơ bản với text label, căn giữa tại `pos`.
    private static Button CreateButton(GameObject parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260, 58);
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.color = COLOR_BTN_NORMAL;

        // Text con
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var t = textGO.GetComponent<Text>();
        t.text = label;
        t.fontSize = 24;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.font = GetBuiltinFont();

        // Màu hover/pressed
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor      = COLOR_BTN_NORMAL;
        colors.highlightedColor = COLOR_BTN_HOVER;
        colors.pressedColor     = COLOR_BTN_PRESS;
        colors.selectedColor    = COLOR_BTN_HOVER;
        btn.colors = colors;
        return btn;
    }

    /// Slider ngang (0 → 1) tại `pos`.
    private static Slider CreateSlider(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        // Background track
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGO.transform.SetParent(root.transform, false);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgGO.GetComponent<Image>().color = COLOR_SLIDER_TRACK;

        // Fill Area
        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(root.transform, false);
        var fillAreaRt = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = new Vector2(5f, 0f);
        fillAreaRt.offsetMax = new Vector2(-15f, 0f);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = new Vector2(10f, 0f);
        fillGO.GetComponent<Image>().color = COLOR_SLIDER_FILL;

        // Handle Slide Area
        var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(root.transform, false);
        var handleAreaRt = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10f, 0f);
        handleAreaRt.offsetMax = new Vector2(-10f, 0f);

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleRt = handleGO.GetComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.sizeDelta = new Vector2(22f, 0f);
        handleGO.GetComponent<Image>().color = Color.white;

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fillGO.GetComponent<RectTransform>();
        slider.handleRect = handleGO.GetComponent<RectTransform>();
        slider.targetGraphic = handleGO.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    /// Lấy font dựng sẵn của Unity (fallback null nếu không tìm thấy).
    private static Font GetBuiltinFont()
    {
        // Unity 6 / 2023+
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        // Unity 2021 trở về
        f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f; // có thể null – Text vẫn dùng font mặc định
    }
}
