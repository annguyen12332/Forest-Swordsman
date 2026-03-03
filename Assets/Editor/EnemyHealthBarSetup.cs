using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor tool that automatically adds HP bar canvas to all enemy prefabs.
/// Run via: Tools → Setup Enemy Health Bars
/// </summary>
public class EnemyHealthBarSetup : Editor
{
  private const string ENEMY_PREFAB_PATH = "Assets/Prefabs/Enemies";
  private const string SPRITE_PATH = "Assets/Sprites/UI";
  private const string SORTING_LAYER = "Default";

  [MenuItem("Tools/Setup Enemy Health Bars")]
  public static void SetupEnemyHealthBars()
  {
    var fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PATH}/Health_Bar_Fill.png");
    var borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PATH}/Health_Bar_Border.png");
    var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PATH}/Health_Bar_Heart.png");

    if (fillSprite == null || borderSprite == null || heartSprite == null)
    {
      Debug.LogError("[EnemyHealthBarSetup] Không tìm thấy sprites tại Assets/Sprites/UI/. Kiểm tra lại tên file PNG.");
      return;
    }

    var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ENEMY_PREFAB_PATH });
    int count = 0;

    foreach (var guid in prefabGuids)
    {
      string path = AssetDatabase.GUIDToAssetPath(guid);
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

      if (prefab == null || prefab.GetComponent<EnemyHealth>() == null) { continue; }

      // Skip if HP bar already exists
      if (prefab.GetComponentInChildren<EnemyHealthBar>() != null)
      {
        Debug.Log($"[EnemyHealthBarSetup] Bỏ qua '{prefab.name}' – HP bar đã tồn tại.");
        continue;
      }

      using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
      {
        AddHealthBarCanvas(scope.prefabContentsRoot, fillSprite, borderSprite, heartSprite);
        count++;
        Debug.Log($"[EnemyHealthBarSetup] Đã thêm HP bar vào '{prefab.name}'.");
      }
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log($"[EnemyHealthBarSetup] Hoàn tất! Đã setup HP bar cho {count} prefab(s).");
  }

  /// <summary>
  /// Removes GraphicRaycaster from existing HealthBarCanvas in all enemy prefabs.
  /// Run this if you see the "2 EventSystems" warning after a previous setup.
  /// Menu: Tools → Fix Enemy Health Bar (Remove EventSystem Warning)
  /// </summary>
  [MenuItem("Tools/Fix Enemy Health Bar (Remove EventSystem Warning)")]
  public static void FixExistingHealthBars()
  {
    var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ENEMY_PREFAB_PATH });
    int count = 0;

    foreach (var guid in prefabGuids)
    {
      string path = AssetDatabase.GUIDToAssetPath(guid);
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      if (prefab == null) { continue; }

      using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
      {
        var raycasters = scope.prefabContentsRoot.GetComponentsInChildren<GraphicRaycaster>(true);
        foreach (var r in raycasters)
        {
          Object.DestroyImmediate(r);
          count++;
        }
      }
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log($"[EnemyHealthBarSetup] Đã xóa {count} GraphicRaycaster. Warning 'EventSystem' sẽ không còn nữa.");
  }

  /// <summary>Builds the World Space Canvas hierarchy and attaches EnemyHealthBar component.</summary>
  private static void AddHealthBarCanvas(
    GameObject root,
    Sprite fillSprite,
    Sprite borderSprite,
    Sprite heartSprite)
  {
    // ---- Canvas (World Space) ----
    var canvasGo = new GameObject("HealthBarCanvas");
    canvasGo.transform.SetParent(root.transform, false);
    canvasGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);

    var canvas = canvasGo.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;
    canvas.sortingLayerName = SORTING_LAYER;
    canvas.sortingOrder = 10;

    // Remove GraphicRaycaster – HP bar is display-only, no mouse input needed.
    // This prevents the "2 EventSystems" warning at runtime.
    var raycaster = canvasGo.GetComponent<GraphicRaycaster>();
    if (raycaster != null) { Object.DestroyImmediate(raycaster); }

    var rectCanvas = canvasGo.GetComponent<RectTransform>();
    rectCanvas.sizeDelta = new Vector2(100f, 14f);
    rectCanvas.localScale = Vector3.one * 0.01f;

    // ---- Background (border sprite) ----
    var bgGo = new GameObject("Background");
    bgGo.transform.SetParent(canvasGo.transform, false);
    var bgImg = bgGo.AddComponent<Image>();
    bgImg.sprite = borderSprite;
    bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
    var bgRect = bgGo.GetComponent<RectTransform>();
    bgRect.anchorMin = Vector2.zero;
    bgRect.anchorMax = Vector2.one;
    bgRect.sizeDelta = Vector2.zero;

    // ---- Slider ----
    var sliderGo = new GameObject("HealthSlider");
    sliderGo.transform.SetParent(canvasGo.transform, false);
    var slider = sliderGo.AddComponent<Slider>();
    slider.minValue = 0f;
    slider.maxValue = 1f;
    slider.value = 1f;
    slider.interactable = false;
    slider.transition = Selectable.Transition.None;

    var sliderRect = sliderGo.GetComponent<RectTransform>();
    sliderRect.anchorMin = Vector2.zero;
    sliderRect.anchorMax = Vector2.one;
    sliderRect.offsetMin = new Vector2(16f, 2f);
    sliderRect.offsetMax = new Vector2(-2f, -2f);

    // Fill Area
    var fillAreaGo = new GameObject("Fill Area");
    fillAreaGo.transform.SetParent(sliderGo.transform, false);
    var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
    fillAreaRect.anchorMin = Vector2.zero;
    fillAreaRect.anchorMax = Vector2.one;
    fillAreaRect.sizeDelta = Vector2.zero;
    fillAreaRect.offsetMin = Vector2.zero;
    fillAreaRect.offsetMax = Vector2.zero;

    // Fill
    var fillGo = new GameObject("Fill");
    fillGo.transform.SetParent(fillAreaGo.transform, false);
    var fillImg = fillGo.AddComponent<Image>();
    fillImg.sprite = fillSprite;
    fillImg.color = Color.green;
    fillImg.type = Image.Type.Filled;
    fillImg.fillMethod = Image.FillMethod.Horizontal;
    var fillRect = fillGo.GetComponent<RectTransform>();
    fillRect.anchorMin = Vector2.zero;
    fillRect.anchorMax = Vector2.one;
    fillRect.sizeDelta = Vector2.zero;
    fillRect.offsetMin = Vector2.zero;
    fillRect.offsetMax = Vector2.zero;

    slider.fillRect = fillRect;

    // ---- Heart Icon ----
    var heartGo = new GameObject("HeartIcon");
    heartGo.transform.SetParent(canvasGo.transform, false);
    var heartImg = heartGo.AddComponent<Image>();
    heartImg.sprite = heartSprite;
    heartImg.preserveAspect = true;
    var heartRect = heartGo.GetComponent<RectTransform>();
    heartRect.anchorMin = new Vector2(0f, 0.5f);
    heartRect.anchorMax = new Vector2(0f, 0.5f);
    heartRect.pivot = new Vector2(0.5f, 0.5f);
    heartRect.anchoredPosition = new Vector2(7f, 0f);
    heartRect.sizeDelta = new Vector2(13f, 13f);

    // ---- EnemyHealthBar component ----
    var healthBarComp = sliderGo.AddComponent<EnemyHealthBar>();
    var so = new SerializedObject(healthBarComp);
    so.FindProperty("healthSlider").objectReferenceValue = slider;
    so.ApplyModifiedPropertiesWithoutUndo();
  }
}
