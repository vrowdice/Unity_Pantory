using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 씬의 단계별 가이드를 진행합니다. (Incremental Refresh 패턴 적용)
/// </summary>
[DefaultExecutionOrder(-100)]
public class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }

    private const int MinRoadTilesToAdvanceRoadStep = 1;

    [SerializeField] private TutorialRunner _tutorialRunner;
    [SerializeField] private TutorialCanvas _tutorialCanvas;
    [SerializeField] private TutorialSequenceData _tutorialSequence;
    [SerializeField] private Vector2 _defaultPanelPosition = new Vector2(0f, -220f);

    private int _currentStepIndex;
    private int _placementBaselineCount;
    private bool _practiceRemovalTargetWasPresent;
    private bool _isWaitingForAction;

    private TutorialGuidedPopup _activePopup;

    private int _waitDayBaselineYear;
    private int _waitDayBaselineMonth;
    private int _waitDayBaselineDay;

    private int StepCount => _tutorialSequence != null ? _tutorialSequence.steps.Count : 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BeginStep(0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NotifyBuildingSelected(string buildingId)
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepKind.SelectBuilding)
            return;

        if (step.buildingId == buildingId)
            AdvanceAfterAction();
    }

    public void NotifyBuildingLayoutChanged()
    {
        if (!_isWaitingForAction || _tutorialRunner == null)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.PlaceBuilding)
        {
            if (IsPlacementGoalMet(step))
                AdvanceAfterAction();
            return;
        }

        if (step.kind == TutorialSceneStepKind.RemovePracticeBuilding)
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

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.EnableRemovalMode)
            AdvanceAfterAction();
    }

    public void NotifyTimePlayStarted()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.StartTimePlay)
            AdvanceAfterAction();
    }

    public void NotifyBuildingResourceAssigned(string buildingId)
    {
        if (!_isWaitingForAction || string.IsNullOrEmpty(buildingId))
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepKind.AssignBuildingResource)
            return;

        if (step.buildingId == buildingId)
            AdvanceAfterAction();
    }

    public void NotifyPanelOpened(MainPanelType panelType)
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.OpenMarketPanel && panelType == MainPanelType.Market)
            AdvanceAfterAction();
    }

    public void NotifyMarketSellConfigured()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.AdjustMarketSell)
            AdvanceAfterAction();
    }

    public void NotifyDayAdvanced()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepKind.WaitDayPassed)
            return;

        if (!HasAtLeastOneDayPassedSinceWaitStepBegan())
            return;

        AdvanceAfterAction();
    }

    private void BeginStep(int stepIndex)
    {
        _currentStepIndex = stepIndex;
        _isWaitingForAction = false;

        if (_currentStepIndex >= StepCount)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];

        if (step.kind == TutorialSceneStepKind.PlaceBuilding && _tutorialRunner != null)
            _placementBaselineCount = GetPlacementCount(step.buildingId);

        if (step.kind == TutorialSceneStepKind.RemovePracticeBuilding && _tutorialRunner is TutorialRunner tutorialRunner)
            _practiceRemovalTargetWasPresent = tutorialRunner.IsPracticeRemovalTargetPresent();

        if (step.kind == TutorialSceneStepKind.EnableAutoEmployeePlacement)
            _tutorialCanvas?.SyncAutoEmployeePlacementSwitch(false);

        if (step.kind == TutorialSceneStepKind.AdjustMarketSell)
            _tutorialCanvas?.PrepareMarketSellStep();

        if (step.kind == TutorialSceneStepKind.WaitDayPassed)
            CaptureWaitDayBaseline();

        if (step.kind == TutorialSceneStepKind.SelectBuilding)
            _tutorialCanvas?.SelectBuildingTypeForBuilding(step.buildingId);
        else if (step.kind == TutorialSceneStepKind.PlaceBuilding)
            _tutorialCanvas?.SelectBuildingTypeForBuilding(step.buildingId);

        if (step.RequiresAction)
            _isWaitingForAction = true;

        ShowStepPopup(step);
        TryEvaluatePendingAction();
    }

    private void ShowStepPopup(TutorialStep step)
    {
        List<TutorialData> popupData = new List<TutorialData>
        {
            new TutorialData
            {
                focusGameObject = ResolveFocusTarget(step),
                tutorialPanelPosition = _defaultPanelPosition
            }
        };

        if (_activePopup == null || _activePopup.IsRetiring)
        {
            _activePopup = UIManager.Instance.ShowTutorialGuidedPopup(popupData);
        }
        else
        {
            _activePopup.gameObject.SetActive(true);
            _activePopup.Init(popupData);
        }

        _activePopup.SetLocalizationKey(null);
        _activePopup.SetStepIndexOverride(_currentStepIndex);
        _activePopup.SetAdvanceCallback(OnPopupAdvanceRequested);
        _activePopup.ConfigureStepPresentation(
            allowWorldInteraction: true,
            showNextButton: !step.RequiresAction);
    }

    private void TryEvaluatePendingAction()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];

        switch (step.kind)
        {
            case TutorialSceneStepKind.SelectBuilding:
                BuildingData selectedBuilding = _tutorialRunner?.PlacementHandler?.SelectedBuilding;
                if (selectedBuilding != null && selectedBuilding.id == step.buildingId)
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
                if (_tutorialRunner.PlacementHandler.IsAutoEmployeePlacement)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.StartTimePlay:
                if (_tutorialCanvas != null && !IsTimePaused())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.WaitDayPassed:
                if (HasAtLeastOneDayPassedSinceWaitStepBegan())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.AssignBuildingResource:
                if (_tutorialRunner?.GridHandler != null &&
                    _tutorialRunner.GridHandler.AnyPlacedBuildingHasConfiguredOutputResource(step.buildingId))
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

    private void CaptureWaitDayBaseline()
    {
        TimeDataHandler time = DataManager.Instance?.Time;
        if (time == null)
            return;

        _waitDayBaselineYear = time.Year;
        _waitDayBaselineMonth = time.Month;
        _waitDayBaselineDay = time.Day;
    }

    private bool HasAtLeastOneDayPassedSinceWaitStepBegan()
    {
        TimeDataHandler time = DataManager.Instance?.Time;
        if (time == null)
            return false;

        return time.Year != _waitDayBaselineYear
            || time.Month != _waitDayBaselineMonth
            || time.Day != _waitDayBaselineDay;
    }

    private GameObject ResolveFocusTarget(TutorialStep step)
    {
        switch (step.focusTarget)
        {
            case TutorialUiFocusTarget.RotateButton:
                return _tutorialCanvas?.RotateBtnContainer;
            case TutorialUiFocusTarget.TimePlayPanel:
                return _tutorialCanvas?.TimePlayPanel;
            case TutorialUiFocusTarget.ResourcePanel:
                return _tutorialCanvas?.ResourceScrollView;
            case TutorialUiFocusTarget.CreditsInfo:
                return _tutorialCanvas?.CreditTopInfoToggle;
        }

        switch (step.kind)
        {
            case TutorialSceneStepKind.SelectBuilding:
                return _tutorialCanvas.FindBuildingButton(step.buildingId)?.gameObject;
            case TutorialSceneStepKind.EnableRemovalMode:
                return _tutorialCanvas.RemovalModeSwitch?.gameObject;
            case TutorialSceneStepKind.EnableAutoEmployeePlacement:
                return _tutorialCanvas.AutoEmployeePlacementSwitch?.gameObject;
            case TutorialSceneStepKind.StartTimePlay:
                return _tutorialCanvas?.PlayPauseButton;
            case TutorialSceneStepKind.WaitDayPassed:
                return _tutorialCanvas?.TimePlayPanel;
            case TutorialSceneStepKind.OpenMarketPanel:
                return _tutorialCanvas.FindQuickMoveButton(MainPanelType.Market)?.gameObject;
            case TutorialSceneStepKind.AdjustMarketSell:
                return _tutorialCanvas?.FindMarketSellDecreaseButton();
            default:
                return null;
        }
    }

    private void OnPopupAdvanceRequested()
    {
        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.Complete)
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

    private bool IsPlacementGoalMet(TutorialStep step)
    {
        if (_tutorialRunner == null || step.kind != TutorialSceneStepKind.PlaceBuilding)
            return false;

        int currentCount = GetPlacementCount(step.buildingId);
        if (step.buildingId == "road")
            return currentCount >= _placementBaselineCount + MinRoadTilesToAdvanceRoadStep;

        return currentCount > _placementBaselineCount;
    }

    private void DismissActivePopup()
    {
        if (_activePopup != null)
        {
            _activePopup.Dismiss();
            _activePopup = null;
        }
    }

    private void AdvanceAfterAction()
    {
        _isWaitingForAction = false;
        AdvanceStep();
    }

    private void AdvanceStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= StepCount)
        {
            CompleteTutorial();
            return;
        }

        BeginStep(_currentStepIndex);
    }

    public void ResetTutorial()
    {
        DismissActivePopup();
        SaveLoadManager.Instance?.StartNewGame(DataManager.Instance);
        SceneLoadManager.Instance?.LoadScene("Tutorial");
    }

    public void CompleteTutorial()
    {
        DismissActivePopup();
        DataManager.Instance?.Player?.MarkIntroTutorialCompleted();
        DataManager.Instance?.ResetToNewGame();
        SceneLoadManager.Instance?.LoadScene("Main");
    }

    public void NotifyAutoEmployeePlacementChanged(bool isOn)
    {
        if (!_isWaitingForAction || !isOn) return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.EnableAutoEmployeePlacement)
            AdvanceAfterAction();
    }
}