using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public partial class MainCanvas : CanvasBase
{
    [Header("Information")]
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private TextMeshProUGUI _deltaCreditText;
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;

    [SerializeField] private DateTopInfoPanel _infoDatePanel;
    [SerializeField] private TopInfoPanel _topInfoPanel;
    [SerializeField] private NewsFlowContainer _newsFlowContainer;
    [SerializeField] private MainCanvasMainEventBtns _mainEventBtns;
    [SerializeField] private RectTransform _creditTopInfoToggleRect;

    private MainRunner _mainRunner;

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (UIManager != null && UIManager.HasAnyOpenCloseablePanel())
        {
            return;
        }

        HandlePanelShortcutKeys();
        UpdateMobileUi();
    }

    public void Init(MainRunner mainRunner)
    {
        base.Init();

        _mainRunner = mainRunner;

        DataManager.Resource.OnResourceChanged -= OnResourceChanged;
        DataManager.Finances.OnCreditChanged -= UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;

        DataManager.Time.OnDayChanged -= OnDayChanged;
        DataManager.Time.OnMonthChanged -= OnMonthChanged;
        DataManager.Time.OnYearChanged -= OnYearChanged;

        DataManager.Resource.OnResourceChanged += OnResourceChanged;
        DataManager.Finances.OnCreditChanged += UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged += UpdateAllMainText;

        DataManager.MainEvent.OnMainEventTypeChanged += OnMainEventTypeChanged;

        DataManager.Time.OnDayChanged += OnDayChanged;
        DataManager.Time.OnMonthChanged += OnMonthChanged;
        DataManager.Time.OnYearChanged += OnYearChanged;

        _infoDatePanel.Init();
        _topInfoPanel.Init();
        _newsFlowContainer.Init();

        if (_mainEventBtns != null && DataManager?.MainEvent != null)
        {
            _mainEventBtns.SetMainEventContainer(DataManager.MainEvent.CurrentEventType, this);
        }

        RectTransform creditToggleRect = _creditTopInfoToggleRect != null
            ? _creditTopInfoToggleRect
            : _creditText != null ? _creditText.rectTransform : null;
        if (creditToggleRect != null)
        {
            UIManager.SetCreditTopInfoToggleRect(creditToggleRect);
        }

        TimePlayPanel timePlayPanel = GetComponentInChildren<TimePlayPanel>(true);
        timePlayPanel?.Init(DataManager, GameManager);

        CreateMainPanels();
        InitializePanelDictionary();
        InitializePanels();
        CreateQuickMoveBtns();

        InitBuildUi();
        InitMobileUi();

        _mainRunner.GridHandler.OnBuildingInstanceLayoutChanged += RefreshBuildingPlacedCountDisplays;

        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    private void OnDestroy()
    {
        if (_mainRunner != null && _mainRunner.GridHandler != null)
            _mainRunner.GridHandler.OnBuildingInstanceLayoutChanged -= RefreshBuildingPlacedCountDisplays;

        if (DataManager != null)
        {
            DataManager.Resource.OnResourceChanged -= OnResourceChanged;
            DataManager.Finances.OnCreditChanged -= UpdateAllMainText;
            DataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;
            DataManager.MainEvent.OnMainEventTypeChanged -= OnMainEventTypeChanged;
            DataManager.Time.OnDayChanged -= OnDayChanged;
            DataManager.Time.OnMonthChanged -= OnMonthChanged;
            DataManager.Time.OnYearChanged -= OnYearChanged;
        }
    }

    override public void UpdateAllMainText()
    {
        UpdateCreditText();
        UpdateResearchText();
        RefreshBuildingPlacedCountDisplays();
    }

    private void OnResourceChanged()
    {
        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    private void OnMainEventTypeChanged(MainEventType mainEventType)
    {
        _mainEventBtns.SetMainEventContainer(mainEventType, this);
    }

    private void UpdateCreditText()
    {
        long resourceAmount = DataManager.Finances.Credit;
        _creditText.text = ReplaceUtils.FormatNumberWithCommas(resourceAmount);
        long deltaCredit = DataManager.Finances.DailyTotal;
        if (deltaCredit == 0)
        {
            _deltaCreditText.text = "";
            return;
        }

        string sign = deltaCredit > 0 ? " +" : " ";
        _deltaCreditText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaCredit)}";
        _deltaCreditText.color = this.VisualManager.GetDeltaColor(deltaCredit);
    }

    private void UpdateResearchText()
    {
        long researchPoints = DataManager.Research.ResearchPoint;
        _researchText.text = ReplaceUtils.FormatNumberWithCommas(researchPoints);
        long deltaResearch = DataManager.Research.CalculateDailyRPProduction();
        if (deltaResearch == 0)
        {
            _deltaResearchText.text = "";
            return;
        }

        string sign = deltaResearch > 0 ? " + " : " ";
        _deltaResearchText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaResearch)}";
        _deltaResearchText.color = this.VisualManager.GetDeltaColor(deltaResearch);
    }

    private void OnMonthChanged()
    {
        Debug.Log("[MainUiManager] Month changed event received.");
    }

    private void OnYearChanged()
    {
        Debug.Log("[MainUiManager] Year changed event received.");
    }

    private void OnDayChanged()
    {
        RefreshResourceScrollView();
        UpdateAllMainText();
        _mainEventBtns.SetMainEventContainer(DataManager.MainEvent.CurrentEventType, this);
    }

    public void ShowNewsPopup(NewsState newsState)
    {
        UIManager.ShowNewsPopup(newsState);
    }

    public void ShowMainEventAnnouncement(InitialMainEventModuleData moduleData)
    {
        UIManager.ShowMainEventAnnouncementPopup(moduleData);
    }

    public void ToggleCreditTopInfoPopup()
    {
        UIManager.Instance?.ToggleCreditTopInfoPopup();
    }

    public void ShowOptionPanel()
    {
        UIManager.ShowOptionPopup();
    }
}
