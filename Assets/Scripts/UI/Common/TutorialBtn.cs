using UnityEngine;

public class TutorialBtn : MonoBehaviour
{
    [SerializeField] private TutorialBase _tutorialBase;

    public void OnClick()
    {
        _tutorialBase.StartTutorial();
    }
}
