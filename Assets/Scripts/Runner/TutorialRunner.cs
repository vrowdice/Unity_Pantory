using UnityEngine;

/// <summary>
/// 튜토리얼 씬의 건설(건물 배치) 러너.
/// </summary>
public class TutorialRunner : MainRunner
{
    public const string PracticeRemovalBuildingId = "road";

    public static Vector2Int PracticeRemovalGridPosition { get; } = new Vector2Int(4, 4);

    [Header("UI")]
    [SerializeField] private TutorialCanvas _tutorialCanvas;

    public override bool AllowRawBuildingPlacement => true;

    protected override void OnGridReady()
    {
        DataManager.PlacedLayout.Consume(out _, out _);
        DataManager?.BlueprintLayout?.Clear();

        PlacePracticeRemovalBuilding();
    }

    protected override void InitBuildSceneCanvas()
    {
        _tutorialCanvas.Init(this);
    }

    private void PlacePracticeRemovalBuilding()
    {
        if (!TryPlaceRoadAt(
                PracticeRemovalBuildingId,
                PracticeRemovalGridPosition,
                0,
                out GameObject placedRoad,
                out bool insufficientCredits))
        {
            if (insufficientCredits)
                Debug.LogWarning("[TutorialRunner] Not enough credits to place practice removal building.");
            else
                Debug.LogWarning("[TutorialRunner] Failed to place practice removal building.");
            return;
        }

        if (placedRoad == null)
            Debug.LogWarning("[TutorialRunner] Road placed flag was true but GameObject is null.");
    }

    public bool IsPracticeRemovalTargetPresent()
    {
        return IsGridCellOccupied(PracticeRemovalGridPosition);
    }

    public GameObject GetPlacedObjectAtGrid(Vector2Int gridPosition)
    {
        return GetPlacedObjectAt(gridPosition);
    }
}
