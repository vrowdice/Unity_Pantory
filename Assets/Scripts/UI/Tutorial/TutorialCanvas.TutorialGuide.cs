using Evo.UI;
using UnityEngine;

/// <summary>
/// 튜토리얼 Director 연동용 MainCanvas 확장.
/// </summary>
public partial class TutorialCanvas
{
    private TimePlayPanel _tutorialTimePlayPanel;
    private Switch _tutorialRemovalModeSwitch;
    private Switch _tutorialAutoEmployeeSwitch;
    private void BindTutorialGuideEvents()
    {
        _tutorialTimePlayPanel = GetComponentInChildren<TimePlayPanel>(true);
        if (_tutorialTimePlayPanel != null)
            _tutorialTimePlayPanel.OnTimePlayStarted += HandleTutorialTimePlayStarted;

        Switch[] switches = GetComponentsInChildren<Switch>(true);
        for (int i = 0; i < switches.Length; i++)
        {
            Switch sw = switches[i];
            string name = sw.gameObject.name;
            if (_tutorialRemovalModeSwitch == null && name.Contains("Removal"))
                _tutorialRemovalModeSwitch = sw;
            if (_tutorialAutoEmployeeSwitch == null && name.Contains("AutoEmployee"))
                _tutorialAutoEmployeeSwitch = sw;
        }
    }

    private void UnbindTutorialGuideEvents()
    {
        if (_tutorialTimePlayPanel != null)
            _tutorialTimePlayPanel.OnTimePlayStarted -= HandleTutorialTimePlayStarted;
    }

    private void HandleTutorialTimePlayStarted()
    {
        TutorialDirector.Instance?.NotifyTimePlayStarted();
    }

    private void HandleTutorialDayChanged()
    {
        TutorialDirector.Instance?.NotifyDayAdvanced();
    }

    private void HandleTutorialBuildingLayoutChanged()
    {
        TutorialDirector.Instance?.NotifyBuildingLayoutChanged();
    }

    public void SetAutoEmployeePlacementForTutorial(bool isOn)
    {
        if (_mainRunner == null || _mainRunner.PlacementHandler == null)
            return;

        if (_tutorialAutoEmployeeSwitch != null)
            _tutorialAutoEmployeeSwitch.SetValue(isOn);

        _mainRunner.PlacementHandler.ToggleAutoEmployeePlacement(isOn);
        TutorialDirector.Instance?.NotifyAutoEmployeePlacementChanged(isOn);
    }

    public void SelectBuildingTypeForBuilding(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return;

        BuildingData buildingData = DataManager.Building.GetBuildingData(buildingId);
        if (buildingData == null)
            return;

        SelectBuildingType(buildingData.buildingType);
    }

    public void PrepareMarketSellStep()
    {
        if (!IsPanelOpen(MainPanelType.Market))
            OpenPanel(MainPanelType.Market);

        MarketCanvas marketCanvas = FindAnyObjectByType<MarketCanvas>(FindObjectsInactive.Include);
        marketCanvas?.SelectResourceById("premium_wood");
    }

    public bool IsTutorialTimePaused()
    {
        return _tutorialTimePlayPanel == null || _tutorialTimePlayPanel.IsTimePaused;
    }
}
