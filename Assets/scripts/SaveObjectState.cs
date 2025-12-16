using System;

[Serializable]
public class SaveObjectState
{
    public string id;          // PersistentID
    public string type;        // Optional: "Chest", "Door", "Puzzle"

    // Chest
    public bool chestOpen;
    public bool chestSpawned;

    // Door
    public bool doorOpen;
    public bool doorLockedByPuzzle;

    // Puzzle (Vishnu)
    public int puzzlePlacedCorrect;

    // Weapon Socket
    public string socketWeaponID;   // uniqueID of placed Vishnu weapon, null/empty if none
    public string socketWeaponName; // fallback: name/itemName of weapon
}
