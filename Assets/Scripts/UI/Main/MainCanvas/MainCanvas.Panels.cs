using System.Collections.Generic;
using UnityEngine;

public partial class MainCanvas
{
    [Header("Panels")]
    [SerializeField] private StoragePanel _storagePanel;
    [SerializeField] private MarketPanel _marketPanel;
    [SerializeField] private EmployeePanel _employmentPanel;
    [SerializeField] private NewsPanel _newsPanel;
    [SerializeField] private ResearchPanel _researchPanel;
    [SerializeField] private OrderPanel _orderPanel;
    [SerializeField] private CreditTopInfoPanel _creditInfoPanel;

    private Dictionary<MainPanelType, BasePanel> _panelDict;
    private MainPanelType _currentOpenPanelType;

    private void InitializePanelDictionary()
    {
        _panelDict = new Dictionary<MainPanelType, BasePanel>
        {
            { MainPanelType.Storage, _storagePanel },
            { MainPanelType.Market, _marketPanel },
            { MainPanelType.Employment, _employmentPanel },
            { MainPanelType.Research, _researchPanel },
            { MainPanelType.News, _newsPanel },
            { MainPanelType.Order, _orderPanel }
        };
    }

    private void InitializePanels()
    {
        foreach (KeyValuePair<MainPanelType, BasePanel> kvp in _panelDict)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnClose();
                kvp.Value.gameObject.SetActive(false);
            }
        }

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

        if (_currentOpenPanelType == panelType && panel.gameObject.activeSelf)
        {
            ClosePanelInternal(panelType);
            _currentOpenPanelType = default(MainPanelType);
            return;
        }

        if (_panelDict.ContainsKey(_currentOpenPanelType) && _currentOpenPanelType != panelType)
        {
            ClosePanelInternal(_currentOpenPanelType);
        }

        panel.Init(this);
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
        foreach (KeyValuePair<MainPanelType, BasePanel> kvp in _panelDict)
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
