using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialSceneSequence", menuName = "Tutorial/Sequence")]
public class TutorialSequenceData : ScriptableObject
{
    public List<TutorialStep> steps = new List<TutorialStep>();
}
