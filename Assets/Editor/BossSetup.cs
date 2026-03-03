using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor automation for boss setup.
/// Menu:
///   Tools → Boss → 1. Create Boss Stats Asset
///   Tools → Boss → 2. Create Boss Prefab
/// Run them IN ORDER after Unity compiles all boss scripts.
/// </summary>
public class BossSetup : Editor
{
  private const string BOSS_SCRIPT_PATH    = "Assets/Scripts/Boss";
  private const string SPRITES_PATH        = "Assets/Sprites/Characters/Boss";
  private const string ANIM_PATH           = "Assets/Animations/Boss/DeathBoss.controller";
  private const string PREFAB_SAVE_PATH    = "Assets/Prefabs/Enemies/Boss.prefab";
  private const string STATS_SO_SAVE_PATH  = "Assets/ScriptableObjects/Boss/DeathBossStats.asset";
  private const string DEATH_VFX_PATH      = "Assets/Prefabs/VFX/Base Death VFX.prefab";
  private const string SPRITE_PATH_UI      = "Assets/Sprites/UI";

  // ── Step 1 ────────────────────────────────────────────────────────────

  [MenuItem("Tools/Boss/1. Create Boss Stats Asset")]
  public static void CreateBossStatsAsset()
  {
    EnsureDirectory("Assets/ScriptableObjects/Boss");

    // Check if already exists
    var existing = AssetDatabase.LoadAssetAtPath<BossStatsSO>(STATS_SO_SAVE_PATH);
    if (existing != null)
    {
      Debug.Log("[BossSetup] BossStats asset đã tồn tại. Chọn nó trong Project để chỉnh sửa.");
      Selection.activeObject = existing;
      return;
    }

    var so = ScriptableObject.CreateInstance<BossStatsSO>();
    AssetDatabase.CreateAsset(so, STATS_SO_SAVE_PATH);
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Selection.activeObject = so;
    Debug.Log($"[BossSetup] Đã tạo BossStats tại {STATS_SO_SAVE_PATH}. Chỉnh sửa stats trong Inspector.");
  }

  // ── Step 2 ────────────────────────────────────────────────────────────

  [MenuItem("Tools/Boss/2. Create Boss Prefab")]
  public static void CreateBossPrefab()
  {
    EnsureDirectory("Assets/Prefabs/Enemies");

    // Load dependencies
    var statsAsset   = AssetDatabase.LoadAssetAtPath<BossStatsSO>(STATS_SO_SAVE_PATH);
    var animCtrl     = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_PATH);
    var deathVFX     = AssetDatabase.LoadAssetAtPath<GameObject>(DEATH_VFX_PATH);
    var fillSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PATH_UI}/Health_Bar_Fill.png");
    var borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PATH_UI}/Health_Bar_Border.png");
    var heartSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PATH_UI}/Health_Bar_Heart.png");

    if (statsAsset == null)
    {
      Debug.LogError("[BossSetup] Chưa tạo BossStats! Chạy Tools → Boss → 1. Create Boss Stats Asset trước.");
      return;
    }

    // ── Root ──
    var root = new GameObject("Boss");
    var rb = root.AddComponent<Rigidbody2D>();
    rb.gravityScale = 0f;
    rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

    var col = root.AddComponent<CapsuleCollider2D>();
    col.size   = new Vector2(0.5f, 0.7f);
    col.offset = new Vector2(0f, -0.1f);

    root.AddComponent<Flash>();
    root.AddComponent<Knockback>();

    var bossCtrl             = root.AddComponent<BossController>();
    // Set stats via SerializedObject so private SerializeField is set
    var soCtrl               = new SerializedObject(bossCtrl);
    soCtrl.FindProperty("stats").objectReferenceValue    = statsAsset;
    if (deathVFX != null)
      soCtrl.FindProperty("deathVFXPrefab").objectReferenceValue = deathVFX;
    soCtrl.ApplyModifiedPropertiesWithoutUndo();

    SetTagSafe(root, "Enemy");
    SetLayerSafe(root, "Enemy");

    // ── GFX child ──
    var gfx             = new GameObject("GFX");
    gfx.transform.SetParent(root.transform, false);
    var sr              = gfx.AddComponent<SpriteRenderer>();
    sr.sortingLayerName = "Foreground";
    sr.sortingOrder     = 1;
    // Load idle sprite as default
    var idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/idle2.png");
    if (idleSprite != null) { sr.sprite = idleSprite; }

    if (animCtrl != null)
    {
      var anim       = gfx.AddComponent<Animator>();
      anim.runtimeAnimatorController = animCtrl;
    }
    else
    {
      Debug.LogWarning("[BossSetup] Không tìm thấy DeathBoss.controller – gắn thủ công sau.");
    }

    // ── Flash material – reuse from existing enemy (optional) ──
    var flashComp  = root.GetComponent<Flash>();
    var flashMat   = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WhiteFlash.mat");
    if (flashMat != null)
    {
      var soFlash = new SerializedObject(flashComp);
      soFlash.FindProperty("whiteFlashMat").objectReferenceValue = flashMat;
      soFlash.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Attack Hitbox child ──
    var hitboxGo = new GameObject("AttackHitbox");
    hitboxGo.transform.SetParent(root.transform, false);
    var hitboxCol            = hitboxGo.AddComponent<CircleCollider2D>();
    hitboxCol.isTrigger      = true;
    hitboxCol.radius         = 0.8f;
    hitboxGo.AddComponent<BossAttackHitbox>();

    // ── HP Bar Canvas child ──
    if (fillSprite != null && borderSprite != null && heartSprite != null)
    {
      AddHealthBarCanvas(root, fillSprite, borderSprite, heartSprite);
    }
    else
    {
      Debug.LogWarning("[BossSetup] Không tìm thấy sprites HP bar. Chạy Tools → Setup Enemy Health Bars sau.");
    }

    // ── Save as Prefab ──
    bool success;
    PrefabUtility.SaveAsPrefabAsset(root, PREFAB_SAVE_PATH, out success);
    DestroyImmediate(root);

    if (success)
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_SAVE_PATH);
      Selection.activeObject = prefab;
      Debug.Log($"[BossSetup] Đã tạo Boss prefab tại {PREFAB_SAVE_PATH}.");
    }
    else
    {
      Debug.LogError("[BossSetup] Lỗi khi lưu prefab!");
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────

  private static void EnsureDirectory(string path)
  {
    if (!AssetDatabase.IsValidFolder(path))
    {
      var parts  = path.Split('/');
      string cur = parts[0];
      for (int i = 1; i < parts.Length; i++)
      {
        string next = cur + "/" + parts[i];
        if (!AssetDatabase.IsValidFolder(next))
        {
          AssetDatabase.CreateFolder(cur, parts[i]);
        }
        cur = next;
      }
    }
  }

  private static void AddHealthBarCanvas(
    GameObject root, Sprite fillSprite, Sprite borderSprite, Sprite heartSprite)
  {
    var canvasGo = new GameObject("HealthBarCanvas");
    canvasGo.transform.SetParent(root.transform, false);
    canvasGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);

    var canvas              = canvasGo.AddComponent<Canvas>();
    canvas.renderMode       = RenderMode.WorldSpace;
    canvas.sortingLayerName = "Foreground";
    canvas.sortingOrder     = 10;

    // Remove GraphicRaycaster to prevent duplicate EventSystem warning
    var raycaster = canvasGo.GetComponent<GraphicRaycaster>();
    if (raycaster != null) { DestroyImmediate(raycaster); }

    var rectCanvas           = canvasGo.GetComponent<RectTransform>();
    rectCanvas.sizeDelta     = new Vector2(120f, 16f);
    rectCanvas.localScale    = Vector3.one * 0.01f;

    // Background
    var bgGo    = new GameObject("Background");
    bgGo.transform.SetParent(canvasGo.transform, false);
    var bgImg   = bgGo.AddComponent<Image>();
    bgImg.sprite = borderSprite;
    bgImg.color  = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    var bgRect  = bgGo.GetComponent<RectTransform>();
    bgRect.anchorMin = Vector2.zero;
    bgRect.anchorMax = Vector2.one;
    bgRect.sizeDelta = Vector2.zero;

    // Slider
    var sliderGo             = new GameObject("HealthSlider");
    sliderGo.transform.SetParent(canvasGo.transform, false);
    var slider               = sliderGo.AddComponent<Slider>();
    slider.minValue          = 0f;
    slider.maxValue          = 1f;
    slider.value             = 1f;
    slider.interactable      = false;
    slider.transition        = Selectable.Transition.None;
    var sliderRect           = sliderGo.GetComponent<RectTransform>();
    sliderRect.anchorMin     = Vector2.zero;
    sliderRect.anchorMax     = Vector2.one;
    sliderRect.offsetMin     = new Vector2(18f, 2f);
    sliderRect.offsetMax     = new Vector2(-2f, -2f);

    var fillAreaGo           = new GameObject("Fill Area");
    fillAreaGo.transform.SetParent(sliderGo.transform, false);
    var fillAreaRect         = fillAreaGo.AddComponent<RectTransform>();
    fillAreaRect.anchorMin   = Vector2.zero;
    fillAreaRect.anchorMax   = Vector2.one;
    fillAreaRect.offsetMin   = Vector2.zero;
    fillAreaRect.offsetMax   = Vector2.zero;

    var fillGo               = new GameObject("Fill");
    fillGo.transform.SetParent(fillAreaGo.transform, false);
    var fillImg              = fillGo.AddComponent<Image>();
    fillImg.sprite           = fillSprite;
    fillImg.color            = new Color(0.2f, 0.8f, 0.2f);
    fillImg.type             = Image.Type.Filled;
    fillImg.fillMethod       = Image.FillMethod.Horizontal;
    var fillRect             = fillGo.GetComponent<RectTransform>();
    fillRect.anchorMin       = Vector2.zero;
    fillRect.anchorMax       = Vector2.one;
    fillRect.offsetMin       = Vector2.zero;
    fillRect.offsetMax       = Vector2.zero;
    slider.fillRect          = fillRect;

    // Heart Icon
    var heartGo              = new GameObject("HeartIcon");
    heartGo.transform.SetParent(canvasGo.transform, false);
    var heartImg             = heartGo.AddComponent<Image>();
    heartImg.sprite          = heartSprite;
    heartImg.preserveAspect  = true;
    var heartRect            = heartGo.GetComponent<RectTransform>();
    heartRect.anchorMin      = new Vector2(0f, 0.5f);
    heartRect.anchorMax      = new Vector2(0f, 0.5f);
    heartRect.pivot          = new Vector2(0.5f, 0.5f);
    heartRect.anchoredPosition = new Vector2(8f, 0f);
    heartRect.sizeDelta      = new Vector2(14f, 14f);

    // EnemyHealthBar component
    var hpBar  = sliderGo.AddComponent<EnemyHealthBar>();
    var soHp   = new SerializedObject(hpBar);
    soHp.FindProperty("healthSlider").objectReferenceValue = slider;
    soHp.ApplyModifiedPropertiesWithoutUndo();
  }
  /// <summary>Sets tag only if it exists in project TagManager, else warns.</summary>
  private static void SetTagSafe(GameObject go, string tag)
  {
    try
    {
      go.tag = tag;
    }
    catch
    {
      Debug.LogWarning($"[BossSetup] Tag '{tag}' chưa được định nghĩa trong project. " +
                       "Vào Edit → Project Settings → Tags and Layers để thêm.");
    }
  }

  /// <summary>Sets layer only if it exists (LayerMask.NameToLayer != -1), else warns.</summary>
  private static void SetLayerSafe(GameObject go, string layerName)
  {
    int layer = LayerMask.NameToLayer(layerName);
    if (layer >= 0)
    {
      go.layer = layer;
    }
    else
    {
      Debug.LogWarning($"[BossSetup] Layer '{layerName}' chưa được định nghĩa. " +
                       "Vào Edit → Project Settings → Tags and Layers để thêm. Dùng Default layer tạm thời.");
    }
  }
}
