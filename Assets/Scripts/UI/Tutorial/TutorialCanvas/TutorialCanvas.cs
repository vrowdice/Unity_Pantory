using UnityEngine;
using TMPro;

public partial class TutorialCanvas : CanvasBase, IBuildSceneCanvas, IBuildScenePanelHost, IBuildingBuildHost, IBuildingTypeSelectHost
{
    [Header("Information")]
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private TextMeshProUGUI _deltaCreditText;
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;

    [SerializeField] private DateTopInfoPanel _infoDatePanel;
    [SerializeField] private TopInfoPanel _topInfoPanel;
    [SerializeField] private RectTransform _creditTopInfoToggleRect;

    [Header("Tutorial")]
    [SerializeField] private GameObject _newsFlowContainer;
    [SerializeField] private GameObject _mainEventBtns;

    protected BuildingSceneRunnerBase _sceneRunner;
    protected ITutorialSceneFlow _tutorialFlow;
    private TimePlayPanel _timePlayPanel;

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (UIManager != null && UIManager.HasAnyOpenCloseablePanel())
            return;

        HandlePanelShortcutKeys();
    }

    protected override void Start()
    {
        // TutorialDirector가 단계별 가이드를 담당합니다. TutorialBase 자동 팝업은 사용하지 않습니다.
    }

    public void Init(BuildingSceneRunnerBase sceneRunner)
    {
        base.Init();

        _sceneRunner = sceneRunner;
        _tutorialFlow = TutorialDirector.Instance;
        if (_tutorialFlow == null)
            Debug.LogError("[TutorialCanvas] TutorialDirector.Instance is null. Tutorial guidance will not start.");

        DataManager.Resource.OnResourceChanged -= OnResourceChanged;
        DataManager.Finances.OnCreditChanged -= UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;
        DataManager.Time.OnDayChanged -= OnDayChanged;

        DataManager.Resource.OnResourceChanged += OnResourceChanged;
        DataManager.Finances.OnCreditChanged += UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged += UpdateAllMainText;
        DataManager.Time.OnDayChanged += OnDayChanged;

        if (_newsFlowContainer != null)
            _newsFlowContainer.SetActive(false);
        if (_mainEventBtns != null)
            _mainEventBtns.SetActive(false);

        _infoDatePanel.Init();
        _topInfoPanel.Init();

        RectTransform creditToggleRect = _creditTopInfoToggleRect != null
            ? _creditTopInfoToggleRect
            : _creditText != null ? _creditText.rectTransform : null;
        if (creditToggleRect != null)
            UIManager.SetCreditTopInfoToggleRect(creditToggleRect);

        _timePlayPanel = GetComponentInChildren<TimePlayPanel>(true);
        _timePlayPanel?.Init(DataManager, GameManager);
        if (_timePlayPanel != null)
            _timePlayPanel.OnTimePlayStarted += HandleTimePlayStarted;

        CreateTutorialPanels();
        InitializePanelDictionary();
        InitializePanels();
        CreateQuickMoveBtns();
        InitBuildUi();

        _sceneRunner.GridHandler.OnBuildingInstanceLayoutChanged += RefreshBuildingPlacedCountDisplays;
        _sceneRunner.GridHandler.OnBuildingInstanceLayoutChanged += OnBuildingLayoutChanged;

        RefreshResourceScrollView();
        UpdateAllMainText();

        _tutorialFlow?.NotifyCanvasReady(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_sceneRunner != null && _sceneRunner.GridHandler != null)
        {
            _sceneRunner.GridHandler.OnBuildingInstanceLayoutChanged -= RefreshBuildingPlacedCountDisplays;
            _sceneRunner.GridHandler.OnBuildingInstanceLayoutChanged -= OnBuildingLayoutChanged;
        }

        if (DataManager != null)
        {
            DataManager.Resource.OnResourceChanged -= OnResourceChanged;
            DataManager.Finances.OnCreditChanged -= UpdateAllMainText;
            DataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;
            DataManager.Time.OnDayChanged -= OnDayChanged;
        }

        if (_timePlayPanel != null)
            _timePlayPanel.OnTimePlayStarted -= HandleTimePlayStarted;
    }

    public GameObject FindPlayPauseButton()
    {
        return _timePlayPanel != null && _timePlayPanel.PlayPauseButton != null
            ? _timePlayPanel.PlayPauseButton.gameObject
            : null;
    }

    public GameObject FindTimePlayPanel()
    {
        return _timePlayPanel != null ? _timePlayPanel.gameObject : null;
    }

    private void HandleTimePlayStarted()
    {
        _tutorialFlow?.NotifyTimePlayStarted();
    }

    public override void UpdateAllMainText()
    {
        UpdateCreditText();
        UpdateResearchText();
        RefreshBuildingPlacedCountDisplays();
    }

    public void ShowOptionPanel()
    {
        UIManager.ShowOptionPopup();
    }

    public void ToggleCreditTopInfoPopup()
    {
        UIManager.Instance?.ToggleCreditTopInfoPopup();
    }

    private void OnResourceChanged()
    {
        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    private void OnDayChanged()
    {
        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    private void OnBuildingLayoutChanged()
    {
        _tutorialFlow?.NotifyBuildingLayoutChanged();
    }

    private void UpdateCreditText()
    {
        if (_creditText == null)
            return;

        long resourceAmount = DataManager.Finances.Credit;
        _creditText.text = ReplaceUtils.FormatNumberWithCommas(resourceAmount);
        if (_deltaCreditText == null)
            return;

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
        if (_researchText == null)
            return;

        long researchPoints = DataManager.Research.ResearchPoint;
        _researchText.text = ReplaceUtils.FormatNumberWithCommas(researchPoints);
        if (_deltaResearchText == null)
            return;

        long deltaResearch = DataManager.Research.CalculateDailyRPProduction();
        if (deltaResearch == 0)
        {
            _deltaResearchText.text = "";
            return;
        }

        string sign = deltaResearch > 0 ? " + " : " ";
        _deltaResearchText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaResearch)}";
        _deltaResearchText.color = VisualManager.GetDeltaColor(deltaResearch);
    }
}
