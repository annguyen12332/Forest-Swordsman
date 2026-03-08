using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Weapon")]
public class WeaponInfo : ScriptableObject
{
    public GameObject weaponPrefab;
    public float weaponCooldown;
    public int weaponDamage;
    public float weaponRange;
    public Sprite weaponIcon;
    public int weaponUpgradeLevel = 0; // Thêm cấp độ riêng cho vũ khí
}

