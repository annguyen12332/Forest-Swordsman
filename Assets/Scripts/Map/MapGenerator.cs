using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TileMapping
{
    public int id;
    
    [Header("Tile Settings (Optional)")]
    public TileBase tile;
    public Tilemap targetTilemap;

    [Header("Prefab Settings (Optional)")]
    public GameObject prefab;
    public Transform parentTransform;
}

public class MapGenerator : MonoBehaviour
{
    public List<TileMapping> tileMappings;

    [Header("Generation Settings")]
    public float cellSize = 1f;

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (MapProgressionManager.Instance == null) return;

        // 1. Đã có map lưu cho scene này → load lại
        int[,] savedMap = MapProgressionManager.Instance.GetSavedMap(sceneName);
        if (savedMap != null)
        {
            GenerateFromMatrix(savedMap);
            Debug.Log("[MapGenerator] Loaded saved map for: " + sceneName);
            return;
        }

        // 2. Có pending map (được gen lúc level up) → dùng nó
        if (MapProgressionManager.Instance.HasPendingMap())
        {
            int[,] mapData = MapProgressionManager.Instance.ConsumePendingMap();
            if (mapData != null)
            {
                GenerateFromMatrix(mapData);
                MapProgressionManager.Instance.SaveMapData(sceneName, mapData);
                Debug.Log("[MapGenerator] Applied and SAVED pending map to: " + sceneName);
            }
            return;
        }

        // 3. Không có pending map, nhưng level đang đủ điều kiện procedural
        //    → Sinh map mới RIÊNG cho scene này (tránh bị "cướp" bởi scene khác)
        if (MapProgressionManager.Instance.ShouldApplyProceduralMap())
        {
            int[,] freshMap = MapProgressionManager.Instance.GenerateFreshMap();
            if (freshMap != null)
            {
                GenerateFromMatrix(freshMap);
                MapProgressionManager.Instance.SaveMapData(sceneName, freshMap);
                Debug.Log("[MapGenerator] Generated & SAVED fresh map for: " + sceneName);
            }
        }
    }

[ContextMenu("XÓA Lưu Map Cũ Của Scene Này")]
    public void ClearSavedMapForThisScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            int removed = SaveManager.Instance.Data.savedMaps.RemoveAll(m => m.sceneName == sceneName);
            SaveManager.Instance.SaveGame();
            
            if (removed > 0)
                Debug.Log($"[MapGenerator] Đã XÓA THÀNH CÔNG toàn bộ dữ liệu map lưu của scene: {sceneName}");
            else
                Debug.Log($"[MapGenerator] Scene: {sceneName} chưa có map nào được lưu cả.");
        }
    }
    public void GenerateMap(TextAsset mapFile)
    {
        if (mapFile == null) return;

        ClearMap();

        string[] rows = mapFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        int height = rows.Length;
        
        for (int y = 0; y < height; y++)
        {
            string[] cols = rows[y].Trim().Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            int width = cols.Length;

            for (int x = 0; x < width; x++)
            {
                if (int.TryParse(cols[x], out int id))
                {
                    TileMapping mapping = tileMappings.Find(m => m.id == id);
                    if (mapping != null)
                    {
                        // Toạ độ Tilemap (x, -y) do game 2D top-down thường có toạ độ ngược trục y khi nhìn text matrix
                        Vector3Int cellPos = new Vector3Int(x, -y, 0);

                        // Sinh Tile
                        if (mapping.tile != null && mapping.targetTilemap != null)
                        {
                            mapping.targetTilemap.SetTile(cellPos, mapping.tile);
                        }

                        // Sinh Prefab
                        if (mapping.prefab != null && mapping.parentTransform != null)
                        {
                            Vector3 spawnPos = new Vector3(x * cellSize + cellSize / 2f, -y * cellSize + cellSize / 2f, 0);
                            
                            if (mapping.targetTilemap != null) 
                            {
                                // Nếu có Tilemap đi kèm (hoặc bạn dùng 1 Grid cell size cố định), ưu tiên lấy center world của ô Tile
                                spawnPos = mapping.targetTilemap.GetCellCenterWorld(cellPos);
                            }
                            
#if UNITY_EDITOR
                            if (!Application.isPlaying)
                            {
                                GameObject obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(mapping.prefab, mapping.parentTransform);
                                if (obj != null) obj.transform.position = spawnPos;
                            }
                            else
#endif
                            {
                                Instantiate(mapping.prefab, spawnPos, Quaternion.identity, mapping.parentTransform);
                            }
                        }
                    }
                }
            }
        }
        
        Debug.Log("Map generated based on file: " + mapFile.name);
    }

    /// <summary>
    /// Generate map directly from a 2D int array (used by MapPainterWindow).
    /// </summary>
    public void GenerateFromMatrix(int[,] matrix)
    {
        if (matrix == null) return;

        ClearMap();

        int height = matrix.GetLength(0);
        int width = matrix.GetLength(1);

        int offsetX = width / 2;
        int offsetY = height / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int id = matrix[y, x];
                if (id <= 0) continue; // 0 = empty

                TileMapping mapping = tileMappings.Find(m => m.id == id);
                if (mapping == null) continue;

                Vector3Int cellPos = new Vector3Int(x - offsetX, -(y - offsetY), 0);

                if (mapping.tile != null && mapping.targetTilemap != null)
                {
                    mapping.targetTilemap.SetTile(cellPos, mapping.tile);
                }

                if (mapping.prefab != null && mapping.parentTransform != null)
                {
                    // Tự động lót 1 viên gạch Grass ở dưới chân Quái/Cây/Đá để tránh bị lủng lỗ bản đồ
                    TileMapping grassMapping = tileMappings.Find(m => m.id == MapProgressionManager.Instance.grassId);
                    if (grassMapping != null && grassMapping.targetTilemap != null)
                    {
                        grassMapping.targetTilemap.SetTile(cellPos, grassMapping.tile);
                    }

                    // Ưu tiên dùng hàm GetCellCenterWorld của Tilemap để canh giữa ô chính xác 100%
                    Tilemap referenceMap = mapping.targetTilemap;
                    if (referenceMap == null) 
                    {
                        // Nếu prefab không có target tilemap (quái/cây), mượn tạm tilemap của nền Grass để tính toạ độ
                        referenceMap = grassMapping != null ? grassMapping.targetTilemap : null;
                    }

                    Vector3 spawnPos = new Vector3((x - offsetX) * cellSize + cellSize / 2f, -(y - offsetY) * cellSize + cellSize / 2f, 0);
                    if (referenceMap != null)
                    {
                        spawnPos = referenceMap.GetCellCenterWorld(cellPos);
                    }

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        GameObject obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(mapping.prefab, mapping.parentTransform);
                        if (obj != null) obj.transform.position = spawnPos;
                    }
                    else
#endif
                    {
                        Instantiate(mapping.prefab, spawnPos, Quaternion.identity, mapping.parentTransform);
                    }
                }
            }
        }

        Debug.Log("Map generated from matrix (" + width + "x" + height + ")");
    }

    /// <summary>
    /// Export a matrix to a TextAsset-compatible string.
    /// </summary>
    public static string MatrixToString(int[,] matrix)
    {
        int h = matrix.GetLength(0);
        int w = matrix.GetLength(1);
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x > 0) sb.Append(' ');
                sb.Append(matrix[y, x]);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        if (tileMappings == null || tileMappings.Count == 0) return;

        List<Tilemap> clearedTilemaps = new List<Tilemap>();
        List<Transform> clearedParents = new List<Transform>();

        foreach (var mapping in tileMappings)
        {
            if (mapping.targetTilemap != null && !clearedTilemaps.Contains(mapping.targetTilemap))
            {
                mapping.targetTilemap.ClearAllTiles();
                clearedTilemaps.Add(mapping.targetTilemap);
            }

            if (mapping.parentTransform != null && !clearedParents.Contains(mapping.parentTransform))
            {
                // Dùng vòng lặp ngược để xóa an toàn
                for (int i = mapping.parentTransform.childCount - 1; i >= 0; i--)
                {
                    GameObject child = mapping.parentTransform.GetChild(i).gameObject;
#if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child);
                    }
#else
                    Destroy(child);
#endif
                }
                clearedParents.Add(mapping.parentTransform);
            }
        }
    }
}
