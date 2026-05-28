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
    [SerializeField] private TutorialSequenceData _tutorialSequence;
    [SerializeField] private Vector2 _defaultPanelPosition = new Vector2(0f, -220f);

    private TutorialCanvas _tutorialCanvas;
    private int _currentStepIndex;
    private int _placementBaselineCount;
    private bool _practiceRemovalTargetWasPresent;
    private bool _isWaitingForAction;
    private bool _isPopupVisible;
    private bool _hasStarted;
    private TutorialGuidedPopup _activePopup;

    private int StepCount => _tutorialSequence != null ? _tutorialSequence.steps.Count : 0;

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

        if (_tutorialSequence == null || StepCount == 0)
        {
            Debug.LogError("[TutorialDirector] TutorialSequenceData is missing or empty.");
            yield break;
        }

        _hasStarted = true;
        BeginStep(0);
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

    public void NotifyAutoEmployeePlacementChanged(bool isOn)
    {
        if (!_isWaitingForAction || !isOn)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        if (step.kind == TutorialSceneStepKind.EnableAutoEmployeePlacement)
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

    private void BeginStep(int stepIndex)
    {
        _currentStepIndex = stepIndex;
        _isWaitingForAction = false;

        if (_currentStepIndex >= StepCount)
            return;

        TutorialStep step = _tutorialSequence.steps[_currentStepIndex];
        ApplyInputGate(step);

        if (step.kind == TutorialSceneStepKind.PlaceBuilding && _tutorialRunner != null)
            _placementBaselineCount = GetPlacementCount(step.buildingId);

        if (step.kind == TutorialSceneStepKind.RemovePracticeBuilding && _tutorialRunner is TutorialRunner tutorialRunner)
            _practiceRemovalTargetWasPresent = tutorialRunner.IsPracticeRemovalTargetPresent();

        if (step.kind == TutorialSceneStepKind.EnableAutoEmployeePlacement)
            _tutorialCanvas?.PrepareAutoEmployeePlacementStep();

        if (step.kind == TutorialSceneStepKind.AdjustMarketSell)
            _tutorialCanvas?.PrepareMarketSellStep();

        if (step.kind == TutorialSceneStepKind.SelectBuilding)
            _tutorialCanvas?.SelectBuildingTypeForBuilding(step.buildingId);
        else if (step.kind == TutorialSceneStepKind.PlaceBuilding)
            _tutorialCanvas?.SelectBuildingTypeForBuilding(step.buildingId);

        if (step.RequiresAction)
            _isWaitingForAction = true;

        ShowStepPopup(step);
        TryEvaluatePendingAction();
    }

    private void ApplyInputGate(TutorialStep step)
    {
        switch (step.kind)
        {
            case TutorialSceneStepKind.SelectBuilding:
                TutorialInputGate.Configure(true, new[] { step.buildingId });
                break;
            case TutorialSceneStepKind.PlaceBuilding:
                TutorialInputGate.Configure(true, new[] { step.buildingId });
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

    private void ShowStepPopup(TutorialStep step)
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
                if (_tutorialRunner?.PlacementHandler != null && _tutorialRunner.PlacementHandler.IsAutoEmployeePlacement)
                    AdvanceAfterAction();
                break;

            case TutorialSceneStepKind.StartTimePlay:
                if (_tutorialCanvas != null && !IsTimePaused())
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

    private void OnGuidedPopupClosedUnexpectedly()
    {
        if (!_hasStarted || _currentStepIndex >= StepCount || _activePopup != null)
            return;

        StartCoroutine(RestorePopupNextFrame());
    }

    private IEnumerator RestorePopupNextFrame()
    {
        yield return null;

        if (!_hasStarted || _currentStepIndex >= StepCount || _activePopup != null)
            yield break;

        _isPopupVisible = true;
        ShowStepPopup(_tutorialSequence.steps[_currentStepIndex]);
    }

    private GameObject ResolveFocusTarget(TutorialStep step)
    {
        switch (step.focusTarget)
        {
            case TutorialUiFocusTarget.RotateButton:
                return _tutorialCanvas?.FindRotateBtnContainer();
            case TutorialUiFocusTarget.TimePlayPanel:
                return _tutorialCanvas?.FindTimePlayPanel();
            case TutorialUiFocusTarget.ResourcePanel:
                return _tutorialCanvas?.FindResourceScrollView();
            case TutorialUiFocusTarget.CreditsInfo:
                return _tutorialCanvas?.FindCreditTopInfoToggle();
        }

        switch (step.kind)
        {
            case TutorialSceneStepKind.SelectBuilding:
                return _tutorialCanvas != null
                    ? _tutorialCanvas.FindBuildingButton(step.buildingId)?.gameObject
                    : TutorialUiAnchor.Resolve($"Building_{step.buildingId}");
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
                return null;
        }
    }

    private void OnPopupAdvanceRequested()
    {
        if (!_isPopupVisible)
            return;

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

        if (_currentStepIndex >= StepCount)
        {
            CompleteTutorial();
            return;
        }

        BeginStep(_currentStepIndex);
    }

    public void CompleteTutorial()
    {
        DismissActivePopup();
        TutorialInputGate.Clear();
        DataManager.Instance?.Player?.MarkIntroTutorialCompleted();
        DataManager.Instance?.ResetToNewGame();
        SceneLoadManager.Instance?.LoadScene("Main");
    }
}
