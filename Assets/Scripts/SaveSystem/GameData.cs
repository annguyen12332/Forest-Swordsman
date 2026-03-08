using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // Lưu lượng vàng
    public int currentGold = 0;

    // Lưu lượng đá nâng cấp (Gems)
    public int gemCount = 0;

    // Lưu lượng tim (Hearts) trong hành trang
    public int heartCount = 0;

    // Lưu dữ liệu cấp độ của các loại vũ khí theo tên file ScriptableObject
    public List<WeaponSaveData> weaponsData = new List<WeaponSaveData>();
}

[System.Serializable]
public class WeaponSaveData
{
    public string weaponName;
    public int upgradeLevel;
}
