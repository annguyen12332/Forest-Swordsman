using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI Slider that displays enemy HP.
/// Attach to the HealthSlider child object inside the enemy's HealthBarCanvas.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
  [SerializeField] private Slider healthSlider;
  [SerializeField] private Color flashColor = Color.red;
  [SerializeField] private Color baseColor = Color.green;
  [SerializeField] private float flashDuration = 0.2f;

  private Image fillImage;

  private void Awake()
  {
    if (healthSlider == null)
    {
      healthSlider = GetComponent<Slider>();
    }

    if (healthSlider != null && healthSlider.fillRect != null)
    {
      fillImage = healthSlider.fillRect.GetComponent<Image>();
    }
  }

  /// <summary>Sets the max HP and fills the bar to full.</summary>
  public void SetMaxHealth(int maxHealth)
  {
    if (healthSlider == null) { return; }

    healthSlider.maxValue = maxHealth;
    healthSlider.value = maxHealth;
  }

  /// <summary>Updates current HP value directly without flash effect.</summary>
  public void SetHealth(int currentHealth)
  {
    if (healthSlider == null) { return; }

    healthSlider.value = currentHealth;
  }

  /// <summary>Updates HP with a brief red flash effect on the fill bar.</summary>
  public void TakeDamage(int newHealth)
  {
    if (healthSlider == null) { return; }

    StartCoroutine(FlashRoutine(newHealth));
  }

  private IEnumerator FlashRoutine(int targetHealth)
  {
    if (fillImage != null)
    {
      fillImage.color = flashColor;
    }

    yield return new WaitForSeconds(flashDuration);

    healthSlider.value = targetHealth;

    if (fillImage != null)
    {
      fillImage.color = baseColor;
    }
  }
}
