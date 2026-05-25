using UnityEngine;

/// <summary>
/// 튜토리얼 씬의 건설(건물 배치) 러너.
/// </summary>
public class TutorialRunner : BuildingSceneRunnerBase
{
    public const string PracticeRemovalBuildingId = "road";

    public static Vector2Int PracticeRemovalGridPosition { get; } = new Vector2Int(4, 4);

    [Header("UI")]
    [SerializeField] private TutorialCanvas _tutorialCanvas;

    public TutorialCanvas TutorialCanvas => _tutorialCanvas;
    public override IBuildSceneCanvas BuildSceneCanvas => _tutorialCanvas;

    protected override MainBuildingGridHandler CreateGridHandler()
    {
        return new TutorialBuildingGridHandler(this);
    }

    protected override MainBuildingPlacementHandler CreatePlacementHandler()
    {
        return new TutorialBuildingPlacementHandler(this);
    }

    protected override MainBlueprintHandler CreateBlueprintHandler()
    {
        return new TutorialBlueprintHandler(this);
    }

    protected override void OnGridReady()
    {
        PlacePracticeRemovalBuilding();
    }

    protected override void InitBuildSceneCanvas()
    {
        _tutorialCanvas.Init(this);
    }

    private void PlacePracticeRemovalBuilding()
    {
        BuildingData roadData = DataManager.Building.GetBuildingData(PracticeRemovalBuildingId);
        if (roadData == null)
        {
            Debug.LogWarning("[TutorialRunner] Practice removal building data not found.");
            return;
        }

        if (!GridHandler.TryPlaceRoad(roadData, PracticeRemovalGridPosition, 0, out _, out bool insufficientCredits))
        {
            if (insufficientCredits)
                Debug.LogWarning("[TutorialRunner] Not enough credits to place practice removal building.");
            else
                Debug.LogWarning("[TutorialRunner] Failed to place practice removal building.");
        }
    }

    public bool IsPracticeRemovalTargetPresent()
    {
        return GridHandler.IsCellOccupied(PracticeRemovalGridPosition);
    }
}
