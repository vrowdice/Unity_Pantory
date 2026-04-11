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

    private Dictionary<MainPanelType, MainCanvasPanelBase> _panelDict;
    private MainPanelType? _currentOpenPanelType;

    private void CreateMainPanels()
    {
        if (_storageCanvas != null)
            return;

        GameObject storageObj = Object.Instantiate(_storageCanvasPrefab);
        storageObj.name = _storageCanvasPrefab.name;
        _storageCanvas = storageObj.GetComponent<StorageCanvas>();

        GameObject orderObj = Object.Instantiate(_orderCanvasPrefab);
        orderObj.name = _orderCanvasPrefab.name;
        _orderCanvas = orderObj.GetComponent<OrderCanvas>();

        GameObject marketObj = Object.Instantiate(_marketCanvasPrefab);
        marketObj.name = _marketCanvasPrefab.name;
        _marketCanvas = marketObj.GetComponent<MarketCanvas>();

        GameObject employmentObj = Object.Instantiate(_employmentCanvasPrefab);
        employmentObj.name = _employmentCanvasPrefab.name;
        _employmentCanvas = employmentObj.GetComponent<EmployeeCanvas>();

        GameObject newsObj = Object.Instantiate(_newsCanvasPrefab);
        newsObj.name = _newsCanvasPrefab.name;
        _newsCanvas = newsObj.GetComponent<NewsCanvas>();

        GameObject researchObj = Object.Instantiate(_researchCanvasPrefab);
        researchObj.name = _researchCanvasPrefab.name;
        _researchCanvas = researchObj.GetComponent<ResearchCanvas>();

        GameObject financeObj = Object.Instantiate(_financeCanvasPrefab);
        financeObj.name = _financeCanvasPrefab.name;
        _financeCanvas = financeObj.GetComponent<FinanceCanvas>();
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
            kvp.Value.OnClose();
            kvp.Value.gameObject.SetActive(false);
        }

        _currentOpenPanelType = null;
    }

    public void OpenPanel(MainPanelType panelType)
    {
        MainCanvasPanelBase panel = _panelDict[panelType];

        if (_currentOpenPanelType == panelType && panel.gameObject.activeSelf)
        {
            ClosePanelInternal(panelType);
            _currentOpenPanelType = null;
            return;
        }

        if (_currentOpenPanelType.HasValue && _currentOpenPanelType.Value != panelType)
            ClosePanelInternal(_currentOpenPanelType.Value);

        panel.Init(this);
        _currentOpenPanelType = panelType;
    }

    private void ClosePanelInternal(MainPanelType panelType)
    {
        _panelDict[panelType].OnClose();
    }

    public void CloseAllPanels()
    {
        foreach (KeyValuePair<MainPanelType, MainCanvasPanelBase> kvp in _panelDict)
            ClosePanelInternal(kvp.Key);
    }

    public MainPanelType GetCurrentOpenPanelType()
    {
        return _currentOpenPanelType ?? default;
    }

    public bool IsPanelOpen(MainPanelType panelType)
    {
        return _currentOpenPanelType == panelType;
    }
}
