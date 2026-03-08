using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Slider masterVolumeSlider;

    private const string MASTER_VOLUME_KEY = "MasterVolume";

    // Khi panel được bật, đọc giá trị đã lưu và đồng bộ lên Slider
    private void OnEnable()
    {
        float savedVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        AudioListener.volume = savedVolume;

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = savedVolume;
    }

    // Gọi bởi Slider's OnValueChanged event trong Unity Inspector
    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
    }
}
