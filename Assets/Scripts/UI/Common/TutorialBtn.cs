using UnityEngine;

public class TutorialBtn : BtnBase
{
    [SerializeField] private TutorialBase _tutorialBase;

    protected override void HandleClick()
    {
        _tutorialBase.StartTutorial();
    }
}
