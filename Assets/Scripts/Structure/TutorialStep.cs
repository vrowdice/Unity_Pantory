using System;

[Serializable]
public struct TutorialStep
{
    public TutorialSceneStepKind kind;
    public string buildingId;
    public TutorialUiFocusTarget focusTarget;

    public bool RequiresAction =>
        kind != TutorialSceneStepKind.Message && kind != TutorialSceneStepKind.Complete;

    public TutorialStep(TutorialSceneStepKind kind, string buildingId, TutorialUiFocusTarget focusTarget)
    {
        this.kind = kind;
        this.buildingId = buildingId;
        this.focusTarget = focusTarget;
    }

    public static TutorialStep Msg(TutorialUiFocusTarget target = TutorialUiFocusTarget.None)
        => new TutorialStep(TutorialSceneStepKind.Message, null, target);

    public static TutorialStep Act(TutorialSceneStepKind kind, string buildingId = null)
        => new TutorialStep(kind, buildingId, TutorialUiFocusTarget.None);

    public static TutorialStep Complete()
        => new TutorialStep(TutorialSceneStepKind.Complete, null, TutorialUiFocusTarget.None);
}
