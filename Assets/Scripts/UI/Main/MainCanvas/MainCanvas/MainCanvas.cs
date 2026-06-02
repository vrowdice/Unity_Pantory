using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public partial class MainCanvas : CanvasBase
{
    [Header("Information")]
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private TextMeshProUGUI _deltaCreditText;
    [SerializeField] private AudioClip _creditGainSfx;
    [SerializeField] private AudioClip _creditLossSfx;
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;

    [SerializeField] private DateTopInfoPanel _infoDatePanel;
    [SerializeField] private TopInfoPanel _topInfoPanel;
    [SerializeField] private NewsFlowContainer _newsFlowContainer;
    [SerializeField] private MainCanvasMainEventBtns _mainEventBtns;
    [SerializeField] private RectTransform _creditTopInfoToggleRect;

    protected MainRunner _mainRunner;

    private Coroutine _resourceScrollCoroutine;
    private Coroutine _buildingTypeSpawnCoroutine;
    private Coroutine _buildingListSpawnCoroutine;
    private Coroutine _blueprintEntrySpawnCoroutine;

    private const float CreditCountDuration = 0.35f;

    private long _displayedCredit;
    private bool _creditDisplayInitialized;
    private Tween _creditCountTween;

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
    }

    public void Init(MainRunner mainRunner)
    {
        base.Init();

        _mainRunner = mainRunner;

        DataManager.Resource.OnResourceChanged -= OnResourceChanged;
        DataManager.Finances.OnCreditChanged -= UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;
        DataManager.OnResearchCompleted -= OnResearchCompleted;

        DataManager.Time.OnDayChanged -= OnDayChanged;
        DataManager.Time.OnMonthChanged -= OnMonthChanged;
        DataManager.Time.OnYearChanged -= OnYearChanged;

        DataManager.Resource.OnResourceChanged += OnResourceChanged;
        DataManager.Finances.OnCreditChanged += UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged += UpdateAllMainText;
        DataManager.OnResearchCompleted += OnResearchCompleted;

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

        UIManager.SetCreditTopInfoToggleRect(_creditTopInfoToggleRect);

        TimePlayPanel timePlayPanel = GetComponentInChildren<TimePlayPanel>(true);
        timePlayPanel?.Init(DataManager, GameManager);

        CreateMainPanels();
        InitializePanelDictionary();
        InitializePanels();
        CreateQuickMoveBtns();

        InitBuildUi();
        InitBankruptcyUi();

        _mainRunner.GridHandler.OnBuildingInstanceLayoutChanged += RefreshBuildingPlacedCountDisplays;
        DataManager.Goal?.BindSceneGrid(_mainRunner.GridHandler);

        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    protected override void OnDestroy()
    {
        StaggeredSpawnUtils.Stop(this, ref _resourceScrollCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _buildingTypeSpawnCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _buildingListSpawnCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _blueprintEntrySpawnCoroutine);
        KillCreditCountTween();
        base.OnDestroy();

        CleanupBankruptcyUi();

        if (_mainRunner != null && _mainRunner.GridHandler != null)
            _mainRunner.GridHandler.OnBuildingInstanceLayoutChanged -= RefreshBuildingPlacedCountDisplays;

        DataManager?.Goal?.UnbindSceneGrid();

        if (DataManager != null)
        {
            DataManager.Resource.OnResourceChanged -= OnResourceChanged;
            DataManager.Finances.OnCreditChanged -= UpdateAllMainText;
            DataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;
            DataManager.OnResearchCompleted -= OnResearchCompleted;
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

    private void OnResearchCompleted(string researchId)
    {
        RefreshBuildUiOnResearchCompleted();
    }

    private void OnMainEventTypeChanged(MainEventType mainEventType)
    {
        _mainEventBtns.SetMainEventContainer(mainEventType, this);
    }

    private void UpdateCreditText()
    {
        if (_creditText == null || DataManager?.Finances == null)
            return;

        long targetCredit = DataManager.Finances.Credit;
        UpdateDeltaCreditText();

        if (!_creditDisplayInitialized)
        {
            _creditDisplayInitialized = true;
            SetCreditTextImmediate(targetCredit);
            return;
        }

        long creditDelta = targetCredit - _displayedCredit;
        long dailyTotal = DataManager.Finances.DailyTotal;
        bool isDailySettlement = creditDelta != 0 && creditDelta == dailyTotal;

        if (isDailySettlement && dailyTotal < 0)
            PlayDailyCreditSfx(dailyTotal);
        if (isDailySettlement && dailyTotal > 0)
            PlayDailyCreditSfx(dailyTotal);

        if (targetCredit <= _displayedCredit)
        {
            SetCreditTextImmediate(targetCredit);
            return;
        }

        KillCreditCountTween();

        long startCredit = _displayedCredit;
        _creditCountTween = DOVirtual.Float(0f, 1f, CreditCountDuration, t =>
        {
            long currentCredit = startCredit + (long)((targetCredit - startCredit) * t);
            _displayedCredit = currentCredit;
            _creditText.text = ReplaceUtils.FormatNumberWithCommas(currentCredit);
        })
        .SetEase(Ease.OutCubic)
        .SetLink(_creditText.gameObject)
        .OnComplete(() => SetCreditTextImmediate(targetCredit));
    }

    private void SetCreditTextImmediate(long credit)
    {
        _displayedCredit = credit;
        _creditText.text = ReplaceUtils.FormatNumberWithCommas(credit);
    }

    private void KillCreditCountTween()
    {
        if (_creditCountTween == null)
            return;

        _creditCountTween.Kill();
        _creditCountTween = null;
    }

    private void PlayDailyCreditSfx(long dailyTotal)
    {
        if (dailyTotal == 0)
            return;

        AudioClip clip = dailyTotal > 0 ? _creditGainSfx : _creditLossSfx;
        if (clip == null)
            return;

        SoundManager.Instance?.PlaySFX(clip);
    }

    private void UpdateDeltaCreditText()
    {
        long deltaCredit = DataManager.Finances.DailyTotal;
        if (deltaCredit == 0)
        {
            _deltaCreditText.text = "";
            return;
        }

        string sign = deltaCredit > 0 ? " +" : " ";
        _deltaCreditText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaCredit)}";
        _deltaCreditText.color = VisualManager.GetDeltaColor(deltaCredit);
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

        if(_mainEventBtns != null)
            _mainEventBtns.SetMainEventContainer(DataManager.MainEvent.CurrentEventType, this);

        if(TutorialDirector.Instance != null)
            TutorialDirector.Instance.NotifyDayAdvanced();
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

    public void ShowGoalPopup()
    {
        UIManager?.ShowGoalPopup();
    }
}
