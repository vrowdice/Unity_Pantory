using UnityEngine;

[CreateAssetMenu(fileName = "InitialGoalData", menuName = "Init Game Data/Initial Goal Data")]
public class InitialGoalData : ScriptableObject
{
    [Tooltip("새 게임 시작 시 활성화할 첫 목표")]
    public GoalData startingGoal;

    [Tooltip("새 게임 시 startingGoal을 자동 시작")]
    public bool autoStartOnNewGame = true;
}
