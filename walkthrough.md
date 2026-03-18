# Walkthrough: Scene Flexibility System – Forest Swordsman

## Tổng quan
Hệ thống cho phép tạo nhanh map từ file JSON matrix, thêm dynamic spawn trong mỗi Unity Scene.

## Các file đã tạo

| File | Vị trí | Mô tả |
|------|---------|-------|
| [MapData.cs](file:///D:/T/PRU_/PRU_Clone/Forest-Swordsman/Forest-Swordsman/Assets/Scripts/Scene%20Management/MapData.cs) | `Scripts/Scene Management/` | Model JSON: MapData, MapLayers, ExitData, EntranceData |
| [PrefabRegistry.cs](file:///D:/T/PRU_/PRU_Clone/Forest-Swordsman/Forest-Swordsman/Assets/Scripts/Scene%20Management/PrefabRegistry.cs) | `Scripts/Scene Management/` | ScriptableObject: 3 list prefab (ground/objects/enemies) |
| [MapLoader.cs](file:///D:/T/PRU_/PRU_Clone/Forest-Swordsman/Forest-Swordsman/Assets/Scripts/Scene%20Management/MapLoader.cs) | `Scripts/Scene Management/` | Singleton: đọc JSON, spawn 3 layer, đặt player |
| [AreaDynamicExit.cs](file:///D:/T/PRU_/PRU_Clone/Forest-Swordsman/Forest-Swordsman/Assets/Scripts/Scene%20Management/AreaDynamicExit.cs) | `Scripts/Scene Management/` | Trigger chuyển map (same-scene hoặc cross-scene) |
| [forest_01.json](file:///D:/T/PRU_/PRU_Clone/Forest-Swordsman/Forest-Swordsman/Assets/Resources/Maps/forest_01.json) | `Resources/Maps/` | Map demo 8×10, exit sang forest_02 |
| [forest_02.json](file:///D:/T/PRU_/PRU_Clone/Forest-Swordsman/Forest-Swordsman/Assets/Resources/Maps/forest_02.json) | `Resources/Maps/` | Map demo 8×10, exit quay lại forest_01 |

## Prefab Index Convention

### Ground Layer
| Index | Prefab |
|-------|--------|
| 0 | (empty – skip) |
| 1 | Grass / Ground tile |

### Objects Layer
| Index | Prefab |
|-------|--------|
| 0 | (empty) |
| 1 | Flower |
| 2 | Tree |
| 3 | Bush |
| 4 | Torch |

### Enemies Layer
| Index | Prefab |
|-------|--------|
| 0 | (empty) |
| 1–4 | (reserved) |
| 5 | Slime |
| 6 | Ghost |

---

## Setup trong Unity Editor

### Bước 1 – Tạo PrefabRegistry asset
1. Chuột phải trong `Assets/ScriptableObjects/` → **Create > Map > Prefab Registry**
2. Đặt tên `PrefabRegistry`
3. Assign prefabs vào 3 list theo bảng index ở trên

> [!IMPORTANT]
> **Index 0 phải để null** (slot đầu tiên bỏ trống). Ví dụ `objectPrefabs[0] = null`, `objectPrefabs[1] = Flower`, `objectPrefabs[2] = Tree`, v.v.

### Bước 2 – Tạo MapLoader GameObject trong mỗi scene có dynamic map

1. Tạo **empty GameObject** tên `MapLoader`
2. Attach script **MapLoader**
3. Assign `PrefabRegistry` asset vào field **Registry**
4. Tạo 3 empty GameObject con (hoặc riêng): `GroundRoot`, `ObjectRoot`, `EnemyRoot`
5. Assign 3 transform này vào **Ground Root / Object Root / Enemy Root** của MapLoader

### Bước 3 – Thêm AreaDynamicExit vào scene

1. Tạo invisible GameObject với **BoxCollider2D** (Is Trigger = true)
2. Attach script **AreaDynamicExit**
3. Điền **Target Map Id** = `"forest_01"` (tên file JSON không có `.json`)
4. Điền **Target Entrance Name** = `"entrance_right"` (tên entrance trong JSON target)
5. Để **Target Scene Name** trống = dynamic same-scene mode

### Bước 4 – Load map ban đầu

Gọi từ code khi scene bắt đầu (ví dụ trong `Start()` của GameManager):

```csharp
MapLoader.Instance.LoadMap("forest_01", "entrance_right");
```

---

## Tạo map mới

Chỉ cần tạo file JSON mới trong `Assets/Resources/Maps/`:

```json
{
  "id": "cave_01",
  "tileSize": 1.0,
  "layers": {
    "rows": 6,
    "cols": 8,
    "ground":  [1,1,1,1,1,1,1,1, ...],
    "objects": [0,2,0,0,0,0,3,0, ...],
    "enemies": [0,0,0,5,0,0,0,0, ...]
  },
  "exits": [
    { "row": 2, "col": 7, "targetMapId": "forest_01", "targetEntranceName": "entrance_right" }
  ],
  "entrances": [
    { "name": "cave_entrance", "row": 2, "col": 0 }
  ]
}
```

---

## Kiến trúc tổng quan

```
UnityScene (giữ nguyên, DontDestroyOnLoad)
├── Player (Singleton)
├── UIFade (Singleton)
├── SceneManagement (Singleton)
├── CameraController (Singleton)
└── MapLoader (Singleton) ← MỚI
    ├── GroundRoot  → Spawn từ layers.ground[]
    ├── ObjectRoot  → Spawn từ layers.objects[]
    └── EnemyRoot   → Spawn từ layers.enemies[]

AreaDynamicExit (trigger) → MapLoader.LoadMap(id, entrance)
                          → Clear roots → Spawn mới → Đặt Player
```

## Lưu ý quan trọng

> [!WARNING]
> `AreaExit.cs` cũ vẫn hoạt động độc lập (load Unity Scene khác). `AreaDynamicExit` là script mới, hai loại có thể dùng song song.

> [!TIP]
> Nếu muốn load map ngay khi vào scene, gọi `MapLoader.Instance.LoadMap(id, entrance)` trong `Start()` của bất kỳ script nào (sau khi MapLoader đã Awake).
