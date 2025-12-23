using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Configuration")]
    public List<MissionData> allMissions;
    public int currentMissionIndex = 0;

    [Header("UI References")]
    public TextMeshProUGUI missionTrackerText;
    public TextMeshProUGUI notificationText;
    public float notificationDuration = 3f;

    private bool allMissionsCompleted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject); // Singleton safety
    }

    private void Start()
    {
        if (notificationText != null) notificationText.gameObject.SetActive(false);

        // Wait one frame to ensure Inventory is ready, then update UI
        StartCoroutine(StartRoutine());
    }

    private IEnumerator StartRoutine()
    {
        yield return null;
        UpdateMissionUI();
    }

    private void Update()
    {
        if (allMissionsCompleted) return;
        CheckCurrentMissionProgress();
    }

    private void CheckCurrentMissionProgress()
    {
        if (currentMissionIndex >= allMissions.Count || allMissions == null) return;

        MissionData currentMission = allMissions[currentMissionIndex];
        if (currentMission == null || currentMission.requirements == null) return;

        bool isMissionComplete = true;
        string progressText = $"<b>{currentMission.missionName}</b>\n";

        foreach (var req in currentMission.requirements)
        {
            if (req == null || req.requiredItem == null) continue;

            int currentCount = 0;
            if (InventorySystem.Instance != null)
            {
                currentCount = InventorySystem.Instance.GetItemCount(req.requiredItem);
            }
            else
            {
                Debug.LogWarning("InventorySystem.Instance is null!");
            }

            string color = (currentCount >= req.requiredAmount) ? "<color=green>" : "<color=red>";
            progressText += $"{color}- {req.requiredItem.itemName}: {currentCount}/{req.requiredAmount}</color>\n";

            if (currentCount < req.requiredAmount) isMissionComplete = false;
        }

        if (missionTrackerText != null)
            missionTrackerText.text = progressText;

        if (isMissionComplete)
            CompleteMission();
    }


    private void CompleteMission()
    {
        currentMissionIndex++;
        StartCoroutine(ShowNotification($"Mission Complete!"));

        if (currentMissionIndex >= allMissions.Count)
        {
            allMissionsCompleted = true;
            if (missionTrackerText != null) missionTrackerText.text = "<color=green>ALL MISSIONS COMPLETED</color>";
        }
    }

    private string GetCurrentMissionName()
    {
        if (currentMissionIndex < allMissions.Count)
            return allMissions[currentMissionIndex].missionName;
        return "None";
    }

    private void UpdateMissionUI()
    {
        if (currentMissionIndex >= allMissions.Count)
        {
            if (missionTrackerText != null) missionTrackerText.text = "<color=green>ALL MISSIONS COMPLETED</color>";
            allMissionsCompleted = true;
            return;
        }
        CheckCurrentMissionProgress();
    }

    IEnumerator ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            yield return new WaitForSeconds(notificationDuration);
            notificationText.gameObject.SetActive(false);
        }
    }

    // --- SAVE / LOAD METHODS ---

    public int GetMissionIndex()
    {
        return currentMissionIndex;
    }

    public void LoadMissionProgress(int savedIndex)
    {
        currentMissionIndex = savedIndex;

        if (currentMissionIndex >= allMissions.Count)
        {
            allMissionsCompleted = true;
            if (missionTrackerText != null) missionTrackerText.text = "<color=green>ALL MISSIONS COMPLETED</color>";
        }
        else
        {
            allMissionsCompleted = false;
            UpdateMissionUI();
        }
    }
}