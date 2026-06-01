using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인스펙터 <see cref="_steps"/> 목록으로 튜토리얼 단계를 진행하며 <see cref="TutorialGuidedPopup"/>을 갱신합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }

    private const int MinRoadTilesToAdvanceRoadStep = 1;

    [SerializeField] private TutorialRunner _tutorialRunner;
    [SerializeField] private TutorialCanvas _tutorialCanvas;
    [SerializeField] private int _debugStartStepIndex = 0;
    [SerializeField] private List<TutorialStep> _steps = new List<TutorialStep>();
    [SerializeField] private Vector2 _defaultPanelPosition = new Vector2(0f, -220f);

    private int _currentStepIndex;
    private int _placementBaselineCount;
    private int _rawBuildingBaselineCount;
    private bool _practiceRemovalTargetWasPresent;
    private bool _isWaitingForAction;

    private TutorialGuidedPopup _activePopup;

    private int _waitDayBaselineYear;
    private int _waitDayBaselineMonth;
    private int _waitDayBaselineDay;

    private int StepCount => _steps != null ? _steps.Count : 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Init()
    {
        if (_steps == null || _steps.Count == 0)
        {
            Debug.LogError("[TutorialDirector] Steps list is missing or empty.");
            return;
        }

        _activePopup = UIManager.Instance?.ShowTutorialGuidedPopup();
        if (_activePopup == null)
        {
            Debug.LogError("[TutorialDirector] Failed to create TutorialGuidedPopup.");
            return;
        }

        BeginStep(_debugStartStepIndex);
    }

    public void NotifyBuildingSelected(string buildingId)
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepType.SelectBuilding)
            return;

        if (step.buildingId == buildingId)
            AdvanceAfterAction();
    }

    public void NotifyRawBuildingAdded(string buildingId)
    {
        if (!_isWaitingForAction || _tutorialRunner == null || string.IsNullOrEmpty(buildingId))
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepType.AddRawBuilding)
            return;

        if (step.buildingId != buildingId)
            return;

        if (IsRawBuildingGoalMet(step))
            AdvanceAfterAction();
    }

    public void NotifyBuildingLayoutChanged()
    {
        if (!_isWaitingForAction || _tutorialRunner == null)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepType.PlaceBuilding)
        {
            if (IsPlacementGoalMet(step))
                AdvanceAfterAction();
            return;
        }

        if (step.kind == TutorialSceneStepType.RemovePracticeBuilding &&
            _practiceRemovalTargetWasPresent &&
            !_tutorialRunner.IsPracticeRemovalTargetPresent())
        {
            AdvanceAfterAction();
        }
    }

    public void NotifyRemovalModeChanged(bool isOn)
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepType.EnableRemovalMode && isOn)
            AdvanceAfterAction();
        if (step.kind == TutorialSceneStepType.DisableRemovalMode && !isOn)
            AdvanceAfterAction();
    }

    public void NotifyAutoEmployeePlacementChanged(bool isOn)
    {
        if (!_isWaitingForAction || !isOn)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepType.EnableAutoEmployeePlacement)
            AdvanceAfterAction();
    }

    public void NotifyTimePlayStarted()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepType.StartTimePlay)
            AdvanceAfterAction();
    }

    public void NotifyBuildingResourceAssigned(string buildingId)
    {
        if (!_isWaitingForAction || string.IsNullOrEmpty(buildingId))
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepType.AssignBuildingResource)
            return;

        if (step.buildingId == buildingId)
            AdvanceAfterAction();
    }

    public void NotifyPanelOpened(MainPanelType panelType)
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepType.OpenMarketPanel && panelType == MainPanelType.Market)
            AdvanceAfterAction();
    }

    public void NotifyPanelClosed()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepType.ClosePanel)
            return;

        AdvanceAfterAction();
    }

    public void NotifyMarketSellConfigured()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepType.AdjustMarketSell)
            AdvanceAfterAction();
    }

    public void NotifyDayAdvanced()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind != TutorialSceneStepType.WaitDayPassed)
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

        TutorialStep step = _steps[_currentStepIndex];

        if (step.kind == TutorialSceneStepType.PlaceBuilding && _tutorialRunner != null)
            _placementBaselineCount = GetPlacementCount(step.buildingId);

        if (step.kind == TutorialSceneStepType.AddRawBuilding && _tutorialRunner != null)
            _rawBuildingBaselineCount = GetRawBuildingCount(step.buildingId);

        if (step.kind == TutorialSceneStepType.RemovePracticeBuilding && _tutorialRunner != null)
            _practiceRemovalTargetWasPresent = _tutorialRunner.IsPracticeRemovalTargetPresent();

        if (step.kind == TutorialSceneStepType.EnableAutoEmployeePlacement)
            _tutorialCanvas?.SetAutoEmployeePlacementForTutorial(false);

        if (step.kind == TutorialSceneStepType.AdjustMarketSell)
            _tutorialCanvas?.PrepareMarketSellStep();

        if (step.kind == TutorialSceneStepType.WaitDayPassed)
            CaptureWaitDayBaseline();

        if (step.kind == TutorialSceneStepType.SelectBuilding || step.kind == TutorialSceneStepType.PlaceBuilding)
            _tutorialCanvas?.SelectBuildingTypeForBuilding(step.buildingId);

        if (step.RequiresAction)
            _isWaitingForAction = true;

        RefreshStepPopup(step);
        TryEvaluatePendingAction();
    }

    private void RefreshStepPopup(TutorialStep step)
    {
        if (_activePopup == null || _activePopup.IsRetiring)
            return;

        _activePopup.ApplyStep(
            _currentStepIndex,
            ResolveFocusObject(step),
            _defaultPanelPosition,
            showNextButton: !step.RequiresAction,
            step.focusObjectName);
        _activePopup.SetAdvanceCallback(OnPopupAdvanceRequested);
    }

    private void TryEvaluatePendingAction()
    {
        if (!_isWaitingForAction)
            return;

        TutorialStep step = _steps[_currentStepIndex];

        switch (step.kind)
        {
            case TutorialSceneStepType.SelectBuilding:
                BuildingData selectedBuilding = _tutorialRunner?.PlacementHandler?.SelectedBuilding;
                if (selectedBuilding != null && selectedBuilding.id == step.buildingId)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.PlaceBuilding:
                if (IsPlacementGoalMet(step))
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.AddRawBuilding:
                if (IsRawBuildingGoalMet(step))
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.OpenMarketPanel:
                if (_tutorialCanvas != null && _tutorialCanvas.IsPanelOpen(MainPanelType.Market))
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.ClosePanel:
                if (_tutorialCanvas != null && !_tutorialCanvas.IsAnyPanelOpen())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.EnableRemovalMode:
                if (_tutorialRunner?.PlacementHandler != null && _tutorialRunner.PlacementHandler.IsRemovalMode)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.RemovePracticeBuilding:
                if (_practiceRemovalTargetWasPresent && !_tutorialRunner.IsPracticeRemovalTargetPresent())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.EnableAutoEmployeePlacement:
                if (_tutorialRunner.PlacementHandler.IsAutoEmployeePlacement)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.StartTimePlay:
                if (_tutorialCanvas != null && !_tutorialCanvas.IsTutorialTimePaused())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.WaitDayPassed:
                if (HasAtLeastOneDayPassedSinceWaitStepBegan())
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepType.AssignBuildingResource:
                if (_tutorialRunner?.GridHandler != null &&
                    _tutorialRunner.GridHandler.AnyPlacedBuildingHasConfiguredOutputResource(step.buildingId))
                {
                    AdvanceAfterAction();
                }
                break;
        }
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

    private GameObject ResolveFocusObject(TutorialStep step)
    {
        if (step.focusGameObject != null)
            return step.focusGameObject;

        if (string.IsNullOrWhiteSpace(step.focusObjectName))
            return null;

        return TutorialFocusResolver.FindFocusObject(step.focusObjectName, _tutorialCanvas != null ? _tutorialCanvas.gameObject : null);
    }

    private void OnPopupAdvanceRequested()
    {
        TutorialStep step = _steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepType.Complete)
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
        if (_tutorialRunner == null || step.kind != TutorialSceneStepType.PlaceBuilding)
            return false;

        int currentCount = GetPlacementCount(step.buildingId);
        if (step.buildingId == "road")
            return currentCount >= _placementBaselineCount + MinRoadTilesToAdvanceRoadStep;

        return currentCount > _placementBaselineCount;
    }

    private int GetRawBuildingCount(string buildingId)
    {
        if (_tutorialRunner == null || string.IsNullOrEmpty(buildingId))
            return 0;

        return _tutorialRunner.RawBuildingHandler.CountPlacedBuildingsWithId(buildingId);
    }

    private bool IsRawBuildingGoalMet(TutorialStep step)
    {
        if (_tutorialRunner == null || step.kind != TutorialSceneStepType.AddRawBuilding)
            return false;

        return GetRawBuildingCount(step.buildingId) > _rawBuildingBaselineCount;
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
        SceneLoadManager.Instance?.LoadScene("Tutorial");
    }

    public void CompleteTutorial()
    {
        DismissActivePopup();
        SaveLoadManager.Instance?.MarkIntroTutorialCompleted();
        DataManager.Instance?.ResetToNewGame();
        SceneLoadManager.Instance?.LoadScene("Main");
    }
}
