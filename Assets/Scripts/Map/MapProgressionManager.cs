using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton tồn tại qua các scene.
/// Khi player lên level chia hết cho 3 → tự động sinh ngẫu nhiên map mới (procedural).
/// Map data được lưu pending, khi chuyển scene sẽ được MapGenerator áp dụng.
/// </summary>
public class MapProgressionManager : Singleton<MapProgressionManager>
{
    [Header("Settings")]
    public int levelMilestone = 3;

    [Header("Map Size")]
    public int mapWidth = 50;
    public int mapHeight = 35;

    [Header("Tile IDs")]
    [Tooltip("ID cho Grass (nền chính)")]
    public int grassId = 1;
    [Tooltip("ID cho Water")]
    public int waterId = 2;
    [Tooltip("ID cho Tree")]
    public int treeId = 3;
    [Tooltip("Danh sách các ID của Enemy. Mỗi khi spawn 1 con quái, hệ thống sẽ bốc ngẫu nhiên 1 loại trong list này.")]
    public List<int> enemyIds = new List<int> { 4 };
    [Tooltip("ID cho Rock")]
    public int rockId = 5;
    [Header("Generation Rates")]
    [Tooltip("Khoảng tỉ lệ Nước (%)")]
    public Vector2Int waterPercentRange = new Vector2Int(5, 20);
    [Tooltip("Khoảng tỉ lệ Cây (%)")]
    public Vector2Int treePercentRange = new Vector2Int(10, 25);
    [Tooltip("Khoảng tỉ lệ Đá (%)")]
    public Vector2Int rockPercentRange = new Vector2Int(2, 10);
    
    [Tooltip("SỐ LƯỢNG Quái tối thiểu và tối đa mỗi lần gen (mỗi lần ngẫu nhiên khác nhau)")]
    public Vector2Int enemyCountRange = new Vector2Int(8, 15);
    [Tooltip("Khoảng cách TỐI THIỂU giữa các con quái (để phân bố đều)")]
    public int minEnemyDistance = 4;

    [Header("Water Cluster")]
    [Tooltip("Kích thước tối đa 1 cụm nước")]
    [Range(1, 10)] public int waterClusterSize = 4;

    private int[,] pendingMapData;
    private bool hasPending = false;

    private void OnEnable()
    {
        PlayerLevel.OnLevelUpEvent += HandleLevelUp;
    }

    private void OnDisable()
    {
        PlayerLevel.OnLevelUpEvent -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (newLevel > 1 && newLevel % levelMilestone == 0)
        {
            // XÓA tất cả saved maps → buộc mọi scene phải gen map MỚI khi player vào lần tới
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                SaveManager.Instance.Data.savedMaps.Clear();
                SaveManager.Instance.SaveGame();
                Debug.Log($"[MapProgression] Milestone level {newLevel}: Đã xóa tất cả saved maps. Mọi scene sẽ tự gen map mới khi được visit.");
            }

            // Gen sẵn 1 pending map cho scene đầu tiên player vào (dùng pending path cho nhanh)
            GeneratePendingMap();
        }
    }

    [ContextMenu("Generate Pending Map (Test)")]
    public void GeneratePendingMap()
    {
        pendingMapData = GenerateProceduralMap();
        hasPending = true;
        Debug.Log("[MapProgression] Procedural map generated (" + mapWidth + "x" + mapHeight + "), waiting for next area.");
    }

    /// <summary>
    /// Sinh 1 map hoàn toàn mới ngay lập tức và trả về (không dùng pending state).
    /// Dùng cho các scene cần tự generate map độc lập.
    /// </summary>
    public int[,] GenerateFreshMap()
    {
        int[,] map = GenerateProceduralMap();
        Debug.Log("[MapProgression] GenerateFreshMap() called — map mới được sinh trực tiếp.");
        return map;
    }

    /// <summary>
    /// Trả về true nếu player đã đạt ít nhất 1 milestone → scene nên gen map procedural.
    /// </summary>
    public bool ShouldApplyProceduralMap()
    {
        if (PlayerLevel.Instance == null) return false;
        int lvl = PlayerLevel.Instance.CurrentLevel;
        // Đủ điều kiện khi đã qua milestone ít nhất 1 lần (level >= levelMilestone)
        return lvl >= levelMilestone;
    }

    public bool HasPendingMap()
    {
        return hasPending;
    }

    public int[,] ConsumePendingMap()
    {
        if (!hasPending) return null;
        hasPending = false;
        return pendingMapData;
    }

    /// <summary>
    /// Kiểm tra xem Scene này đã từng được gen map lần nào chưa (trong Save File).
    /// </summary>
    public int[,] GetSavedMap(string sceneName)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return null;

        SavedMapData savedData = SaveManager.Instance.Data.savedMaps.Find(m => m.sceneName == sceneName);
        if (savedData != null && savedData.grid != null && savedData.grid.Count > 0)
        {
            // Unflatten list 1 chiều thành mảng 2 chiều
            int[,] matrix = new int[savedData.height, savedData.width];
            for (int y = 0; y < savedData.height; y++)
            {
                for (int x = 0; x < savedData.width; x++)
                {
                    matrix[y, x] = savedData.grid[y * savedData.width + x];
                }
            }
            return matrix;
        }
        return null;
    }

    /// <summary>
    /// Lưu map vừa được gen vào Save File cho scene hiện tại.
    /// </summary>
    public void SaveMapData(string sceneName, int[,] matrix)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;

        int h = matrix.GetLength(0);
        int w = matrix.GetLength(1);
        List<int> gridList = new List<int>(w * h);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                gridList.Add(matrix[y, x]);
            }
        }

        SavedMapData newMap = new SavedMapData
        {
            sceneName = sceneName,
            width = w,
            height = h,
            grid = gridList
        };

        // Nếu trùng scene thì xoá cái cũ, lưu cái mới
        SaveManager.Instance.Data.savedMaps.RemoveAll(m => m.sceneName == sceneName);
        SaveManager.Instance.Data.savedMaps.Add(newMap);

        // Gọi SaveManager để ghi xuống ổ cứng
        SaveManager.Instance.SaveGame();
    }

    /// <summary>
    /// Sinh map ngẫu nhiên bằng thuật toán procedural.
    /// </summary>
    private int[,] GenerateProceduralMap()
    {
        int[,] map = new int[mapHeight, mapWidth];

        // Random tỉ lệ % thực tế cho đợt gen này (nằm trong khoảng min-max)
        int currentWaterPercent = Random.Range(waterPercentRange.x, waterPercentRange.y + 1);
        int currentTreePercent = Random.Range(treePercentRange.x, treePercentRange.y + 1);
        int currentRockPercent = Random.Range(rockPercentRange.x, rockPercentRange.y + 1);
        int currentEnemyCount = Random.Range(enemyCountRange.x, enemyCountRange.y + 1);

        Debug.Log($"[MapGen] Yêu cầu map này: Water {currentWaterPercent}%, Tree {currentTreePercent}%, Rock {currentRockPercent}%, Enemy Target Count: {currentEnemyCount} con");

        // Bước 1: Lấp đầy nền Grass
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                map[y, x] = grassId;

        // Bước 2: Đặt cụm nước (water clusters)
        int waterCells = Mathf.RoundToInt(mapWidth * mapHeight * currentWaterPercent / 100f);
        int placed = 0;
        int attempts = 0;
        while (placed < waterCells && attempts < waterCells * 5)
        {
            attempts++;
            int cx = Random.Range(1, mapWidth - 1);
            int cy = Random.Range(1, mapHeight - 1);

            if (map[cy, cx] != grassId) continue;

            // Mọc cụm nước từ điểm seed
            int clusterSize = Random.Range(1, waterClusterSize + 1);
            List<Vector2Int> cluster = new List<Vector2Int>();
            cluster.Add(new Vector2Int(cx, cy));
            map[cy, cx] = waterId;
            placed++;

            for (int i = 0; i < clusterSize && placed < waterCells; i++)
            {
                Vector2Int current = cluster[Random.Range(0, cluster.Count)];
                // Chọn hướng ngẫu nhiên
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                Vector2Int dir = dirs[Random.Range(0, dirs.Length)];
                Vector2Int next = current + dir;

                if (next.x > 0 && next.x < mapWidth - 1 && next.y > 0 && next.y < mapHeight - 1)
                {
                    if (map[next.y, next.x] == grassId)
                    {
                        map[next.y, next.x] = waterId;
                        cluster.Add(next);
                        placed++;
                    }
                }
            }
        }

        // Bước 3: Rải Tree (phân bổ đều toàn map theo sector)
        int treeCells = Mathf.RoundToInt(mapWidth * mapHeight * currentTreePercent / 100f);
        PlaceRandomDistributed(map, treeId, treeCells);

        // Bước 4: Rải Rock (phân bổ đều toàn map theo sector)
        int rockCells = Mathf.RoundToInt(mapWidth * mapHeight * currentRockPercent / 100f);
        PlaceRandomDistributed(map, rockId, rockCells);

        // Bước 5: Rải Enemy (Phân bố đều để tránh tụ tập)
        PlaceEnemiesEvenly(map, enemyIds, currentEnemyCount, minEnemyDistance);

        // Bước 6: Đảm bảo vùng trống ở góc trên trái (spawn point cho player)
        int safeZone = 2; // Ví dụ 2x2
        for (int sy = 0; sy < safeZone && sy < mapHeight; sy++)
            for (int sx = 0; sx < safeZone && sx < mapWidth; sx++)
                map[sy, sx] = grassId;

        // BƯỚC 6.5: BẢO VỆ ĐƯỜNG VIỀN BAO QUANH MAP (Border Safe Zone)
        // Chắn chắn rằng người chơi luôn có thể đi một vòng quanh mép bản đồ (chỗ hay đặt Portal/Cửa)
        // bằng cách đè lại Grass lên 1 lớp ô mép ngoài cùng.
        for (int x = 0; x < mapWidth; x++)
        {
            map[0, x] = grassId;               // Cạnh dưới
            map[mapHeight - 1, x] = grassId;   // Cạnh trên
        }
        for (int y = 0; y < mapHeight; y++)
        {
            map[y, 0] = grassId;               // Cạnh trái
            map[y, mapWidth - 1] = grassId;    // Cạnh phải
        }

        // Bước 7: Đảm bảo map luôn thông - không có vùng bị nước cô lập
        EnsureConnectivity(map);

        return map;
    }

    /// <summary>
    /// Dùng BFS flood fill từ safe zone để kiểm tra mọi ô non-water có reachable không.
    /// Nếu có vùng bị cô lập bởi nước → xóa water tile gây chặn đường.
    /// Lặp tối đa 5 lần đến khi map thông hoàn toàn.
    /// </summary>
    private void EnsureConnectivity(int[,] map)
    {
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        int totalCleared = 0;

        for (int pass = 0; pass < 5; pass++)
        {
            // BFS từ safe zone (0,0) để tìm tất cả ô non-water có thể đi được
            bool[,] reachable = new bool[mapHeight, mapWidth];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            // Seed điểm xuất phát: safe zone góc trên-trái (luôn là grass)
            reachable[0, 0] = true;
            queue.Enqueue(new Vector2Int(0, 0));

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + dx[d];
                    int ny = cur.y + dy[d];
                    if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight) continue;
                    if (reachable[ny, nx]) continue;
                    
                    // KẺ THÙ CHẶN ĐƯỜNG: Nước (Water), Cây (Tree), Đá (Rock) đều CHẶN đường đi
                    int tileId = map[ny, nx];
                    if (tileId == waterId || tileId == treeId || tileId == rockId) continue;

                    reachable[ny, nx] = true;
                    // Đường đi hợp lệ là Cỏ (Grass) hoặc chỗ Quái Đứng (Enemy)
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }

            // Tìm ô "Có thể đi" (Grass/Enemy) bị cô lập → xóa vật cản kề bên để mở đường
            int clearedThisPass = 0;
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int tileId = map[y, x];
                    // Nếu chính nó là vật cản thì bỏ qua (chúng ta chỉ cứu những ô Grass/Enemy bị nhốt)
                    if (tileId == waterId || tileId == treeId || tileId == rockId) continue;
                    
                    if (reachable[y, x]) continue; // Đã liên thông với Safe Zone, ok

                    // NHỐT! Ô grass/enemy này bị cô lập. Phá vật cản TÙY Ý kề cạnh để nối đường
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d];
                        int ny = y + dy[d];
                        if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight) continue;
                        
                        int neighbor = map[ny, nx];
                        if (neighbor == waterId || neighbor == treeId || neighbor == rockId)
                        {
                            map[ny, nx] = grassId; // PHÁ!
                            clearedThisPass++;
                        }
                    }
                }
            }

            totalCleared += clearedThisPass;
            if (clearedThisPass == 0) break; // map đã thông hoàn toàn
        }

        if (totalCleared > 0)
            Debug.Log($"[MapGen] EnsureConnectivity: Đã phá {totalCleared} vật cản (Nước/Cây/Đá) gây điếc/ngõ cụt → Map thông.");
        else
            Debug.Log("[MapGen] EnsureConnectivity: Map tự nhiên 100% thông, không cần can thiệp.");
    }

    /// <summary>
    /// Đặt ngẫu nhiên 1 loại tile lên các ô Grass, phân bổ ĐỀU theo lưới Sector
    /// để element không bị tụ cụm ở 1 vùng mà trải khắp bản đồ.
    /// </summary>
    private void PlaceRandomDistributed(int[,] map, int id, int count)
    {
        // Chia map thành lưới sector, mỗi sector chứa 1 số lượng element tương ứng
        int sectorCols = 5;
        int sectorRows = 4;
        int totalSectors = sectorCols * sectorRows;

        // Phân chia số lượng cho từng sector (một số sector sẽ có 1 phần tử hơn)
        int baseCount = count / totalSectors;
        int remainder = count % totalSectors;

        // Xáo trộn thứ tự sector để phần dư phân bố ngẫu nhiên
        List<int> sectorOrder = new List<int>();
        for (int s = 0; s < totalSectors; s++) sectorOrder.Add(s);
        for (int s = sectorOrder.Count - 1; s > 0; s--)
        {
            int rand = Random.Range(0, s + 1);
            int tmp = sectorOrder[s]; sectorOrder[s] = sectorOrder[rand]; sectorOrder[rand] = tmp;
        }

        int placed = 0;
        for (int si = 0; si < totalSectors; si++)
        {
            int sectorIdx = sectorOrder[si];
            int sc = sectorIdx % sectorCols;
            int sr = sectorIdx / sectorCols;

            // Biên của sector trong không gian map
            int xMin = sc * mapWidth / sectorCols;
            int xMax = (sc + 1) * mapWidth / sectorCols;
            int yMin = sr * mapHeight / sectorRows;
            int yMax = (sr + 1) * mapHeight / sectorRows;

            int sectorTarget = baseCount + (si < remainder ? 1 : 0);
            int sectorPlaced = 0;
            int attempts = 0;
            int maxAttempts = sectorTarget * 20 + 50;

            while (sectorPlaced < sectorTarget && attempts < maxAttempts)
            {
                attempts++;
                int x = Random.Range(xMin, xMax);
                int y = Random.Range(yMin, yMax);

                if (map[y, x] == grassId)
                {
                    map[y, x] = id;
                    sectorPlaced++;
                    placed++;
                }
            }
        }
        Debug.Log($"[MapGen] PlaceDistributed id={id}: đã đặt {placed}/{count}");
    }

    /// <summary>
    /// Đặt Enemy phân bổ đều theo sector + đảm bảo luôn đặt đủ số lượng tối thiểu
    /// bằng fallback (bỏ điều kiện khoảng cách nếu cần).
    /// </summary>
    private void PlaceEnemiesEvenly(int[,] map, List<int> ids, int count, int minDistance)
    {
        if (ids == null || ids.Count == 0) return;

        int placed = 0;
        List<Vector2Int> placedPositions = new List<Vector2Int>();

        // --- PASS 1: Phân bổ theo sector, có giữ khoảng cách minDistance ---
        int sectorCols = 4;
        int sectorRows = 3;
        int totalSectors = sectorCols * sectorRows;

        // Xáo trộn thứ tự sector
        List<int> sectorOrder = new List<int>();
        for (int s = 0; s < totalSectors; s++) sectorOrder.Add(s);
        for (int s = sectorOrder.Count - 1; s > 0; s--)
        {
            int rand = Random.Range(0, s + 1);
            int tmp = sectorOrder[s]; sectorOrder[s] = sectorOrder[rand]; sectorOrder[rand] = tmp;
        }

        // Phân phần nhỏ quái cho từng sector để trải đều
        int baseCount = count / totalSectors;
        int remainder = count % totalSectors;

        for (int si = 0; si < totalSectors && placed < count; si++)
        {
            int sectorIdx = sectorOrder[si];
            int sc = sectorIdx % sectorCols;
            int sr = sectorIdx / sectorCols;

            int xMin = sc * mapWidth / sectorCols + 1;
            int xMax = (sc + 1) * mapWidth / sectorCols - 1;
            int yMin = sr * mapHeight / sectorRows + 1;
            int yMax = (sr + 1) * mapHeight / sectorRows - 1;

            // Đảm bảo sector có chiều rộng tối thiểu
            if (xMax <= xMin) xMax = xMin + 1;
            if (yMax <= yMin) yMax = yMin + 1;

            int sectorTarget = baseCount + (si < remainder ? 1 : 0);
            int sectorPlaced = 0;

            for (int attempt = 0; attempt < 300 && sectorPlaced < sectorTarget; attempt++)
            {
                int x = Random.Range(xMin, xMax);
                int y = Random.Range(yMin, yMax);

                if (map[y, x] != grassId) continue;

                // Nới lỏng dần khoảng cách theo số lần thử
                float currentMinDist = attempt < 100 ? minDistance :
                                       attempt < 200 ? Mathf.Max(1f, minDistance - 1) : 0.5f;

                bool tooClose = false;
                foreach (Vector2Int pos in placedPositions)
                {
                    if (Vector2Int.Distance(new Vector2Int(x, y), pos) < currentMinDist)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    map[y, x] = ids[Random.Range(0, ids.Count)];
                    placedPositions.Add(new Vector2Int(x, y));
                    sectorPlaced++;
                    placed++;
                }
            }
        }

        // --- PASS 2 (Fallback): Nếu vẫn chưa đủ số lượng tối thiểu, thả tự do vào bất kỳ ô Grass ---
        if (placed < count)
        {
            Debug.LogWarning($"[MapGen] Pass 1 chỉ đặt được {placed}/{count} quái. Dùng fallback...");
            int maxFallbackAttempts = (count - placed) * 500;
            int fallbackAttempts = 0;

            while (placed < count && fallbackAttempts < maxFallbackAttempts)
            {
                fallbackAttempts++;
                int x = Random.Range(1, mapWidth - 1);
                int y = Random.Range(1, mapHeight - 1);

                if (map[y, x] == grassId)
                {
                    map[y, x] = ids[Random.Range(0, ids.Count)];
                    placedPositions.Add(new Vector2Int(x, y));
                    placed++;
                }
            }
        }

        Debug.Log($"[MapGen] ✅ Thực tế đã sinh được {placed}/{count} con quái trên map {mapWidth}x{mapHeight}.");
    }
}
