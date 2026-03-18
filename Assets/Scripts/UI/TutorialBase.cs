using UnityEngine;
using System.Collections.Generic;

public class TutorialBase : MonoBehaviour
{
    [SerializeField] private List<TutorialData> _tutorialPanelInfo = new List<TutorialData>();

    /// <summary>
    /// Inspector에 설정된 TutorialData 목록으로 UIManager를 통해 튜토리얼 팝업을 표시합니다.
    /// </summary>
    public void StartTutorial()
    {
        if (_tutorialPanelInfo == null || _tutorialPanelInfo.Count == 0) return;
        UIManager.Instance.ShowTutorialPopup(_tutorialPanelInfo, this.name);
    }
}
