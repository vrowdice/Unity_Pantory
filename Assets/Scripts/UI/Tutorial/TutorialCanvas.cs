using UnityEngine;

/// <summary>
/// 튜토리얼 씬 UI. 게임 데이터 초기화는 씬 로드 시 DataManager.ResetToTutorialGame()에서 수행됩니다.
/// </summary>
public partial class TutorialCanvas : MainCanvas
{
    [SerializeField] private TutorialDirector _tutorialDirector;

    public void Init(TutorialRunner tutorialRunner)
    {
        Init((MainRunner)tutorialRunner);

        BindTutorialGuideEvents();

        if (_mainRunner != null && _mainRunner.GridHandler != null)
            _mainRunner.GridHandler.OnBuildingInstanceLayoutChanged += HandleTutorialBuildingLayoutChanged;

        DataManager.Time.OnDayChanged += HandleTutorialDayChanged;

        _tutorialDirector.Init();
    }

    protected override void OnDestroy()
    {
        UnbindTutorialGuideEvents();

        if (_mainRunner != null && _mainRunner.GridHandler != null)
            _mainRunner.GridHandler.OnBuildingInstanceLayoutChanged -= HandleTutorialBuildingLayoutChanged;

        if (DataManager != null)
            DataManager.Time.OnDayChanged -= HandleTutorialDayChanged;

        base.OnDestroy();
    }
}
