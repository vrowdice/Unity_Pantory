using System.Collections.Generic;
using UnityEngine;

public partial class TutorialCanvas
{
    [Header("Tutorial Panel Prefabs")]
    [SerializeField] private GameObject _marketCanvasPrefab;

    private MarketCanvas _marketCanvas;
    private Dictionary<MainPanelType, MainCanvasPanelBase> _panelDict;
    private MainPanelType? _currentOpenPanelType;

    public GameObject CreditTopInfoToggle => _creditTopInfoToggleRect.gameObject;

    private void CreateTutorialPanels()
    {
        if (_marketCanvas != null)
            return;

        GameObject marketObj = Object.Instantiate(_marketCanvasPrefab);
        marketObj.name = _marketCanvasPrefab.name;
        _marketCanvas = marketObj.GetComponent<MarketCanvas>();
    }

    private void InitializePanelDictionary()
    {
        _panelDict = new Dictionary<MainPanelType, MainCanvasPanelBase>
        {
            { MainPanelType.Market, _marketCanvas }
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
        if (!_panelDict.TryGetValue(panelType, out MainCanvasPanelBase panel))
            return;

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
        _tutorialFlow?.NotifyPanelOpened(panelType);
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

    public void PrepareMarketSellStep()
    {
        if (_marketCanvas == null)
            return;

        if (!IsPanelOpen(MainPanelType.Market))
            OpenPanel(MainPanelType.Market);

        _marketCanvas.SelectResourceById("premium_wood");
    }

    public GameObject FindMarketSellDecreaseButton()
    {
        return _marketCanvas != null
            ? _marketCanvas.FindSellDecreaseButton()
            : null;
    }

    public GameObject FindCreditTopInfoToggle()
    {
        return _creditTopInfoToggleRect != null
            ? _creditTopInfoToggleRect.gameObject
            : _creditText != null ? _creditText.gameObject : null;
    }

    private void HandlePanelShortcutKeys()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsTypingInTextInput())
            return;

        if (Input.GetKeyDown(KeyCode.F3))
            OpenPanel(MainPanelType.Market);
    }
}
