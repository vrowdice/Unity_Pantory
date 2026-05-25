using System.Collections.Generic;

/// <summary>
/// MainCanvas / TutorialCanvas 공통 빌드·블루프린트 UI 계약.
/// </summary>
public interface IBuildSceneCanvas
{
    void SyncBlueprintAddButtonSelected(bool isBlueprintMode);

    void RequestSaveBlueprintEntry(List<PlacedBuildingSaveData> captured, List<PlacedRoadSaveData> capturedRoads);
}
