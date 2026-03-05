using UnityEngine;

[CreateAssetMenu(menuName = "New Heart Item")]
public class HeartItemInfo : ScriptableObject
{
    public Sprite itemIcon;
    public int healAmount = 1;
}
