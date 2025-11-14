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

        _saveInfoPanel.OnShow(threadTitle, inputResourceIds, inputResourceCounts, outputResourceIds, outputResourceCounts, totalMaintenance, this);
        Debug.Log($"[DesignUiManager] Save info panel shown. Thread ID used for calculation: {threadId}");
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

        ThreadState thread = _dataManager.GetThread(threadId);
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
}
