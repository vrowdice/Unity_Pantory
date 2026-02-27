using System.Collections.Generic;
using UnityEngine;

public partial class MainCanvas
{
    [Header("Main Panel Prefabs")]
    [SerializeField] private GameObject _storageCanvasPrefab;
    [SerializeField] private GameObject _orderCanvasPrefab;
    [SerializeField] private GameObject _marketCanvasPrefab;
    [SerializeField] private GameObject _employmentCanvasPrefab;
    [SerializeField] private GameObject _newsCanvasPrefab;
    [SerializeField] private GameObject _researchCanvasPrefab;
    [SerializeField] private GameObject _financeCanvasPrefab;

    private StorageCanvas _storageCanvas;
    private OrderCanvas _orderCanvas;
    private MarketCanvas _marketCanvas;
    private EmployeeCanvas _employmentCanvas;
    private NewsCanvas _newsCanvas;
    private ResearchCanvas _researchCanvas;
    private FinanceCanvas _financeCanvas;

    [SerializeField] private CreditTopInfoPanel _creditInfoPanel;

    private Dictionary<MainPanelType, MainCanvasPanelBase> _panelDict;
    private MainPanelType _currentOpenPanelType;

    private void CreateMainPanels()
    {
        if (_storageCanvas != null)
        {
            return;
        }

        if (_storageCanvasPrefab != null)
        {
            GameObject storageObj = Object.Instantiate(_storageCanvasPrefab);
            _storageCanvas = storageObj.GetComponent<StorageCanvas>();
        }

        if (_orderCanvasPrefab != null)
        {
            GameObject orderObj = Object.Instantiate(_orderCanvasPrefab);
            _orderCanvas = orderObj.GetComponent<OrderCanvas>();
        }

        if (_marketCanvasPrefab != null)
        {
            GameObject marketObj = Object.Instantiate(_marketCanvasPrefab);
            _marketCanvas = marketObj.GetComponent<MarketCanvas>();
        }

        if (_employmentCanvasPrefab != null)
        {
            GameObject employmentObj = Object.Instantiate(_employmentCanvasPrefab);
            _employmentCanvas = employmentObj.GetComponent<EmployeeCanvas>();
        }

        if (_newsCanvasPrefab != null)
        {
            GameObject newsObj = Object.Instantiate(_newsCanvasPrefab);
            _newsCanvas = newsObj.GetComponent<NewsCanvas>();
        }

        if (_researchCanvasPrefab != null)
        {
            GameObject researchObj = Object.Instantiate(_researchCanvasPrefab);
            _researchCanvas = researchObj.GetComponent<ResearchCanvas>();
        }

        if (_financeCanvasPrefab != null)
        {
            GameObject financeObj = Object.Instantiate(_financeCanvasPrefab);
            _financeCanvas = financeObj.GetComponent<FinanceCanvas>();
        }
    }

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
