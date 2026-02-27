using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TutorialData
{
    public string id;
    public GameObject focusGameObject;
    public UnityEvent onStepStart;
    public Vector2 tutorialPanelPosition;
}
