using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the sequential 3-boss gauntlet in the boss scene.
/// Place one instance in the boss scene. Tracks defeated arenas,
/// shows boss announcement banners, and loads the victory scene when all 3 are cleared.
///
/// SETUP in Inspector:
///  - bossArenas[0] = Slime King arena
///  - bossArenas[1] = Elder Grape Shaman arena
///  - bossArenas[2] = Forest Chimera arena
/// </summary>
public class BossSequenceManager : MonoBehaviour
{
    public static BossSequenceManager Instance { get; private set; }

    [Header("Arenas (assign in order)")]
    [SerializeField] private BossArena[] bossArenas;

    [Header("Progression Gates (between arenas — start active/blocking)")]
    [SerializeField] private GameObject[] progressionGates;

    [Header("Player Spawn")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Victory")]
    [SerializeField] private string victorySceneName = "Town";
    [SerializeField] private float  victoryDelay     = 4f;

    [Header("Boss Banner UI")]
    [SerializeField] private TextMeshProUGUI bannerText;
    [SerializeField] private CanvasGroup     bannerCanvasGroup;

    private static readonly string[] BossLabels =
    {
        "Boss 1 / 3\n<size=60%>Slime King</size>",
        "Boss 2 / 3\n<size=60%>Elder Grape Shaman</size>",
        "Boss 3 / 3\n<size=60%>Forest Chimera</size>",
    };

    private int defeatedCount;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Replicate what AreaEntrance does for normal scenes:
        // 1) Teleport the DontDestroyOnLoad player to the correct spawn point.
        // 2) Re-wire the persistent CameraController to follow that player.
        if (playerSpawnPoint != null && PlayerController.Instance != null)
            PlayerController.Instance.transform.position = playerSpawnPoint.position;

        CameraController.Instance?.SetPlayerCameraFollow();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Called by BossArena ───────────────────────────────────────────────────

    public void OnBossArenaCleared(BossArena arena)
    {
        defeatedCount++;

        // Open the progression gate to the next arena
        int gateIndex = defeatedCount - 1;
        if (gateIndex < progressionGates.Length && progressionGates[gateIndex] != null)
            progressionGates[gateIndex].SetActive(false);

        if (defeatedCount >= bossArenas.Length)
        {
            StartCoroutine(VictoryRoutine());
            return;
        }

        // Auto-activate the next boss immediately — don't wait for player to walk in
        bossArenas[defeatedCount].ActivateBoss();
    }

    // ── Called by individual boss AIs in their Start() ────────────────────────

    public void ShowBossBanner(int bossIndex)
    {
        StartCoroutine(BannerRoutine(bossIndex));
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private IEnumerator BannerRoutine(int index)
    {
        if (bannerText == null || bannerCanvasGroup == null) yield break;

        bannerText.text = index < BossLabels.Length ? BossLabels[index] : $"Boss {index + 1}";

        yield return StartCoroutine(FadeBanner(0f, 1f, 0.4f));
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(FadeBanner(1f, 0f, 0.4f));
    }

    private IEnumerator VictoryRoutine()
    {
        if (bannerText != null && bannerCanvasGroup != null)
        {
            bannerText.text = "ALL BOSSES DEFEATED!\nVICTORY!";
            yield return StartCoroutine(FadeBanner(0f, 1f, 0.5f));
        }

        yield return new WaitForSeconds(victoryDelay);
        SceneManager.LoadScene(victorySceneName);
    }

    private IEnumerator FadeBanner(float from, float to, float duration)
    {
        if (bannerCanvasGroup == null) yield break;
        float elapsed = 0f;
        bannerCanvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bannerCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        bannerCanvasGroup.alpha = to;
    }
}
