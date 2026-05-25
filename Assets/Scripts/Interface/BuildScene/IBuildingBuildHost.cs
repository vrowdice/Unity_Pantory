/// <summary>
/// MainCanvas / TutorialCanvas 공통 건물 배치 UI 계약.
/// </summary>
public interface IBuildingBuildHost
{
    void SelectBuilding(BuildingData buildingData, bool isSelected);

    void DeselectBuilding();

    void RefreshBuildingPlacedCountDisplays();
}
