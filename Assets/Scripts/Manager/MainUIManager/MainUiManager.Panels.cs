using System.Collections.Generic;
using UnityEngine;

public partial class MainUiManager
{
    private void InitializePanelDictionary()
    {
        _panelDict = new Dictionary<MainPanelType, BasePanel>
        {
            { MainPanelType.Storage, _storagePanel },
            { MainPanelType.Market, _marketPanel },
            { MainPanelType.Employment, _employmentPanel },
            { MainPanelType.Finance, _financePanel }
        };
    }

    private void InitializePanels()
    {
        foreach (var kvp in _panelDict)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnClose();
            }
        }

        Debug.Log("[MainUiManager] All panels initialized.");
    }

    public void OpenPanel(MainPanelType panelType)
    {
        if (!_panelDict.ContainsKey(panelType))
        {
            Debug.LogWarning($"[MainUiManager] Panel type {panelType} not found in dictionary.");
            return;
        }

        BasePanel panel = _panelDict[panelType];

        if (panel == null)
        {
            Debug.LogWarning($"[MainUiManager] Panel {panelType} is null.");
            return;
        }

        if (_panelDict.ContainsKey(_currentOpenPanelType))
        {
            ClosePanelInternal(_currentOpenPanelType);
        }

        panel.OnOpen(_dataManager, this);
        _currentOpenPanelType = panelType;
        Debug.Log($"[MainUiManager] Panel {panelType} opened.");
    }

    private void ClosePanelInternal(MainPanelType panelType)
    {
        if (!_panelDict.ContainsKey(panelType))
        {
            return;
        }

        BasePanel panel = _panelDict[panelType];

        if (panel == null)
        {
            return;
        }

        panel.OnClose();
        Debug.Log($"[MainUiManager] Panel {panelType} closed.");
    }

    public void CloseAllPanels()
    {
        foreach (var kvp in _panelDict)
        {
            ClosePanelInternal(kvp.Key);
        }
    }

    public MainPanelType GetCurrentOpenPanelType()
    {
        return _currentOpenPanelType;
    }

    public bool IsPanelOpen(MainPanelType panelType)
    {
        return _currentOpenPanelType == panelType;
    }
}
