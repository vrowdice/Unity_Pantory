using UnityEngine;

/// <summary>
/// 도로·스플리터·터널 등 운송 타일 데이터.
/// </summary>
[CreateAssetMenu(fileName = "NewRoadBuilding", menuName = "Game Data/Building Data/Road")]
public class RoadBuildingData : BuildingData
{
    [Tooltip("true면 인접 4방향으로 차선별 전달(tunnel). false면 outputOffset(·secondary)만.")]
    public bool passThroughNeighbors;

    [Tooltip("true면 secondaryOutputOffset으로 두 번째 출구(splitter).")]
    public bool hasSecondaryOutput;

    [Tooltip("두 번째 이웃 그리드 칸(회전 전 로컬). splitter 기본 (0,1).")]
    public Vector2Int secondaryOutputOffset = new Vector2Int(0, 1);

    public override bool IsRoad => true;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (hasSecondaryOutput && IsInsideFootprint(secondaryOutputOffset, size))
            secondaryOutputOffset = new Vector2Int(0, 1);
    }

    private static bool IsInsideFootprint(Vector2Int localCell, Vector2Int footprintSize) =>
        localCell.x >= 0 && localCell.x < footprintSize.x
        && localCell.y >= 0 && localCell.y < footprintSize.y;
}
