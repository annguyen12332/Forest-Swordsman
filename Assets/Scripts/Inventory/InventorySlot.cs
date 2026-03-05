using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private HeartItemInfo heartItemInfo;
    [SerializeField] private int itemCount = 0;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image itemIcon;

    private void Awake()
    {
        // Tự tạo Item Icon nếu chưa có
        if (itemIcon == null)
        {
            Transform existing = transform.Find("Item Icon");
            if (existing != null)
            {
                itemIcon = existing.GetComponent<Image>();
                if (itemIcon != null)
                {
                    itemIcon.color = Color.clear; // ẩn mặc định, tránh hình trắng
                    itemIcon.sprite = null;
                }
            }
            else
            {
                GameObject iconGO = new GameObject("Item Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(transform, false);

                RectTransform rt = iconGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.1f, 0.1f);
                rt.anchorMax = new Vector2(0.9f, 0.9f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                itemIcon = iconGO.GetComponent<Image>();
                itemIcon.preserveAspect = true;
                itemIcon.raycastTarget = false;
                itemIcon.color = Color.clear; // trong suốt mặc định
            }
        }

        // Tự tạo Count Text nếu chưa có
        if (countText == null)
        {
            Transform existing = transform.Find("Count Text");
            if (existing != null)
            {
                countText = existing.GetComponent<TMP_Text>();
                if (countText != null)
                {
                    countText.text = ""; // xóa text mặc định "0"
                    countText.fontSize = 14;
                }
            }
            else
            {
                GameObject textGO = new GameObject("Count Text", typeof(RectTransform));
                textGO.transform.SetParent(transform, false);

                RectTransform rt = textGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(1f, 0.4f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                countText = textGO.AddComponent<TextMeshProUGUI>();
                countText.fontSize = 14;
                countText.fontStyle = FontStyles.Bold;
                countText.color = Color.white;
                countText.alignment = TextAlignmentOptions.BottomRight;
                countText.raycastTarget = false;
                countText.text = "";
            }
        }
    }

    private void Start()
    {
        UpdateSlotUI();
    }

    // ─── Weapon ───────────────────────────────────────────
    public WeaponInfo GetWeaponInfo() => weaponInfo;

    public void SetWeaponInfo(WeaponInfo newWeaponInfo, int amount = 1)
    {
        weaponInfo = newWeaponInfo;
        heartItemInfo = null;
        itemCount = amount;
        UpdateSlotUI();
    }

    // ─── Heart Item ───────────────────────────────────────
    public HeartItemInfo GetHeartItemInfo() => heartItemInfo;

    public void SetHeartItem(HeartItemInfo info, int amount = 1)
    {
        heartItemInfo = info;
        weaponInfo = null;
        itemCount = amount;
        UpdateSlotUI();
    }

    // ─── Chung ────────────────────────────────────────────
    public void AddToCount(int amount)
    {
        itemCount += amount;
        UpdateSlotUI();
    }

    /// <summary>Dùng 1 item — luôn trừ count, kể cả máu đầy.</summary>
    public void UseItem()
    {
        if (itemCount > 0)
        {
            itemCount--;
            if (itemCount <= 0)
            {
                weaponInfo = null;
                heartItemInfo = null;
            }
            UpdateSlotUI();
        }
    }

    public int GetCount() => itemCount;

    public bool IsEmpty() => weaponInfo == null && heartItemInfo == null;

    private void UpdateSlotUI()
    {
        if (countText != null)
            countText.text = itemCount > 0 ? itemCount.ToString() : "";

        if (itemIcon != null)
        {
            Sprite icon = null;
            if (weaponInfo != null)         icon = weaponInfo.weaponIcon;
            else if (heartItemInfo != null)  icon = heartItemInfo.itemIcon;

            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.color = Color.white; // hiện ảnh
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.color = Color.clear; // ẩn bằng transparent
            }
        }
    }
}
