using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MissionRequirement
{
    public ItemData requiredItem;
    public int requiredAmount = 1;
}

[CreateAssetMenu(fileName = "New Mission", menuName = "Missions/Create New Mission")]
public class MissionData : ScriptableObject
{
    [Header("Mission Info")]
    public string missionName = "Find the Key";
    [TextArea] public string description = "Locate the rusty key to unlock the door.";

    [Header("Requirements")]
    public List<MissionRequirement> requirements;

    [Header("Rewards (Optional)")]
    public ItemData rewardItem;
}