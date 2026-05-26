using UnityEngine;

public partial class MainCanvas
{
    [Header("Goals")]
    [SerializeField] private GoalPanelContainer _goalPanel;

    private void InitGoalUi()
    {
        if (_goalPanel == null)
            _goalPanel = GetComponentInChildren<GoalPanelContainer>(true);

        _goalPanel?.Init(DataManager);
    }
}
