using UnityEngine;

public enum VishnuWeaponType
{
    Chakra,
    Sword,
    Conch,
    Mace
}

[CreateAssetMenu(fileName = "New Vishnu Weapon", menuName = "Inventory/Vishnu Weapon")]
public class VishnuWeaponItemData : ItemData
{
    [Header("Vishnu Puzzle")]
    public VishnuWeaponType weaponType;
}
