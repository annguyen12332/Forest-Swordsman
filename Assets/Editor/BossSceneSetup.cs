using System.IO;
using System.Linq;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Unity Editor tool — automatically builds the full 3-boss gauntlet.
///
/// Run in order from:   Tools ▶ Boss Setup ▶ ① Create Boss Prefabs
///                                          ▶ ② Build Boss Scene
///                      Or just use:        ▶ ▶ Full Setup (Run Both)
/// </summary>
public static class BossSceneSetup
{
    // ── Asset paths ────────────────────────────────────────────────────────────
    private const string SLIME_PREFAB       = "Assets/Prefabs/Enemies/Slime.prefab";
    private const string GRAPE_PREFAB       = "Assets/Prefabs/Enemies/Grape/Grape.prefab";
    private const string BULLET_PREFAB      = "Assets/Prefabs/Bullet.prefab";
    private const string GRAPE_PROJ_PREFAB  = "Assets/Prefabs/Enemies/Grape/Grape Projectile.prefab";
    private const string GRAPE_SHADOW       = "Assets/Prefabs/Enemies/Grape/Grape Shadow.prefab";
    private const string GRAPE_SPLATTER     = "Assets/Prefabs/Enemies/Grape/Grape Projectile Splatter.prefab";
    private const string DEATH_VFX          = "Assets/Prefabs/VFX/Base Death VFX.prefab";
    private const string HEALTH_GLOBE       = "Assets/Prefabs/Pickups/Health Globe.prefab";
    private const string GOLD_COIN          = "Assets/Prefabs/Pickups/Gold Coin.prefab";
    private const string STAMINA_GLOBE      = "Assets/Prefabs/Pickups/Stamina Globe.prefab";
    private const string GEM_PICKUP         = "Assets/Prefabs/Pickups/GemPickup.prefab";
    private const string BOOM_PICKUP        = "Assets/Prefabs/Pickups/Boom_pickUp.prefab";
    private const string PLAYER_PREFAB      = "Assets/Prefabs/Player.prefab";
    private const string WHITE_FLASH_MAT    = "Assets/Materials/WhiteFlash.mat";
    private const string FONT_ASSET         = "Assets/Sprites/UI/Gixel SDF.asset";
    private const string HP_FILL_SPRITE     = "Assets/Sprites/UI/Health_Bar_Fill.png";
    private const string HP_BORDER_SPRITE   = "Assets/Sprites/UI/Health_Bar_Border.png";
    private const string BOSS_PREFAB_DIR    = "Assets/Prefabs/Enemies/Boss";
    private const string BOSS_SCENE_PATH    = "Assets/Scenes/Scene_Boss.unity";

    // ── Arena geometry constants ──────────────────────────────────────────────
    private const float HALF_X       = 9f;   // inner half-width
    private const float HALF_Y       = 7f;   // inner half-height
    private const float WALL_T       = 0.5f; // wall half-thickness → wall size = 1
    private const float OPENING_HALF = 2.5f; // half height of left/right openings
    private const float ARENA_GAP    = 3f;   // space between arenas (filled by prog-gate)
    private const float OUTER_X      = HALF_X + WALL_T; // 9.5
    private const float OUTER_Y      = HALF_Y + WALL_T; // 7.5
    private const float ARENA_STEP   = (HALF_X + WALL_T) * 2f + ARENA_GAP; // ~22

    // ── Boss config  ──────────────────────────────────────────────────────────
    private static readonly string[] BossNames = { "Boss_SlimeKing", "Boss_GrapeShaman", "Boss_Chimera" };
    private static readonly Color[]  BossColors =
    {
        new Color(0.45f, 0.90f, 0.45f), // Slime King — green
        new Color(0.80f, 0.35f, 0.80f), // Grape Shaman — purple
        new Color(0.95f, 0.35f, 0.20f), // Chimera — fiery red
    };

    // ── Palette ────────────────────────────────────────────────────────────────
    private static readonly Color WALL_COLOR   = new Color(0.25f, 0.18f, 0.12f);
    private static readonly Color GATE_COLOR   = new Color(0.70f, 0.55f, 0.20f);
    private static readonly Color FLOOR_COLOR  = new Color(0.18f, 0.32f, 0.14f);

    // ═══════════════════════════════════════════════════════════════════════════
    //  MENU ITEMS
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Boss Setup/▶ Full Setup (Run Both Steps)", priority = 0)]
    public static void FullSetup()
    {
        CreateBossPrefabs();
        BuildBossScene();
    }

    [MenuItem("Tools/Boss Setup/① Create Boss Prefabs", priority = 11)]
    public static void CreateBossPrefabs()
    {
        EnsureDir(BOSS_PREFAB_DIR);

        var slimePfb   = Load<GameObject>(SLIME_PREFAB);
        var grapePfb   = Load<GameObject>(GRAPE_PREFAB);
        var bulletPfb  = Load<GameObject>(BULLET_PREFAB);
        var grapeProj  = Load<GameObject>(GRAPE_PROJ_PREFAB);
        var grapeShadow = Load<GameObject>(GRAPE_SHADOW);
        var grapeSplatter = Load<GameObject>(GRAPE_SPLATTER);
        var deathVFX   = Load<GameObject>(DEATH_VFX);
        var healthGlobe = Load<GameObject>(HEALTH_GLOBE);
        var goldCoin   = Load<GameObject>(GOLD_COIN);
        var staminaGlobe = Load<GameObject>(STAMINA_GLOBE);
        var gemPickup  = Load<GameObject>(GEM_PICKUP);
        var boomPickup = Load<GameObject>(BOOM_PICKUP);
        var flashMat   = Load<Material>(WHITE_FLASH_MAT);

        if (!slimePfb || !grapePfb || !bulletPfb)
        {
            Debug.LogError("[BossSetup] Core prefabs not found. Aborting prefab creation.");
            return;
        }

        // ── Helper prefabs ────────────────────────────────────────────────────

        var slamAoePfb = CreateGroundSlamPrefab(flashMat);
        var bossGrapeProj = CreateBossGrapeProjectilePrefab(grapeShadow, grapeSplatter);

        // ── Three boss prefabs ────────────────────────────────────────────────

        var slimeKingPfb = CreateSlimeKingPrefab(slimePfb, bulletPfb, slimePfb,
            slamAoePfb, deathVFX, healthGlobe, goldCoin, staminaGlobe, gemPickup, boomPickup, flashMat);

        var grapeShamanPfb = CreateGrapeShamanPrefab(grapePfb, bossGrapeProj, grapePfb,
            deathVFX, healthGlobe, goldCoin, staminaGlobe, gemPickup, boomPickup, flashMat);

        var chimeraPfb = CreateChimeraPrefab(slimePfb, bossGrapeProj, bulletPfb,
            deathVFX, healthGlobe, goldCoin, staminaGlobe, gemPickup, boomPickup, flashMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BossSetup] ✔ All boss prefabs created successfully:\n" +
                  $"  {BOSS_PREFAB_DIR}/Boss_SlimeKing.prefab\n" +
                  $"  {BOSS_PREFAB_DIR}/Boss_GrapeShaman.prefab\n" +
                  $"  {BOSS_PREFAB_DIR}/Boss_Chimera.prefab\n" +
                  $"  {BOSS_PREFAB_DIR}/Boss_GroundSlamAoE.prefab\n" +
                  $"  {BOSS_PREFAB_DIR}/Boss_GrapeProjectile.prefab");
    }

    [MenuItem("Tools/Boss Setup/② Build Boss Scene", priority = 12)]
    public static void BuildBossScene()
    {
        // Prompt user to save current work
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[BossSetup] Scene build cancelled by user.");
            return;
        }

        // Load prereqs
        var slimeKingPfb   = Load<GameObject>(BOSS_PREFAB_DIR + "/Boss_SlimeKing.prefab");
        var grapeShamanPfb = Load<GameObject>(BOSS_PREFAB_DIR + "/Boss_GrapeShaman.prefab");
        var chimeraPfb     = Load<GameObject>(BOSS_PREFAB_DIR + "/Boss_Chimera.prefab");
        var playerPfb      = Load<GameObject>(PLAYER_PREFAB);
        var font           = Load<TMP_FontAsset>(FONT_ASSET);
        var fillSprite     = Load<Sprite>(HP_FILL_SPRITE);
        var borderSprite   = Load<Sprite>(HP_BORDER_SPRITE);

        if (slimeKingPfb == null || grapeShamanPfb == null || chimeraPfb == null)
        {
            Debug.LogError("[BossSetup] Boss prefabs not found. Run '① Create Boss Prefabs' first.");
            return;
        }

        // ── Copy Scene_3 → Scene_Boss (reuse the forest map, lighting, tilemaps) ─
        const string SCENE_3_PATH = "Assets/Scenes/Scene_3.unity";
        string absSource = Path.GetFullPath(SCENE_3_PATH);
        string absDest   = Path.GetFullPath(BOSS_SCENE_PATH);

        if (!File.Exists(absSource))
        {
            Debug.LogError($"[BossSetup] Scene_3 not found at {SCENE_3_PATH}. Aborting.");
            return;
        }
        File.Copy(absSource, absDest, overwrite: true);
        AssetDatabase.Refresh();

        // Open the freshly copied scene
        var scene = EditorSceneManager.OpenScene(BOSS_SCENE_PATH, OpenSceneMode.Single);

        // ── Remove Scene_3 enemy groups — keep map/environment ────────────────
        string[] removeByName = { "Slimes", "Enemies", "Grapes" };
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (System.Array.IndexOf(removeByName, rootGo.name) >= 0)
                Object.DestroyImmediate(rootGo);
        }

        // Scene_3 already contains Cameras.prefab and Managers.prefab — no need to add them again.

        // ── Player spawn point ─────────────────────────────────────────────────
        // When coming from another scene the DontDestroyOnLoad player is used;
        // BossSequenceManager.Start() teleports them to this spawn point.
        // When playing boss scene directly, the Player prefab below is used.
        Vector3 playerStart = new Vector3(-HALF_X + 2f, 0f, 0f);
        var spawnPointGo = new GameObject("PlayerSpawnPoint");
        spawnPointGo.transform.position = playerStart;

        if (playerPfb != null)
        {
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPfb);
            playerGo.transform.position = playerStart;
        }

        // ── Build 3 arenas ────────────────────────────────────────────────────
        var arenaScripts = new BossArena[3];
        GameObject[] bossPrefabs = { slimeKingPfb, grapeShamanPfb, chimeraPfb };
        GameObject[] progressionGates = new GameObject[2];

        var arenaRoot = new GameObject("Arenas");

        for (int i = 0; i < 3; i++)
        {
            float cx = i * ARENA_STEP;
            var (arena, spawnPt, entryDoor, exitDoor) = BuildArena(arenaRoot.transform, cx, i, bossPrefabs[i]);
            arenaScripts[i] = arena;

            // Wire SpawnPoint and BossPrefab into BossArena
            SetRef(arena, "bossPrefab", bossPrefabs[i]);
            SetRef(arena, "bossSpawnPoint", spawnPt);

            // Build progression gate AFTER this arena (except last)
            if (i < 2)
            {
                float gateX = cx + OUTER_X + ARENA_GAP * 0.5f;
                var gate = CreateProgressionGate(arenaRoot.transform, gateX, i);
                progressionGates[i] = gate;

                // Wire entryDoor of THIS arena AND exitDoor as arenaDoors
                SetRefArray(arena, "arenaDoors", new GameObject[] { entryDoor, exitDoor });
            }
            else
            {
                // Last arena only needs entry door (no right progression gate)
                SetRefArray(arena, "arenaDoors", new GameObject[] { entryDoor, exitDoor });
            }
        }

        // ── BossSequenceManager ───────────────────────────────────────────────
        var seqGo = new GameObject("BossSequenceManager");
        var seq = seqGo.AddComponent<BossSequenceManager>();
        SetRefArray(seq, "bossArenas", arenaScripts);
        SetRefArray(seq, "progressionGates", progressionGates);        SetRef(seq, "playerSpawnPoint", spawnPointGo.transform);
        // ── UI ────────────────────────────────────────────────────────────────
        var (healthBarUI, bannerGroup, bannerText) = BuildBossUI(font, fillSprite, borderSprite);
        SetRef(seq, "bannerText", bannerText);
        SetRef(seq, "bannerCanvasGroup", bannerGroup);

        // ── Add scene to Build Settings ───────────────────────────────────────
        EditorSceneManager.SaveScene(scene, BOSS_SCENE_PATH);

        var buildScenes = EditorBuildSettings.scenes.ToList();
        bool alreadyAdded = buildScenes.Any(s => s.path == BOSS_SCENE_PATH);
        if (!alreadyAdded)
        {
            buildScenes.Add(new EditorBuildSettingsScene(BOSS_SCENE_PATH, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[BossSetup] Added {BOSS_SCENE_PATH} to Build Settings.");
        }

        EditorSceneManager.SaveScene(scene, BOSS_SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log("[BossSetup] ✔ Boss scene built successfully at: " + BOSS_SCENE_PATH);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PREFAB CREATION
    // ═══════════════════════════════════════════════════════════════════════════

    private static GameObject CreateSlimeKingPrefab(
        GameObject srcPfb, GameObject bulletPfb, GameObject miniSlimePfb,
        GameObject slamAoePfb, GameObject deathVFX,
        GameObject hGlobe, GameObject coin, GameObject stGlobe, GameObject gem, GameObject boom,
        Material flashMat)
    {
        var go = BuildBossBase("Boss_SlimeKing", srcPfb, flashMat, 3f);
        var pathfinding = go.GetComponent<EnemyPathfinding>();
        SetFloat(pathfinding, "moveSpeed", 0.7f);

        var bh = go.AddComponent<BossHealth>();
        SetInt(bh, "startingHealth", 150);
        SetFloat(bh, "knockbackThrust", 6f);
        SetRef(bh, "deathVFXPrefab", deathVFX);

        var ai = go.AddComponent<BossSlimeKingAI>();
        SetRef(ai, "bulletPrefab", bulletPfb);
        SetRef(ai, "miniSlimePrefab", miniSlimePfb);
        SetRef(ai, "slamAoePrefab", slamAoePfb);

        SetupPickupSpawner(go, hGlobe, coin, stGlobe, gem, boom);

        string path = $"{BOSS_PREFAB_DIR}/Boss_SlimeKing.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[BossSetup] Created Boss_SlimeKing.prefab");
        return prefab;
    }

    private static GameObject CreateGrapeShamanPrefab(
        GameObject srcPfb, GameObject grapeProj, GameObject grapeMinionPfb,
        GameObject deathVFX,
        GameObject hGlobe, GameObject coin, GameObject stGlobe, GameObject gem, GameObject boom,
        Material flashMat)
    {
        var go = BuildBossBase("Boss_GrapeShaman", srcPfb, flashMat, 2.5f);
        var pathfinding = go.GetComponent<EnemyPathfinding>();
        SetFloat(pathfinding, "moveSpeed", 1.2f);

        var bh = go.AddComponent<BossHealth>();
        SetInt(bh, "startingHealth", 100);
        SetFloat(bh, "knockbackThrust", 5f);
        SetRef(bh, "deathVFXPrefab", deathVFX);

        var ai = go.AddComponent<BossGrapeShamanAI>();
        SetRef(ai, "grapeProjectilePrefab", grapeProj);
        SetRef(ai, "grapeMinionPrefab", grapeMinionPfb);

        SetupPickupSpawner(go, hGlobe, coin, stGlobe, gem, boom);

        string path = $"{BOSS_PREFAB_DIR}/Boss_GrapeShaman.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[BossSetup] Created Boss_GrapeShaman.prefab");
        return prefab;
    }

    private static GameObject CreateChimeraPrefab(
        GameObject srcPfb, GameObject arcProj, GameObject bulletPfb,
        GameObject deathVFX,
        GameObject hGlobe, GameObject coin, GameObject stGlobe, GameObject gem, GameObject boom,
        Material flashMat)
    {
        var go = BuildBossBase("Boss_Chimera", srcPfb, flashMat, 2f);
        var pathfinding = go.GetComponent<EnemyPathfinding>();
        SetFloat(pathfinding, "moveSpeed", 2.5f);

        var bh = go.AddComponent<BossHealth>();
        SetInt(bh, "startingHealth", 200);
        SetFloat(bh, "knockbackThrust", 4f);
        SetRef(bh, "deathVFXPrefab", deathVFX);

        var ai = go.AddComponent<BossChimeraAI>();
        SetRef(ai, "p1ProjectilePrefab", arcProj);
        SetRef(ai, "p2BulletPrefab", bulletPfb);

        SetupPickupSpawner(go, hGlobe, coin, stGlobe, gem, boom);

        string path = $"{BOSS_PREFAB_DIR}/Boss_Chimera.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[BossSetup] Created Boss_Chimera.prefab");
        return prefab;
    }

    private static GameObject CreateGroundSlamPrefab(Material flashMat)
    {
        var go = new GameObject("Boss_GroundSlamAoE");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(32, new Color(1f, 0.6f, 0.1f, 0.7f));
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<BossGroundSlamAoE>();

        string path = $"{BOSS_PREFAB_DIR}/Boss_GroundSlamAoE.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[BossSetup] Created Boss_GroundSlamAoE.prefab");
        return prefab;
    }

    private static GameObject CreateBossGrapeProjectilePrefab(GameObject shadow, GameObject splatter)
    {
        var srcGrapeProj = Load<GameObject>(GRAPE_PROJ_PREFAB);

        var go = new GameObject("Boss_GrapeProjectile");
        var sr = go.AddComponent<SpriteRenderer>();

        // Copy sprite from original Grape Projectile
        if (srcGrapeProj != null)
        {
            var srcSr = srcGrapeProj.GetComponent<SpriteRenderer>();
            if (srcSr) { sr.sprite = srcSr.sprite; sr.sortingOrder = srcSr.sortingOrder + 1; }
        }
        sr.color = new Color(0.85f, 0.5f, 1f); // purple tint to differentiate

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;

        var proj = go.AddComponent<BossGrapeProjectile>();
        SetRef(proj, "grapeProjectileShadow", shadow);
        SetRef(proj, "splatterPrefab", splatter);
        // animCurve will default to linear; user can adjust in Inspector

        string path = $"{BOSS_PREFAB_DIR}/Boss_GrapeProjectile.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[BossSetup] Created Boss_GrapeProjectile.prefab");
        return prefab;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SCENE BUILDING
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Builds one arena room and returns the BossArena component + key objects.</summary>
    private static (BossArena arena, Transform spawnPt, GameObject entryDoor, GameObject exitDoor)
        BuildArena(Transform parent, float centerX, int index, GameObject bossPrefab)
    {
        var arenaRoot = new GameObject($"Arena_{index + 1}_{BossNames[index]}");
        arenaRoot.transform.SetParent(parent, false);
        arenaRoot.transform.localPosition = new Vector3(centerX, 0, 0);

        Color wallColor = Color.Lerp(WALL_COLOR, BossColors[index], 0.25f);

        // ── Permanent walls ───────────────────────────────────────────────────
        var wallsRoot = new GameObject("Walls");
        wallsRoot.transform.SetParent(arenaRoot.transform, false);

        // Top & bottom (full width including door areas)
        float topBotWidth = (OUTER_X + WALL_T) * 2f;
        CreateWall(wallsRoot.transform, new Vector3(0,  OUTER_Y, 0), new Vector2(topBotWidth, WALL_T * 2), wallColor, "Wall_Top");
        CreateWall(wallsRoot.transform, new Vector3(0, -OUTER_Y, 0), new Vector2(topBotWidth, WALL_T * 2), wallColor, "Wall_Bottom");

        // Left side: upper & lower halves (gap in centre for opening)
        float wallSegH = OUTER_Y - OPENING_HALF;
        float wallSegCY = OPENING_HALF + wallSegH * 0.5f;
        CreateWall(wallsRoot.transform, new Vector3(-OUTER_X,  wallSegCY, 0), new Vector2(WALL_T * 2, wallSegH), wallColor, "Wall_Left_Upper");
        CreateWall(wallsRoot.transform, new Vector3(-OUTER_X, -wallSegCY, 0), new Vector2(WALL_T * 2, wallSegH), wallColor, "Wall_Left_Lower");
        CreateWall(wallsRoot.transform, new Vector3( OUTER_X,  wallSegCY, 0), new Vector2(WALL_T * 2, wallSegH), wallColor, "Wall_Right_Upper");
        CreateWall(wallsRoot.transform, new Vector3( OUTER_X, -wallSegCY, 0), new Vector2(WALL_T * 2, wallSegH), wallColor, "Wall_Right_Lower");

        // ── Arena doors (activated during boss fight) ─────────────────────────
        var doorsRoot = new GameObject("ArenaDoors");
        doorsRoot.transform.SetParent(arenaRoot.transform, false);

        var entryDoor = CreateWall(doorsRoot.transform, new Vector3(-OUTER_X, 0, 0),
            new Vector2(WALL_T * 2, OPENING_HALF * 2), new Color(0.8f, 0.2f, 0.2f), "Door_Entry");
        entryDoor.SetActive(false); // starts inactive — activates when boss spawns

        var exitDoor = CreateWall(doorsRoot.transform, new Vector3(OUTER_X, 0, 0),
            new Vector2(WALL_T * 2, OPENING_HALF * 2), new Color(0.8f, 0.2f, 0.2f), "Door_Exit");
        exitDoor.SetActive(false);

        // ── Boss spawn point ──────────────────────────────────────────────────
        var spawnGo = new GameObject("BossSpawnPoint");
        spawnGo.transform.SetParent(arenaRoot.transform, false);
        spawnGo.transform.localPosition = Vector3.zero;

        // ── BossArena trigger ─────────────────────────────────────────────────
        var arenaComp = arenaRoot.AddComponent<BossArena>();
        var trigCol  = arenaRoot.AddComponent<BoxCollider2D>();
        trigCol.isTrigger = true;
        trigCol.size = new Vector2(HALF_X * 2f - 1f, HALF_Y * 2f - 1f);

        // ── Boss label (visual helper) ────────────────────────────────────────
        CreateArenaLabel(arenaRoot.transform, BossNames[index], BossColors[index]);

        return (arenaComp, spawnGo.transform, entryDoor, exitDoor);
    }

    /// <summary>Creates a progression gate (blocking strip) between two arenas.</summary>
    private static GameObject CreateProgressionGate(Transform parent, float x, int index)
    {
        var gateGo = new GameObject($"ProgressionGate_{index + 1}");
        gateGo.transform.SetParent(parent, false);
        gateGo.transform.localPosition = new Vector3(x, 0, 0);

        var sr = gateGo.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite(GATE_COLOR);
        sr.size   = new Vector2(ARENA_GAP, OUTER_Y * 2f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.sortingOrder = 3;

        var col = gateGo.AddComponent<BoxCollider2D>();
        col.size = new Vector2(ARENA_GAP, OUTER_Y * 2f);

        // Starts ACTIVE (blocking). Disabled by BossSequenceManager when previous boss dies.
        gateGo.SetActive(true);

        return gateGo;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UI BUILDING
    // ═══════════════════════════════════════════════════════════════════════════

    private static (BossHealthBarUI ui, CanvasGroup bannerGroup, TextMeshProUGUI bannerText)
        BuildBossUI(TMP_FontAsset font, Sprite fillSprite, Sprite borderSprite)
    {
        // ── Root Canvas ───────────────────────────────────────────────────────
        var canvasGo = new GameObject("BossUI_Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Boss Health Bar Panel (top of screen) ─────────────────────────────
        var hpPanelGo = CreateRectChild(canvasGo.transform, "BossHealthBar_Panel");
        var hpRect = hpPanelGo.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0, 1);
        hpRect.anchorMax = new Vector2(1, 1);
        hpRect.pivot     = new Vector2(0.5f, 1f);
        hpRect.offsetMin = new Vector2(20, 0);
        hpRect.offsetMax = new Vector2(-20, 0);
        hpRect.sizeDelta = new Vector2(0, 90);

        var hpBg = hpPanelGo.AddComponent<Image>();
        hpBg.color = new Color(0f, 0f, 0f, 0.75f);
        if (borderSprite != null) hpBg.sprite = borderSprite;
        hpBg.type = Image.Type.Sliced;

        // CanvasGroup for fade-in/out
        var hpCg = hpPanelGo.AddComponent<CanvasGroup>();
        hpCg.alpha = 0f;

        // Boss Name TMP
        var bossNameGo = CreateRectChild(hpPanelGo.transform, "BossNameText");
        var bossNameRect = bossNameGo.GetComponent<RectTransform>();
        bossNameRect.anchorMin = new Vector2(0, 0.55f);
        bossNameRect.anchorMax = new Vector2(0.7f, 1f);
        bossNameRect.offsetMin = new Vector2(10, 0);
        bossNameRect.offsetMax = Vector2.zero;
        var bossNameTMP = bossNameGo.AddComponent<TextMeshProUGUI>();
        bossNameTMP.text = "Boss Name";
        bossNameTMP.fontSize = 28;
        bossNameTMP.fontStyle = FontStyles.Bold;
        bossNameTMP.color = Color.white;
        bossNameTMP.alignment = TextAlignmentOptions.BottomLeft;
        if (font) bossNameTMP.font = font;

        // Phase TMP
        var phaseGo = CreateRectChild(hpPanelGo.transform, "PhaseText");
        var phaseRect = phaseGo.GetComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(0.7f, 0.55f);
        phaseRect.anchorMax = new Vector2(1f, 1f);
        phaseRect.offsetMin = Vector2.zero;
        phaseRect.offsetMax = new Vector2(-10, 0);
        var phaseTMP = phaseGo.AddComponent<TextMeshProUGUI>();
        phaseTMP.text = "Phase 1";
        phaseTMP.fontSize = 22;
        phaseTMP.color = new Color(1f, 0.85f, 0.3f);
        phaseTMP.alignment = TextAlignmentOptions.BottomRight;
        if (font) phaseTMP.font = font;

        // HP Slider
        var sliderGo   = CreateRectChild(hpPanelGo.transform, "HP_Slider");
        var sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0);
        sliderRect.anchorMax = new Vector2(1, 0.5f);
        sliderRect.offsetMin = new Vector2(10, 8);
        sliderRect.offsetMax = new Vector2(-10, 0);

        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue     = 0;
        slider.maxValue     = 1;
        slider.value        = 1;
        slider.interactable = false;

        var fillAreaGo   = CreateRectChild(sliderGo.transform, "Fill Area");
        var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fillGo   = CreateRectChild(fillAreaGo.transform, "Fill");
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImg  = fillGo.AddComponent<Image>();
        fillImg.color    = new Color(0.9f, 0.25f, 0.25f);
        fillImg.type     = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        if (fillSprite != null) fillImg.sprite = fillSprite;
        slider.fillRect = fillRect;

        // Background slice for slider
        var bgGo = CreateRectChild(sliderGo.transform, "Background");
        bgGo.transform.SetAsFirstSibling();
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.05f, 0.05f);

        // BossHealthBarUI component
        var healthBarUI = hpPanelGo.AddComponent<BossHealthBarUI>();
        SetRef(healthBarUI, "healthSlider", slider);
        SetRef(healthBarUI, "bossNameText", bossNameTMP);
        SetRef(healthBarUI, "phaseText", phaseTMP);
        SetRef(healthBarUI, "canvasGroup", hpCg);

        hpPanelGo.SetActive(false); // hidden until boss fight starts

        // ── Boss Banner Panel (centre screen) ─────────────────────────────────
        var bannerGo = CreateRectChild(canvasGo.transform, "BossBanner_Panel");
        var bannerRect = bannerGo.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.2f, 0.35f);
        bannerRect.anchorMax = new Vector2(0.8f, 0.65f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;

        var bannerCg = bannerGo.AddComponent<CanvasGroup>();
        bannerCg.alpha = 0f;

        var bannerBg = bannerGo.AddComponent<Image>();
        bannerBg.color = new Color(0f, 0f, 0f, 0.82f);

        var bannerTextGo   = CreateRectChild(bannerGo.transform, "BannerText");
        var bannerTextRect = bannerTextGo.GetComponent<RectTransform>();
        bannerTextRect.anchorMin = Vector2.zero;
        bannerTextRect.anchorMax = Vector2.one;
        bannerTextRect.offsetMin = new Vector2(15, 10);
        bannerTextRect.offsetMax = new Vector2(-15, -10);
        var bannerTMP = bannerTextGo.AddComponent<TextMeshProUGUI>();
        bannerTMP.text      = "BOSS";
        bannerTMP.fontSize  = 40;
        bannerTMP.fontStyle = FontStyles.Bold;
        bannerTMP.color     = Color.white;
        bannerTMP.alignment = TextAlignmentOptions.Center;
        bannerTMP.enableWordWrapping = false;
        bannerTMP.enableAutoSizing   = false;
        if (font) bannerTMP.font = font;

        return (healthBarUI, bannerCg, bannerTMP);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HELPERS — GameObject / Component creation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Clones the sprite+collider shape from source prefab and adds shared boss components.</summary>
    private static GameObject BuildBossBase(string name, GameObject srcPfb, Material flashMat, float scale)
    {
        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;

        // ── Sprite ────────────────────────────────────────────────────────────
        var sr = go.AddComponent<SpriteRenderer>();
        if (srcPfb != null)
        {
            var srcSr = srcPfb.GetComponent<SpriteRenderer>();
            if (srcSr) { sr.sprite = srcSr.sprite; sr.sortingLayerName = srcSr.sortingLayerName; sr.sortingOrder = srcSr.sortingOrder; }
        }

        // ── Physics ───────────────────────────────────────────────────────────
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Copy collider from source prefab
        if (srcPfb != null)
        {
            var srcCap = srcPfb.GetComponent<CapsuleCollider2D>();
            if (srcCap != null)
            {
                var cap = go.AddComponent<CapsuleCollider2D>();
                cap.size      = srcCap.size;
                cap.offset    = srcCap.offset;
                cap.direction = srcCap.direction;
            }
            else
            {
                var srcCirc = srcPfb.GetComponent<CircleCollider2D>();
                var circ = go.AddComponent<CircleCollider2D>();
                circ.radius = srcCirc != null ? srcCirc.radius : 0.25f;
            }
        }

        // ── Flash ─────────────────────────────────────────────────────────────
        var flash = go.AddComponent<Flash>();
        if (flashMat != null)
        {
            var flashSo = new SerializedObject(flash);
            flashSo.FindProperty("whiteFlashMat").objectReferenceValue = flashMat;
            flashSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Knockback ─────────────────────────────────────────────────────────
        go.AddComponent<Knockback>();

        // ── Pathfinding ───────────────────────────────────────────────────────
        go.AddComponent<EnemyPathfinding>();

        return go;
    }

    private static void SetupPickupSpawner(GameObject go,
        GameObject hGlobe, GameObject coin, GameObject stGlobe, GameObject gem, GameObject boom)
    {
        var ps = go.GetComponent<PickupSpawner>() ?? go.AddComponent<PickupSpawner>();
        var so = new SerializedObject(ps);
        so.FindProperty("healthGlobe").objectReferenceValue  = hGlobe;
        so.FindProperty("goldCoin").objectReferenceValue     = coin;
        so.FindProperty("staminaGlobe").objectReferenceValue = stGlobe;
        so.FindProperty("gemPickup").objectReferenceValue    = gem;
        so.FindProperty("boomPickup").objectReferenceValue   = boom;
        so.FindProperty("gemDropChance").intValue            = 60;
        so.FindProperty("boomDropChance").intValue           = 30;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Creates a wall segment with SpriteRenderer + BoxCollider2D.</summary>
    private static GameObject CreateWall(Transform parent, Vector3 localPos, Vector2 size, Color color, string wallName)
    {
        var go = new GameObject(wallName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite    = CreateSolidSprite(color);
        sr.size      = size;
        sr.drawMode  = SpriteDrawMode.Sliced;
        sr.color     = color;
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;

        return go;
    }

    private static void CreateColoredQuad(Transform parent, Vector3 localPos, Vector2 size, Color color, string quadName)
    {
        var go = new GameObject(quadName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite   = CreateSolidSprite(color);
        sr.size     = size;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.color    = color;
        sr.sortingOrder = 0;
    }

    private static void CreateArenaLabel(Transform parent, string label, Color color)
    {
        var go = new GameObject("_ArenaLabel_EditorOnly");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, HALF_Y - 1f, 0);
        // This GameObject has no runtime component - purely a visual label in Editor hierarchy
        // Developers can delete it or wire a world-space TMP if desired
    }

    private static GameObject CreateRectChild(Transform parent, string childName)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HELPERS — Serialized Object wiring
    // ═══════════════════════════════════════════════════════════════════════════

    private static void SetRef(Object target, string propName, Object value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"[BossSetup] Property '{propName}' not found on {target.name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefArray<T>(Object target, string propName, T[] values) where T : Object
    {
        if (target == null) return;
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"[BossSetup] Array property '{propName}' not found on {target.name}"); return; }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string propName, int value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop != null) { prop.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    private static void SetFloat(Object target, string propName, float value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop != null) { prop.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HELPERS — Sprites & Assets
    // ═══════════════════════════════════════════════════════════════════════════

    private static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogWarning($"[BossSetup] Could not load asset at: {path}");
        return asset;
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    /// <summary>Creates a 4×4 solid-colour sprite (not saved as asset, ephemeral).</summary>
    private static Sprite CreateSolidSprite(Color color)
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 16f);
    }

    /// <summary>Creates a filled circle sprite (not saved as asset).</summary>
    private static Sprite CreateCircleSprite(int resolution, Color color)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float r = resolution * 0.5f;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            tex.SetPixel(x, y, (dx * dx + dy * dy) <= r * r ? color : Color.clear);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution * 0.5f);
    }
}
