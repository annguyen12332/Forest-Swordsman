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

    // Lưu lượng bom trong ActiveInventory
    public int bombCount = 0;

    // Lưu lượng kinh nghiệm và cấp độ
    public int playerLevel = 1;
    public int playerXP = 0;

    // Lưu dữ liệu cấp độ của các loại vũ khí theo tên file ScriptableObject
    public List<WeaponSaveData> weaponsData = new List<WeaponSaveData>();

    // Lưu tên scene cuối cùng để Continue load đúng chỗ
    public string lastSceneName = "";
}

[System.Serializable]
public class WeaponSaveData
{
    public string weaponName;
    public int upgradeLevel;
}
