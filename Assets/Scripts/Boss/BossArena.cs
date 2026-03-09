using System.Collections;
using UnityEngine;

/// <summary>
/// Manages a single boss arena room.
/// - Locks doors when player enters the trigger zone.
/// - Spawns the linked boss prefab at the spawn point.
/// - Unlocks doors after the boss is defeated.
/// Assign arena door GameObjects (walls/gates) via the Inspector.
/// </summary>
public class BossArena : MonoBehaviour
{
    [Header("Arena Setup")]
    [SerializeField] private GameObject[] arenaDoors;
    [SerializeField] private GameObject   bossPrefab;
    [SerializeField] private Transform    bossSpawnPoint;

    public bool   BossDefeated   { get; private set; }
    public bool   BossActivated  { get; private set; }

    private GameObject bossInstance;

    private void Start()
    {
        // Doors start open so player can freely explore until they step in
        SetDoors(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!BossActivated && other.CompareTag("Player"))
            ActivateBoss();
    }

    /// <summary>Can also be called externally (e.g. by BossSequenceManager after prior boss dies).</summary>
    public void ActivateBoss()
    {
        if (BossActivated) return;
        BossActivated = true;

        SetDoors(true); // lock player inside

        if (bossPrefab != null && bossSpawnPoint != null)
        {
            bossInstance = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
            BossHealth bh = bossInstance.GetComponent<BossHealth>();
            if (bh != null)
                bh.OnBossDied += HandleBossDied;
        }
    }

    private void HandleBossDied()
    {
        BossDefeated = true;
        SetDoors(false); // open exit doors
        BossSequenceManager.Instance?.OnBossArenaCleared(this);
    }

    private void SetDoors(bool active)
    {
        foreach (var door in arenaDoors)
        {
            if (door != null) door.SetActive(active);
        }
    }
}
