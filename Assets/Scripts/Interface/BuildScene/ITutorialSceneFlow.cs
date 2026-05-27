/// <summary>
/// 튜토리얼 씬 진행 알림 계약. TutorialCanvas → TutorialDirector 순환 참조를 피합니다.
/// </summary>
public interface ITutorialSceneFlow
{
    void NotifyCanvasReady(TutorialCanvas canvas);

    void NotifyBuildingLayoutChanged();

    void NotifyBuildingSelected(string buildingId);

    void NotifyPanelOpened(MainPanelType panelType);

    void NotifyRemovalModeChanged(bool isOn);

    void NotifyAutoEmployeePlacementChanged(bool isOn);

    void NotifyTimePlayStarted();

    void NotifyBuildingResourceAssigned(string buildingId);

    void NotifyMarketSellConfigured();

    void NotifyDayAdvanced();
}
