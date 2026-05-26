using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 씬의 단계별 가이드를 진행합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TutorialDirector : MonoBehaviour, ITutorialSceneFlow
{
    public static TutorialDirector Instance { get; private set; }

    private const string LocalizationOwnerName = "TutorialScene";
    private const int MinRoadTilesToAdvanceRoadStep = 1;

    [SerializeField] private BuildingSceneRunnerBase _tutorialRunner;
    [SerializeField] private Vector2 _defaultPanelPosition = new Vector2(0f, -220f);

    private TutorialCanvas _tutorialCanvas;
    private int _currentStepIndex;
    private int _placementBaselineCount;
    private bool _practiceRemovalTargetWasPresent;
    private bool _isWaitingForAction;
    private bool _isPopupVisible;
    private bool _hasStarted;
    private TutorialGuidedPopup _activePopup;

    private readonly List<TutorialSceneStepDefinition> _steps = new List<TutorialSceneStepDefinition>
    {
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.EnableRemovalMode),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.RemovePracticeBuilding),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.EnableAutoEmployeePlacement),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.SelectBuilding, "logging_camp"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.PlaceBuilding, "logging_camp"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.SelectBuilding, "unload"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.PlaceBuilding, "unload"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.SelectBuilding, "road"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.PlaceBuilding, "road"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.SelectBuilding, "sawmill"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.PlaceBuilding, "sawmill"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.SelectBuilding, "road"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.PlaceBuilding, "road"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.SelectBuilding, "load"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.PlaceBuilding, "load"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.AssignBuildingResource, "unload"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.AssignBuildingResource, "sawmill"),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.StartTimePlay),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.OpenMarketPanel),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.AdjustMarketSell),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Message),
        new TutorialSceneStepDefinition(TutorialSceneStepKind.Complete)
    };

    private void Awake()
    {
        Instance = this;

        if (_tutorialRunner == null)
            _tutorialRunner = FindAnyObjectByType<TutorialRunner>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NotifyCanvasReady(TutorialCanvas canvas)
    {
        if (_hasStarted)
            return;

        _tutorialCanvas = canvas;

        if (_tutorialRunner == null)
            _tutorialRunner = FindAnyObjectByType<TutorialRunner>();

        StartCoroutine(StartTutorialAfterSceneReady());
    }

    private IEnumerator StartTutorialAfterSceneReady()
    {
        yield return null;

        if (_hasStarted)
            yield break;

        _hasStarted = true;
        BeginStep(0);
    }

    public void NotifyBuildingSelected(string buildingId)
    {
        if (!_isWaitingForAction)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind != TutorialSceneStepKind.SelectBuilding)
            return;

        if (step.BuildingId == buildingId)
            AdvanceAfterAction();
    }

    public void NotifyBuildingLayoutChanged()
    {
        if (!_isWaitingForAction || _tutorialRunner == null)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind == TutorialSceneStepKind.PlaceBuilding)
        {
            if (IsPlacementGoalMet(step))
                AdvanceAfterAction();
            return;
        }

        if (step.Kind == TutorialSceneStepKind.RemovePracticeBuilding)
        {
            if (_tutorialRunner is TutorialRunner tutorialRunner &&
                _practiceRemovalTargetWasPresent &&
                !tutorialRunner.IsPracticeRemovalTargetPresent())
                AdvanceAfterAction();
        }
    }

    public void NotifyRemovalModeChanged(bool isOn)
    {
        if (!_isWaitingForAction || !isOn)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind == TutorialSceneStepKind.EnableRemovalMode)
            AdvanceAfterAction();
    }

    public void NotifyAutoEmployeePlacementChanged(bool isOn)
    {
        if (!_isWaitingForAction || !isOn)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind == TutorialSceneStepKind.EnableAutoEmployeePlacement)
            AdvanceAfterAction();
    }

    public void NotifyTimePlayStarted()
    {
        if (!_isWaitingForAction)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind == TutorialSceneStepKind.StartTimePlay)
            AdvanceAfterAction();
    }

    public void NotifyBuildingResourceAssigned(string buildingId)
    {
        if (!_isWaitingForAction || string.IsNullOrEmpty(buildingId))
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind != TutorialSceneStepKind.AssignBuildingResource)
            return;

        if (step.BuildingId == buildingId)
            AdvanceAfterAction();
    }

    public void NotifyPanelOpened(MainPanelType panelType)
    {
        if (!_isWaitingForAction)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind == TutorialSceneStepKind.OpenMarketPanel && panelType == MainPanelType.Market)
            AdvanceAfterAction();
    }

    public void NotifyMarketSellConfigured()
    {
        if (!_isWaitingForAction)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind == TutorialSceneStepKind.AdjustMarketSell)
            AdvanceAfterAction();
    }

    private void BeginStep(int stepIndex)
    {
        _currentStepIndex = stepIndex;
        _isWaitingForAction = false;

        if (_currentStepIndex >= _steps.Count)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        ApplyInputGate(step);

        if (step.Kind == TutorialSceneStepKind.PlaceBuilding && _tutorialRunner != null)
            _placementBaselineCount = GetPlacementCount(step.BuildingId);

        if (step.Kind == TutorialSceneStepKind.RemovePracticeBuilding && _tutorialRunner is TutorialRunner tutorialRunner)
            _practiceRemovalTargetWasPresent = tutorialRunner.IsPracticeRemovalTargetPresent();

        if (step.Kind == TutorialSceneStepKind.EnableAutoEmployeePlacement)
            _tutorialCanvas?.PrepareAutoEmployeePlacementStep();

        if (step.Kind == TutorialSceneStepKind.AdjustMarketSell)
            _tutorialCanvas?.PrepareMarketSellStep();

        if (step.Kind == TutorialSceneStepKind.SelectBuilding)
            _tutorialCanvas?.SelectBuildingTypeForBuilding(step.BuildingId);
        else if (step.Kind == TutorialSceneStepKind.PlaceBuilding)
            _tutorialCanvas?.SelectBuildingTypeForBuilding(step.BuildingId);

        if (step.RequiresAction)
            _isWaitingForAction = true;

        ShowStepPopup(step);
        TryEvaluatePendingAction();
    }

    private void ApplyInputGate(TutorialSceneStepDefinition step)
    {
        switch (step.Kind)
        {
            case TutorialSceneStepKind.SelectBuilding:
                TutorialInputGate.Configure(true, new[] { step.BuildingId });
                break;
            case TutorialSceneStepKind.PlaceBuilding:
                TutorialInputGate.Configure(true, new[] { step.BuildingId });
                break;
            case TutorialSceneStepKind.OpenMarketPanel:
                TutorialInputGate.Configure(true, allowedPanels: new[] { MainPanelType.Market });
                break;
            case TutorialSceneStepKind.AdjustMarketSell:
                TutorialInputGate.Configure(true, allowedPanels: new[] { MainPanelType.Market });
                break;
            case TutorialSceneStepKind.EnableRemovalMode:
            case TutorialSceneStepKind.RemovePracticeBuilding:
                TutorialInputGate.Configure(true);
                break;
            case TutorialSceneStepKind.EnableAutoEmployeePlacement:
                TutorialInputGate.Configure(true, allowAutoEmployeeToggle: true);
                break;
            case TutorialSceneStepKind.StartTimePlay:
                TutorialInputGate.Configure(true, allowTimePlay: true);
                break;
            case TutorialSceneStepKind.AssignBuildingResource:
                TutorialInputGate.Configure(true);
                break;
            case TutorialSceneStepKind.Complete:
                TutorialInputGate.Clear();
                break;
            default:
                TutorialInputGate.Configure(false);
                break;
        }
    }

    private void ShowStepPopup(TutorialSceneStepDefinition step)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[TutorialDirector] UIManager.Instance is null. Cannot show tutorial popup.");
            return;
        }

        List<TutorialData> popupData = new List<TutorialData>
        {
            new TutorialData
            {
                focusGameObject = ResolveFocusTarget(step),
                tutorialPanelPosition = _defaultPanelPosition
            }
        };

        _isPopupVisible = true;

        TutorialGuidedPopup popup = _activePopup;
        if (popup == null)
        {
            popup = UIManager.Instance.ShowTutorialGuidedPopup(popupData, LocalizationOwnerName);
            if (popup == null)
            {
                Debug.LogError("[TutorialDirector] Failed to create TutorialGuidedPopup.");
                return;
            }

            _activePopup = popup;
        }
        else
        {
            popup.gameObject.SetActive(true);
            popup.Init(popupData, LocalizationOwnerName);
        }

        popup.SetLocalizationKey(null);
        popup.SetStepIndexOverride(_currentStepIndex);
        popup.SetAdvanceCallback(OnPopupAdvanceRequested);
        popup.SetOnDismissedUnexpectedly(OnGuidedPopupClosedUnexpectedly);
        popup.ConfigureStepPresentation(
            allowWorldInteraction: true,
            showNextButton: !step.RequiresAction);
    }

    private void TryEvaluatePendingAction()
    {
        if (!_isWaitingForAction)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];

        switch (step.Kind)
        {
            case TutorialSceneStepKind.SelectBuilding:
                BuildingData selectedBuilding = _tutorialRunner?.PlacementHandler?.SelectedBuilding;
                if (selectedBuilding != null && selectedBuilding.id == step.BuildingId)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.PlaceBuilding:
                if (IsPlacementGoalMet(step))
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.OpenMarketPanel:
                if (_tutorialCanvas != null && _tutorialCanvas.IsPanelOpen(MainPanelType.Market))
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.AdjustMarketSell:
                break;

            case TutorialSceneStepKind.EnableRemovalMode:
                if (_tutorialRunner?.PlacementHandler != null && _tutorialRunner.PlacementHandler.IsRemovalMode)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.RemovePracticeBuilding:
                if (_tutorialRunner is TutorialRunner tutorialRunner &&
                    _practiceRemovalTargetWasPresent &&
                    !tutorialRunner.IsPracticeRemovalTargetPresent())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.EnableAutoEmployeePlacement:
                if (_tutorialRunner?.PlacementHandler != null && _tutorialRunner.PlacementHandler.IsAutoEmployeePlacement)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.StartTimePlay:
                if (_tutorialCanvas != null && !IsTimePaused())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.AssignBuildingResource:
                if (_tutorialRunner?.GridHandler != null &&
                    _tutorialRunner.GridHandler.AnyPlacedBuildingHasConfiguredOutputResource(step.BuildingId))
                    AdvanceAfterAction();
                break;
        }
    }

    private bool IsTimePaused()
    {
        TimePlayPanel timePlayPanel = _tutorialCanvas != null
            ? _tutorialCanvas.GetComponentInChildren<TimePlayPanel>(true)
            : null;
        return timePlayPanel == null || timePlayPanel.IsTimePaused;
    }

    private void OnGuidedPopupClosedUnexpectedly()
    {
        if (!_hasStarted || _currentStepIndex >= _steps.Count || _activePopup != null)
            return;

        StartCoroutine(RestorePopupNextFrame());
    }

    private IEnumerator RestorePopupNextFrame()
    {
        yield return null;

        if (!_hasStarted || _currentStepIndex >= _steps.Count || _activePopup != null)
            yield break;

        _isPopupVisible = true;
        ShowStepPopup(_steps[_currentStepIndex]);
    }

    private GameObject ResolveFocusTarget(TutorialSceneStepDefinition step)
    {
        if (_currentStepIndex == RotateBtnStepIndex)
            return _tutorialCanvas?.FindRotateBtnContainer();

        if (_currentStepIndex == TimePlayPanelStepIndex)
            return _tutorialCanvas?.FindTimePlayPanel();

        if (_currentStepIndex == ResourcePanelStepIndex)
            return _tutorialCanvas?.FindResourceScrollView();

        switch (step.Kind)
        {
            case TutorialSceneStepKind.SelectBuilding:
                return _tutorialCanvas != null
                    ? _tutorialCanvas.FindBuildingButton(step.BuildingId)?.gameObject
                    : TutorialUiAnchor.Resolve($"Building_{step.BuildingId}");
            case TutorialSceneStepKind.EnableRemovalMode:
                return _tutorialCanvas != null
                    ? _tutorialCanvas.FindRemovalModeSwitch()?.gameObject
                    : TutorialUiAnchor.Resolve("RemovalMode");
            case TutorialSceneStepKind.EnableAutoEmployeePlacement:
                return _tutorialCanvas != null
                    ? _tutorialCanvas.FindAutoEmployeePlacementSwitch()?.gameObject
                    : TutorialUiAnchor.Resolve("AutoEmployeePlacement");
            case TutorialSceneStepKind.StartTimePlay:
                return _tutorialCanvas?.FindPlayPauseButton();
            case TutorialSceneStepKind.OpenMarketPanel:
                return _tutorialCanvas != null
                    ? _tutorialCanvas.FindQuickMoveButton(MainPanelType.Market)?.gameObject
                    : TutorialUiAnchor.Resolve("QuickMove_Market");
            case TutorialSceneStepKind.AdjustMarketSell:
                return _tutorialCanvas?.FindMarketSellDecreaseButton();
            default:
                if (_currentStepIndex == CreditsInfoStepIndex)
                    return _tutorialCanvas?.FindCreditTopInfoToggle();
                return null;
        }
    }

    private const int RotateBtnStepIndex = 11;
    private const int TimePlayPanelStepIndex = 30;
    private const int ResourcePanelStepIndex = 32;
    private const int CreditsInfoStepIndex = 36;

    private void OnPopupAdvanceRequested()
    {
        if (!_isPopupVisible)
            return;

        TutorialSceneStepDefinition step = _steps[_currentStepIndex];
        if (step.Kind == TutorialSceneStepKind.Complete)
        {
            CompleteTutorial();
            return;
        }

        if (step.RequiresAction)
            return;

        AdvanceStep();
    }

    private int GetPlacementCount(string buildingId)
    {
        if (_tutorialRunner == null || string.IsNullOrEmpty(buildingId))
            return 0;

        return _tutorialRunner.GridHandler.CountPlacedLayoutEntries(buildingId);
    }

    private bool IsPlacementGoalMet(TutorialSceneStepDefinition step)
    {
        if (_tutorialRunner == null || step.Kind != TutorialSceneStepKind.PlaceBuilding)
            return false;

        int currentCount = GetPlacementCount(step.BuildingId);
        if (step.BuildingId == "road")
            return currentCount >= _placementBaselineCount + MinRoadTilesToAdvanceRoadStep;

        return currentCount > _placementBaselineCount;
    }

    private void DismissActivePopup()
    {
        if (_activePopup == null)
            return;

        _activePopup.Dismiss();
        _activePopup = null;
    }

    private void AdvanceAfterAction()
    {
        _isWaitingForAction = false;
        AdvanceStep();
    }

    private void AdvanceStep()
    {
        _isPopupVisible = false;
        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Count)
        {
            CompleteTutorial();
            return;
        }

        BeginStep(_currentStepIndex);
    }

    public void ResetTutorial()
    {
        DismissActivePopup();
        TutorialInputGate.Clear();
        SaveLoadManager.Instance?.StartNewGame(DataManager.Instance);
        SceneLoadManager.Instance?.LoadScene("Tutorial");
    }

    public void CompleteTutorial()
    {
        DismissActivePopup();
        TutorialInputGate.Clear();
        DataManager.Instance?.Player?.MarkIntroTutorialCompleted();
        DataManager.Instance?.ResetToNewGame();
        SceneLoadManager.Instance?.LoadScene("Main");
    }

    private enum TutorialSceneStepKind
    {
        Message,
        EnableRemovalMode,
        RemovePracticeBuilding,
        EnableAutoEmployeePlacement,
        SelectBuilding,
        PlaceBuilding,
        AssignBuildingResource,
        StartTimePlay,
        OpenMarketPanel,
        AdjustMarketSell,
        Complete
    }

    private readonly struct TutorialSceneStepDefinition
    {
        public TutorialSceneStepKind Kind { get; }
        public string BuildingId { get; }

        public bool RequiresAction =>
            Kind == TutorialSceneStepKind.EnableRemovalMode ||
            Kind == TutorialSceneStepKind.RemovePracticeBuilding ||
            Kind == TutorialSceneStepKind.EnableAutoEmployeePlacement ||
            Kind == TutorialSceneStepKind.SelectBuilding ||
            Kind == TutorialSceneStepKind.PlaceBuilding ||
            Kind == TutorialSceneStepKind.AssignBuildingResource ||
            Kind == TutorialSceneStepKind.StartTimePlay ||
            Kind == TutorialSceneStepKind.OpenMarketPanel ||
            Kind == TutorialSceneStepKind.AdjustMarketSell;

        public TutorialSceneStepDefinition(TutorialSceneStepKind kind, string buildingId = null)
        {
            Kind = kind;
            BuildingId = buildingId;
        }
    }
}
