using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên GameObject "Heart HUD" trên Canvas.
/// Hiển thị icon tim và số lượng tim còn lại ngoài màn chơi.
/// </summary>
public class HeartHUD : Singleton<HeartHUD>
{
    [SerializeField] private Image heartIcon;
    [SerializeField] private TMP_Text heartCountText;

    private int heartCount = 0;

    private void Start()
    {
        Refresh();
    }

    public void AddHeart(int amount = 1)
    {
        heartCount += amount;
        Refresh();
    }

    public void UseHeart()
    {
        if (heartCount > 0)
        {
            heartCount--;
            Refresh();
        }
    }

    public int GetHeartCount() => heartCount;

    private void Refresh()
    {
        if (heartCountText != null)
            heartCountText.text = heartCount.ToString();

        // Icon luôn hiện, kể cả khi count = 0
        if (heartIcon != null)
            heartIcon.gameObject.SetActive(true);
    }
}
