using System.Collections.Generic;
using UnityEngine;

public partial class DesignUiManager
{
    public void OnClickSaveBtn()
    {
        if (_saveInfoPanel == null || _buildingTileManager == null || _dataManager == null)
        {
            Debug.LogWarning("[DesignUiManager] Cannot show save info: Required components are null.");
            return;
        }

        string threadId = _buildingTileManager.CurrentThreadId;
        string threadTitle = GetCurrentThreadTitle();

        if (string.IsNullOrEmpty(threadTitle))
        {
            threadTitle = DefaultThreadTitle;
        }

        if (string.IsNullOrEmpty(threadId))
        {
            threadId = GetThreadIdFromTitle(threadTitle);
            _buildingTileManager.SetCurrentThread(threadId);
        }

        List<string> inputResourceIds;
        Dictionary<string, int> inputResourceCounts;
        List<string> outputResourceIds;
        Dictionary<string, int> outputResourceCounts;

        _buildingTileManager.CalculateProductionChain(threadId, out inputResourceIds, out inputResourceCounts, out outputResourceIds, out outputResourceCounts);
        int totalMaintenance = _buildingTileManager.CalculateTotalMaintenanceCost(threadId);
        
        // 직원 요구사항 계산
        CalculateThreadEmployeeRequirements(threadId, out int requiredWorkers, out int requiredTechnicians, out int requiredResearchers, out int requiredManagers);

        _saveInfoPanel.OnShow(threadTitle, inputResourceIds, inputResourceCounts, outputResourceIds, outputResourceCounts, totalMaintenance, this);
        Debug.Log($"[DesignUiManager] Save info panel shown. Thread ID used for calculation: {threadId}");
        Debug.Log($"[DesignUiManager] Employee requirements: Workers={requiredWorkers}, Technicians={requiredTechnicians}, Researchers={requiredResearchers}, Managers={requiredManagers}");
    }

    public void SaveThreadChanges(string threadName, string categoryId)
    {
        if (_buildingTileManager == null)
        {
            Debug.LogError("[DesignUiManager] Cannot save changes: BuildingTileManager is null.");
            return;
        }

        _buildingTileManager.SaveThreadChanges(threadName, categoryId);

        _currentThreadTitle = threadName;

        DeselectBuilding();
        _gameManager?.ShowWarningPanel("Saved successfully.");
    }

    public void OnClickLoadBtn()
    {
        if (_gameManager == null || _dataManager == null)
        {
            return;
        }

        _gameManager.ShowManageThreadPanel(selectedThreadId =>
        {
            LoadThread(selectedThreadId);
        });
    }

    private void LoadThread(string threadId)
    {
        if (string.IsNullOrEmpty(threadId) || _dataManager == null)
        {
            return;
        }

        ThreadState thread = _dataManager.Thread.GetThread(threadId);
        if (thread == null)
        {
            Debug.LogWarning($"[DesignUiManager] Thread not found: {threadId}");
            return;
        }

        string threadTitle = string.IsNullOrEmpty(thread.threadName) ? DefaultThreadTitle : thread.threadName;
        _currentThreadTitle = threadTitle;

        _buildingTileManager?.SetCurrentThread(threadId);

        Debug.Log($"[DesignUiManager] Thread loaded: {threadId} ({thread.threadName})");
    }

    /// <summary> 스레드의 직원 요구사항을 계산합니다. </summary>
    private void CalculateThreadEmployeeRequirements(string threadId, out int requiredWorkers, out int requiredTechnicians, out int requiredResearchers, out int requiredManagers)
    {
        requiredWorkers = 0;
        requiredTechnicians = 0;
        requiredResearchers = 0;
        requiredManagers = 0;

        if (_buildingTileManager == null || _dataManager == null || _dataManager.Building == null)
        {
            Debug.LogWarning("[DesignUiManager] Cannot calculate employee requirements: Required components are null.");
            return;
        }

        // 현재 스레드의 건물 상태 가져오기
        List<BuildingState> buildingStates = _buildingTileManager.GetCurrentBuildingStates();

        if (buildingStates != null)
        {
            foreach (var buildingState in buildingStates)
            {
                if (buildingState == null || string.IsNullOrEmpty(buildingState.buildingId))
                    continue;

                BuildingData buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    requiredWorkers += buildingData.requiredWorkers;
                    requiredTechnicians += buildingData.requiredTechnicians;
                    requiredResearchers += buildingData.requiredResearchers;
                    requiredManagers += buildingData.requiredManagers;
                }
            }
        }

        Debug.Log($"[DesignUiManager] Calculated employee requirements for thread '{threadId}': Workers={requiredWorkers}, Technicians={requiredTechnicians}, Researchers={requiredResearchers}, Managers={requiredManagers}");
    }
}
