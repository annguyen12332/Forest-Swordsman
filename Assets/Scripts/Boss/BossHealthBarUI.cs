using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen boss health bar shown at the top of the screen.
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

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    /// <summary>Called by BossHealth.Start() when a new boss enters the arena.</summary>
    public void InitBoss(string bossName, int maxHealth)
    {
        if (bossNameText != null) bossNameText.text = bossName;
        if (phaseText != null)    phaseText.text     = "Phase 1";
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value    = maxHealth;
        }
        gameObject.SetActive(true);
        StartCoroutine(FadeRoutine(0f, 1f));
    }

    /// <summary>Update slider value as boss takes damage.</summary>
    public void UpdateHealth(int currentHealth)
    {
        if (healthSlider != null) healthSlider.value = currentHealth;
    }

    /// <summary>Update phase label when boss phase changes.</summary>
    public void UpdatePhaseText(int phase)
    {
        if (phaseText != null) phaseText.text = $"Phase {phase}";
    }

    /// <summary>Fade out and hide after boss death.</summary>
    public void Hide()
    {
        StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        yield return StartCoroutine(FadeRoutine(1f, 0f));
        gameObject.SetActive(false);
    }

    private IEnumerator FadeRoutine(float from, float to)
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
