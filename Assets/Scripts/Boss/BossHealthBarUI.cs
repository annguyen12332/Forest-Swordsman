using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen boss health bar shown at the top of the screen.
/// Supports multiple simultaneous bosses — displays aggregate (combined) HP.
/// Only hides when ALL registered bosses have died.
///
/// Place this on a Canvas UI GameObject in the boss scene.
/// Requires: Slider (healthSlider), TextMeshProUGUI (bossNameText, phaseText), CanvasGroup.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider            healthSlider;
    [SerializeField] private TextMeshProUGUI   bossNameText;
    [SerializeField] private TextMeshProUGUI   phaseText;
    [SerializeField] private CanvasGroup       canvasGroup;
    [SerializeField] private float             fadeDuration = 0.5f;

    // instanceID → (currentHP, maxHP)
    private readonly Dictionary<int, (int current, int max)> trackedBosses =
        new Dictionary<int, (int current, int max)>();

    private bool isFading;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by BossHealth.Start() for each boss that spawns.
    /// Safe to call from multiple bosses simultaneously.
    /// </summary>
    public void RegisterBoss(int instanceID, int maxHP)
    {
        trackedBosses[instanceID] = (maxHP, maxHP);
        RefreshSlider();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            if (!isFading) StartCoroutine(FadeRoutine(0f, 1f));
        }
        RefreshLabels();
    }

    /// <summary>Called by BossHealth.TakeDamage() every time a boss is hit.</summary>
    public void UpdateBossHealth(int instanceID, int currentHP)
    {
        if (!trackedBosses.ContainsKey(instanceID)) return;
        var entry = trackedBosses[instanceID];
        trackedBosses[instanceID] = (currentHP, entry.max);
        RefreshSlider();
    }

    /// <summary>Called by BossHealth.DieRoutine() when a boss dies. Hides bar only when all bosses are dead.</summary>
    public void UnregisterBoss(int instanceID)
    {
        trackedBosses.Remove(instanceID);
        if (trackedBosses.Count == 0)
        {
            StartCoroutine(HideRoutine());
        }
        else
        {
            RefreshSlider();
            RefreshLabels();
        }
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void RefreshSlider()
    {
        int totalCurrent = 0, totalMax = 0;
        foreach (var b in trackedBosses.Values)
        {
            totalCurrent += b.current;
            totalMax     += b.max;
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = Mathf.Max(totalMax, 1);
            healthSlider.value    = totalCurrent;
        }
    }

    private void RefreshLabels()
    {
        int count = trackedBosses.Count;
        if (bossNameText != null)
            bossNameText.text = "BOSS BATTLE";
        if (phaseText != null)
            phaseText.text = count > 1 ? $"{count} Bosses Remaining" : "Final Boss!";
    }

    private IEnumerator HideRoutine()
    {
        yield return StartCoroutine(FadeRoutine(1f, 0f));
        gameObject.SetActive(false);
    }

    private IEnumerator FadeRoutine(float from, float to)
    {
        if (canvasGroup == null) yield break;
        isFading = true;
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
        isFading = false;
    }
}
