using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads a map from a JSON file in Resources/Maps/ and spawns prefabs into the scene.
/// Keeps the player and persistent singletons intact between map transitions.
/// Attach this MonoBehaviour to a persistent GameObject (e.g., GameManager).
/// </summary>
public class MapLoader : Singleton<MapLoader>
{
  [Header("Registry")]
  [Tooltip("ScriptableObject that maps integer indices to prefabs.")]
  [SerializeField] private PrefabRegistry registry;

  [Header("Scene Roots")]
  [Tooltip("Parent transform for all spawned ground tiles.")]
  [SerializeField] private Transform groundRoot;

  [Tooltip("Parent transform for all spawned object/decoration prefabs.")]
  [SerializeField] private Transform objectRoot;

  [Tooltip("Parent transform for all spawned enemy prefabs.")]
  [SerializeField] private Transform enemyRoot;

  /// <summary>The currently loaded map data.</summary>
  public MapData CurrentMap { get; private set; }

  /// <summary>
  /// Loads a map from Resources/Maps/{mapId}.json and spawns its layers.
  /// Repositions the player at the named entrance.
  /// </summary>
  /// <param name="mapId">Filename without extension inside Resources/Maps/.</param>
  /// <param name="entranceName">Name of the entrance where the player should spawn.</param>
  public void LoadMap(string mapId, string entranceName)
  {
    StartCoroutine(LoadMapRoutine(mapId, entranceName));
  }

  private void EnsureRoots()
  {
    if (groundRoot == null)
    {
      GameObject go = GameObject.Find("GroundRoot");
      if (go == null) go = new GameObject("GroundRoot");
      groundRoot = go.transform;
    }
    if (objectRoot == null)
    {
      GameObject go = GameObject.Find("ObjectRoot");
      if (go == null) go = new GameObject("ObjectRoot");
      objectRoot = go.transform;
    }
    if (enemyRoot == null)
    {
      GameObject go = GameObject.Find("EnemyRoot");
      if (go == null) go = new GameObject("EnemyRoot");
      enemyRoot = go.transform;
    }
  }

  private IEnumerator LoadMapRoutine(string mapId, string entranceName)
  {
    UIFade.Instance?.FadeToBlack();
    yield return new WaitForSeconds(0.5f);

    EnsureRoots();

    MapData data = ReadMapJson(mapId);
    if (data == null)
    {
      Debug.LogError($"[MapLoader] Cannot find map: {mapId}");
      yield break;
    }

    int groundTiles = data.layers?.ground?.Count ?? 0;
    int objectTiles = data.layers?.objects?.Count ?? 0;
    int enemyTiles = data.layers?.enemies?.Count ?? 0;
    Debug.Log($"[MapLoader] Loaded map \"{mapId}\". Ground: {groundTiles}, Objects: {objectTiles}, Enemies: {enemyTiles}");

    CurrentMap = data;

    ClearLayer(groundRoot);
    ClearLayer(objectRoot);
    ClearLayer(enemyRoot);

    SpawnLayer(data.layers, LayerType.Ground, data.tileSize, groundRoot, out int spawnedGround);
    SpawnLayer(data.layers, LayerType.Objects, data.tileSize, objectRoot, out int spawnedObjects);
    SpawnLayer(data.layers, LayerType.Enemies, data.tileSize, enemyRoot, out int spawnedEnemies);

    Debug.Log($"[MapLoader] ACTUALLY SPAWNED -> Ground: {spawnedGround}, Objects: {spawnedObjects}, Enemies: {spawnedEnemies} | Rows: {data.layers.rows}, Cols: {data.layers.cols}");

    PlacePlayerAtEntrance(data, entranceName);

    CameraController.Instance?.SetPlayerCameraFollow();
    UIFade.Instance?.FadeToClear();
  }

  // ─── Private Helpers ────────────────────────────────────────────────────

  private MapData ReadMapJson(string mapId)
  {
    TextAsset jsonAsset = Resources.Load<TextAsset>($"Maps/{mapId}");
    if (jsonAsset == null) return null;

    try
    {
      return JsonUtility.FromJson<MapData>(jsonAsset.text);
    }
    catch (System.Exception ex)
    {
      Debug.LogError($"[MapLoader] JSON parse error for map \"{mapId}\": {ex.Message}");
      return null;
    }
  }

  private void ClearLayer(Transform root)
  {
    if (root == null) return;

    for (int i = root.childCount - 1; i >= 0; i--)
    {
      Destroy(root.GetChild(i).gameObject);
    }
  }

  private enum LayerType { Ground, Objects, Enemies }

  private void SpawnLayer(MapLayers layers, LayerType type, float tileSize, Transform root, out int spawnedCount)
  {
    spawnedCount = 0;
    if (root == null || layers == null) return;

    for (int row = 0; row < layers.rows; row++)
    {
      for (int col = 0; col < layers.cols; col++)
      {
        int index = GetIndex(layers, type, row, col);
        if (index <= 0) continue;

        GameObject prefab = GetPrefab(type, index);
        if (prefab == null)
        {
          Debug.LogWarning($"[MapLoader] Prefab for layer {type} at index {index} is missing in registry!");
          continue;
        }

        Vector3 pos = new Vector3(col * tileSize, -row * tileSize, 0f);
        Instantiate(prefab, pos, Quaternion.identity, root);
        spawnedCount++;
      }
    }
  }

  private int GetIndex(MapLayers layers, LayerType type, int row, int col)
  {
    switch (type)
    {
      case LayerType.Ground:  return layers.GetGround(row, col);
      case LayerType.Objects: return layers.GetObject(row, col);
      case LayerType.Enemies: return layers.GetEnemy(row, col);
      default: return 0;
    }
  }

  private GameObject GetPrefab(LayerType type, int index)
  {
    switch (type)
    {
      case LayerType.Ground:  return registry.GetGroundPrefab(index);
      case LayerType.Objects: return registry.GetObjectPrefab(index);
      case LayerType.Enemies: return registry.GetEnemyPrefab(index);
      default: return null;
    }
  }

  private void PlacePlayerAtEntrance(MapData data, string entranceName)
  {
    if (PlayerController.Instance == null) return;

    EntranceData entrance = data.entrances.Find(e => e.name == entranceName);
    if (entrance == null)
    {
      Debug.LogWarning($"[MapLoader] Entrance \"{entranceName}\" not found in map \"{data.id}\". Using map origin.");
      PlayerController.Instance.transform.position = Vector3.zero;
      return;
    }

    float ts = data.tileSize;
    Vector3 spawnPos = new Vector3(entrance.col * ts, -entrance.row * ts, 0f);
    PlayerController.Instance.transform.position = spawnPos;
  }
}
