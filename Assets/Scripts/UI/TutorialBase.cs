using UnityEngine;
using System.Collections.Generic;

public class TutorialBase : UITweenEffectBase
{
    [SerializeField] private List<TutorialData> _tutorialPanelInfo = new List<TutorialData>();

    protected virtual void Awake()
    {
        GameObjectUtils.ApplyPrefabInstanceName(gameObject);
    }

    protected virtual void Start()
    {
        TryAutoStartTutorialIfPending();
    }

    /// <summary>아직 완료하지 않은 패널·팝업 튜토리얼만 자동 표시합니다(완료 시 게임 세이브에 포함).</summary>
    protected void TryAutoStartTutorialIfPending()
    {
        if (_tutorialPanelInfo == null || _tutorialPanelInfo.Count == 0)
            return;

        SaveLoadManager saveLoadManager = SaveLoadManager.Instance;
        if (saveLoadManager == null || !saveLoadManager.ShouldAutoStartTutorialForOwner(gameObject.name))
            return;

        StartTutorial();
    }

    /// <summary>
    /// Inspector에 설정된 TutorialData 목록으로 UIManager를 통해 튜토리얼 팝업을 표시합니다.
    /// </summary>
    public void StartTutorial()
    {
        if (_tutorialPanelInfo == null || _tutorialPanelInfo.Count == 0) return;
        if (SceneLoadManager.Instance.GetSceneName() == "Tutorial") return;

        UIManager.Instance.ShowTutorialPopup(_tutorialPanelInfo, this.name);
    }
}
