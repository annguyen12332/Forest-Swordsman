using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự động thiết lập NPC cổng Boss trong scene Town và đảm bảo Scene_Boss
/// có trong Build Settings.
///
/// Menu: Tools ▶ Town Setup ▶ ▶ Full Setup
///           hoặc chạy từng bước riêng lẻ.
/// </summary>
public static class TownSetup
{
    // ── Paths ─────────────────────────────────────────────────────────────────
    private const string TOWN_SCENE_PATH   = "Assets/Scenes/Town.unity";
    private const string BOSS_SCENE_PATH   = "Assets/Scenes/Scene_Boss.unity";
    private const string NPC_PREFAB_PATH   = "Assets/Prefabs/NPCs/npc_4.prefab";
    private const string FONT_ASSET_PATH   = "Assets/Sprites/UI/Gixel SDF.asset";
    private const string UI_BOX_SPRITE     = "Assets/Sprites/UI/UI_Box.png";

    // Boss scene name used by BossPortalNPC (must match entry in Build Settings)
    private const string BOSS_SCENE_NAME   = "Scene_Boss";

    // NPC world-space position in Town (adjust via Inspector after if needed)
    private static readonly Vector3 NPC_POSITION   = new Vector3(-2f, 0f, 0f);
    private static readonly Vector3 LABEL_OFFSET   = new Vector3(0f, 1.6f, 0f);

    // ═════════════════════════════════════════════════════════════════════════
    //  MENU ITEMS
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Town Setup/▶ Full Setup (All Steps)", priority = 0)]
    public static void FullSetup()
    {
        Step1_AddBossPortalNPCToTown();
        Step2_AddBossSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Town Setup hoàn tất",
            "✔ NPC cổng Boss đã được thêm vào Town.\n" +
            "✔ Scene_Boss đã có trong Build Settings.\n\n" +
            "Xem Console để biết chi tiết.", "OK");
    }

    [MenuItem("Tools/Town Setup/① Thêm NPC Boss Portal vào Town", priority = 11)]
    public static void Step1_AddBossPortalNPCToTown()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[TownSetup] Bị hủy bởi người dùng.");
            return;
        }

        // Guard: scene must exist
        if (!System.IO.File.Exists(System.IO.Path.GetFullPath(TOWN_SCENE_PATH)))
        {
            Debug.LogError($"[TownSetup] Không tìm thấy Town scene tại {TOWN_SCENE_PATH}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(TOWN_SCENE_PATH, OpenSceneMode.Single);

        // Guard: không tạo trùng
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<BossPortalNPC>() != null)
            {
                Debug.LogWarning("[TownSetup] BossPortalNPC đã tồn tại trong Town scene. Bỏ qua.");
                return;
            }
        }

        // ── 1. NPC visual (dùng prefab npc_4) ────────────────────────────────
        var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NPC_PREFAB_PATH);
        GameObject npcGo;
        if (npcPrefab != null)
        {
            npcGo = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab);
            npcGo.name = "NPC_BossPortal";
        }
        else
        {
            // Fallback: plain sprite renderer
            npcGo = new GameObject("NPC_BossPortal");
            npcGo.AddComponent<SpriteRenderer>().color = new Color(0.8f, 0.5f, 0.2f);
            Debug.LogWarning("[TownSetup] Không tìm thấy npc_4.prefab — tạo GameObject trống thay thế.");
        }
        npcGo.transform.position = NPC_POSITION;

        // ── 2. Trigger Collider ───────────────────────────────────────────────
        var collider = npcGo.GetComponent<BoxCollider2D>();
        if (collider == null) collider = npcGo.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size      = new Vector2(3f, 3f); // interact radius

        // ── 3. World-space Canvas + TMP dialogue text ─────────────────────────
        var canvasGo = new GameObject("DialogueCanvas");
        canvasGo.transform.SetParent(npcGo.transform, false);
        canvasGo.transform.localPosition = LABEL_OFFSET;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(5f, 2f);
        rt.localScale       = new Vector3(0.012f, 0.012f, 0.012f);

        // ── 3a. Optional background panel ────────────────────────────────────
        var uiBoxSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UI_BOX_SPRITE);
        if (uiBoxSprite != null)
        {
            var bgGo  = new GameObject("Background");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = uiBoxSprite;
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = new Color(0f, 0f, 0f, 0.75f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
        }

        // ── 3b. TMP Text ──────────────────────────────────────────────────────
        var textGo = new GameObject("DialogueText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (font != null) tmp.font = font;

        tmp.text                = "Help me save my world\n[F] Enter the Boss Realm";
        tmp.fontSize            = 28;
        tmp.alignment           = TextAlignmentOptions.Center;
        tmp.color               = Color.white;
        tmp.enableWordWrapping  = true;

        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4f, 4f);
        textRt.offsetMax = new Vector2(-4f, -4f);

        // Dialogue canvas starts hidden; BossPortalNPC.Start() will disable it.
        // Leave it visible here so the designer can see it in Scene view.
        canvasGo.SetActive(true);

        // ── 4. BossPortalNPC component ────────────────────────────────────────
        var npcComponent = npcGo.AddComponent<BossPortalNPC>();
        // Assign serialized fields via SerializedObject
        var so = new SerializedObject(npcComponent);
        so.FindProperty("bossSceneName").stringValue  = BOSS_SCENE_NAME;
        so.FindProperty("hintLine").stringValue        = "Help me save my world";
        so.FindProperty("interactLine").stringValue    = "[F] Enter the Boss Realm";
        so.FindProperty("dialogueText").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── 5. Sorting Layer — draw NPC above tilemap ─────────────────────────
        var sr = npcGo.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 5;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TownSetup] ✔ NPC_BossPortal đã được thêm vào Town scene tại vị trí " + NPC_POSITION);
    }

    [MenuItem("Tools/Town Setup/② Thêm Scene_Boss vào Build Settings", priority = 12)]
    public static void Step2_AddBossSceneToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();

        bool bossAlreadyIn = scenes.Any(s => s.path == BOSS_SCENE_PATH);
        bool townAlreadyIn = scenes.Any(s => s.path == TOWN_SCENE_PATH);

        if (!System.IO.File.Exists(System.IO.Path.GetFullPath(BOSS_SCENE_PATH)))
        {
            Debug.LogError($"[TownSetup] Scene_Boss.unity không tồn tại tại {BOSS_SCENE_PATH}. " +
                           "Hãy chạy Boss Setup trước.");
            return;
        }

        bool changed = false;

        if (!townAlreadyIn)
        {
            scenes.Add(new EditorBuildSettingsScene(TOWN_SCENE_PATH, true));
            changed = true;
            Debug.Log("[TownSetup] Đã thêm Town.unity vào Build Settings.");
        }

        if (!bossAlreadyIn)
        {
            scenes.Add(new EditorBuildSettingsScene(BOSS_SCENE_PATH, true));
            changed = true;
            Debug.Log("[TownSetup] Đã thêm Scene_Boss.unity vào Build Settings.");
        }

        if (changed)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[TownSetup] ✔ Build Settings đã được cập nhật.");
        }
        else
        {
            Debug.Log("[TownSetup] Town và Scene_Boss đã có trong Build Settings — không cần thêm.");
        }
    }
}
