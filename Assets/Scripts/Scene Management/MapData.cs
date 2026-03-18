using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Root data model for a single map, deserialized from JSON.
/// </summary>
[Serializable]
public class MapData
{
  /// <summary>Unique map identifier, matches the JSON filename.</summary>
  public string id;

  /// <summary>World-space size of each cell in Unity units.</summary>
  public float tileSize = 1f;

  /// <summary>Three separate layers of the map.</summary>
  public MapLayers layers;

  /// <summary>Exit points that link to other maps.</summary>
  public List<ExitData> exits = new List<ExitData>();

  /// <summary>Named spawn positions for incoming transitions.</summary>
  public List<EntranceData> entrances = new List<EntranceData>();
}

/// <summary>
/// Holds the three 2-D layers. Each layer is a flat int[] treated as
/// a row-major 2-D array of size rows x cols.
/// </summary>
[Serializable]
public class MapLayers
{
  /// <summary>Number of rows in each layer matrix.</summary>
  public int rows;

  /// <summary>Number of columns in each layer matrix.</summary>
  public int cols;

  /// <summary>
  /// Ground tiles (terrain). 0 = empty, positive = prefab index in PrefabRegistry.
  /// Flat array, row-major: index = row * cols + col.
  /// </summary>
  public List<int> ground;

  /// <summary>
  /// Static decoration / interactive objects (trees, bushes, chests, torches).
  /// Same encoding as ground.
  /// </summary>
  public List<int> objects;

  /// <summary>
  /// Enemy spawns (slime, ghost). Same encoding as ground.
  /// </summary>
  public List<int> enemies;

  /// <summary>Get prefab index at (row, col) from the ground layer.</summary>
  public int GetGround(int row, int col) => (ground != null && ground.Count > row * cols + col) ? ground[row * cols + col] : 0;

  /// <summary>Get prefab index at (row, col) from the objects layer.</summary>
  public int GetObject(int row, int col) => (objects != null && objects.Count > row * cols + col) ? objects[row * cols + col] : 0;

  /// <summary>Get prefab index at (row, col) from the enemies layer.</summary>
  public int GetEnemy(int row, int col) => (enemies != null && enemies.Count > row * cols + col) ? enemies[row * cols + col] : 0;
}

/// <summary>
/// Describes a trigger cell where the player exits to another map.
/// </summary>
[Serializable]
public class ExitData
{
  /// <summary>Row of the exit trigger cell.</summary>
  public int row;

  /// <summary>Column of the exit trigger cell.</summary>
  public int col;

  /// <summary>ID of the target map JSON file (without extension).</summary>
  public string targetMapId;

  /// <summary>Name of the entrance in the target map where the player spawns.</summary>
  public string targetEntranceName;
}

/// <summary>
/// A named spawn point in this map, referenced by incoming exits from other maps.
/// </summary>
[Serializable]
public class EntranceData
{
  /// <summary>Unique name, matched by ExitData.targetEntranceName.</summary>
  public string name;

  /// <summary>Row of the spawn cell.</summary>
  public int row;

  /// <summary>Column of the spawn cell.</summary>
  public int col;
}
