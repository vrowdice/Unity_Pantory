using UnityEngine;
using System.Collections.Generic;

public class TutorialBase : MonoBehaviour
{
    [SerializeField] private List<TutorialData> _tutorialPanelInfo = new List<TutorialData>();

    protected virtual void Start()
    {
        TryAutoStartTutorialIfPending();
    }

    /// <summary>
    /// PlayerDataHandler 기준으로 아직 자동 표시 대기(true)인 경우에만 튜토리얼을 띄웁니다.
    /// </summary>
    protected void TryAutoStartTutorialIfPending()
    {
        if (_tutorialPanelInfo == null || _tutorialPanelInfo.Count == 0)
        {
            return;
        }

        if (!DataManager.Instance.Player.ShouldAutoStartTutorialForOwner(gameObject.name))
        {
            return;
        }

        StartTutorial();
    }

    /// <summary>
    /// Inspector에 설정된 TutorialData 목록으로 UIManager를 통해 튜토리얼 팝업을 표시합니다.
    /// </summary>
    public void StartTutorial()
    {
        if (_tutorialPanelInfo == null || _tutorialPanelInfo.Count == 0) return;
        UIManager.Instance.ShowTutorialPopup(_tutorialPanelInfo, this.name);
    }
}
