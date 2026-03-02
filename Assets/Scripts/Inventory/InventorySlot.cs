using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private int itemCount = 0;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image itemIcon;

    private void Start() {
        UpdateSlotUI();
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }

    public void SetWeaponInfo(WeaponInfo newWeaponInfo, int amount = 1)
    {
        weaponInfo = newWeaponInfo;
        itemCount = amount;
        UpdateSlotUI();
    }

    public void AddToCount(int amount)
    {
        itemCount += amount;
        UpdateSlotUI();
    }

    public void UseItem()
    {
        if (itemCount > 0)
        {
            itemCount--;
            UpdateSlotUI();
            if (itemCount <= 0)
            {
                weaponInfo = null;
                UpdateSlotUI();
            }
        }
    }

    public int GetCount() {
        return itemCount;
    }

    private void UpdateSlotUI()
    {
        if (countText != null)
        {
            countText.text = itemCount > 1 ? itemCount.ToString() : "";
        }

        if (itemIcon != null)
        {
            if (weaponInfo != null)
            {
                itemIcon.sprite = weaponInfo.weaponIcon;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.gameObject.SetActive(false);
            }
        }
    }
}
