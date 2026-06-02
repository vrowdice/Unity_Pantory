using System;
using UnityEngine;

[Serializable]
public struct TutorialStep
{
    public TutorialSceneStepType kind;
    public string buildingId;
    public GameObject focusGameObject;
    public string focusObjectName;

    public bool RequiresAction =>
        kind != TutorialSceneStepType.Message && kind != TutorialSceneStepType.Complete;
}
