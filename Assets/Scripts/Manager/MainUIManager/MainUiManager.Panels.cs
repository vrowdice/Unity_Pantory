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
            { MainPanelType.Research, _researchPanel },
            { MainPanelType.Event, _eventPanel },
            { MainPanelType.Order, _orderPanel }
        };
    }

    private void InitializePanels()
    {
        foreach (var kvp in _panelDict)
        {
            if (kvp.Value != null)
            {
                // 패널을 닫고 비활성화하여 시작 시 모든 패널이 닫힌 상태로 시작
                kvp.Value.OnClose();
                kvp.Value.gameObject.SetActive(false);
            }
        }
        
        // 현재 열린 패널 타입 초기화
        _currentOpenPanelType = default(MainPanelType);
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

        // 토글 기능: 이미 열려있는 패널을 다시 누르면 닫기
        if (_currentOpenPanelType == panelType && panel.gameObject.activeSelf)
        {
            ClosePanelInternal(panelType);
            _currentOpenPanelType = default(MainPanelType); // 기본값으로 리셋
            return;
        }

        // 다른 패널이 열려있으면 닫기
        if (_panelDict.ContainsKey(_currentOpenPanelType) && _currentOpenPanelType != panelType)
        {
            ClosePanelInternal(_currentOpenPanelType);
        }

        // 새 패널 열기
        panel.OnOpen(_gameManager, _dataManager, this);
        _currentOpenPanelType = panelType;
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
