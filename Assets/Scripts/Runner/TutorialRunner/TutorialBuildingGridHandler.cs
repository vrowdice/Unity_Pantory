/// <summary>
/// 튜토리얼 씬 그리드 핸들러.
/// </summary>
public class TutorialBuildingGridHandler : MainBuildingGridHandler
{
    protected override bool AllowRawBuildingPlacement => true;

    public TutorialBuildingGridHandler(BuildingSceneRunnerBase runner) : base(runner)
    {

    }
}
