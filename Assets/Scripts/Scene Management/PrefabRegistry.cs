using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the list of prefabs used when spawning map elements from a JSON matrix.
/// Index 0 is always reserved as "empty" (null). Assign prefabs in the Inspector
/// so that the index in this list matches the integer value in the JSON matrix.
/// </summary>
[CreateAssetMenu(fileName = "PrefabRegistry", menuName = "Map/Prefab Registry")]
public class PrefabRegistry : ScriptableObject
{
  [Header("Ground / Terrain Prefabs")]
  [Tooltip("Index 0 = empty (null). Index 1+ = ground tile prefabs.")]
  public List<GameObject> groundPrefabs = new List<GameObject>();

  [Header("Object / Decoration Prefabs")]
  [Tooltip("Index 0 = empty (null). Index 1+ = object prefabs (tree, bush, torch).")]
  public List<GameObject> objectPrefabs = new List<GameObject>();

  [Header("Enemy Prefabs")]
  [Tooltip("Index 0 = empty (null). Index 1+ = enemy prefabs (slime, ghost).")]
  public List<GameObject> enemyPrefabs = new List<GameObject>();

  /// <summary>
  /// Returns the ground prefab for the given index, or null if index is 0 or out of range.
  /// </summary>
  public GameObject GetGroundPrefab(int index)
  {
    return GetSafe(groundPrefabs, index);
  }

  /// <summary>
  /// Returns the object prefab for the given index, or null if index is 0 or out of range.
  /// </summary>
  public GameObject GetObjectPrefab(int index)
  {
    return GetSafe(objectPrefabs, index);
  }

  /// <summary>
  /// Returns the enemy prefab for the given index, or null if index is 0 or out of range.
  /// </summary>
  public GameObject GetEnemyPrefab(int index)
  {
    return GetSafe(enemyPrefabs, index);
  }

  private GameObject GetSafe(List<GameObject> list, int index)
  {
    if (index <= 0 || index >= list.Count) return null;
    return list[index];
  }
}
