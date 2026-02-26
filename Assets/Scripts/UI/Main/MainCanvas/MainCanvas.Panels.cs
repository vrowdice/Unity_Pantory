using System.Collections.Generic;
using UnityEngine;

public partial class MainCanvas
{
    [Header("Main Panel Canvas")]
    [SerializeField] private StorageCanvas _storageCanvas;
    [SerializeField] private OrderCanvas _orderCanvas;
    [SerializeField] private MarketCanvas _marketCanvas;
    [SerializeField] private EmployeeCanvas _employmentCanvas;
    [SerializeField] private NewsCanvas _newsCanvas;
    [SerializeField] private ResearchCanvas _researchCanvas;
    [SerializeField] private FinanceCanvas _financeCanvas;

    [SerializeField] private CreditTopInfoPanel _creditInfoPanel;

    private Dictionary<MainPanelType, MainCanvasPanelBase> _panelDict;
    private MainPanelType _currentOpenPanelType;

    private void InitializePanelDictionary()
    {
        _panelDict = new Dictionary<MainPanelType, MainCanvasPanelBase>
        {
            { MainPanelType.Storage, _storageCanvas },
            { MainPanelType.Order, _orderCanvas },
            { MainPanelType.Market, _marketCanvas },
            { MainPanelType.Employment, _employmentCanvas },
            { MainPanelType.Research, _researchCanvas },
            { MainPanelType.News, _newsCanvas },
            { MainPanelType.Finance, _financeCanvas }
        };
    }

    private void InitializePanels()
    {
        foreach (KeyValuePair<MainPanelType, MainCanvasPanelBase> kvp in _panelDict)
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

        MainCanvasPanelBase panel = _panelDict[panelType];

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

        MainCanvasPanelBase panel = _panelDict[panelType];

        if (panel == null)
        {
            return;
        }

        panel.OnClose();
    }

    public void CloseAllPanels()
    {
        foreach (KeyValuePair<MainPanelType, MainCanvasPanelBase> kvp in _panelDict)
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
