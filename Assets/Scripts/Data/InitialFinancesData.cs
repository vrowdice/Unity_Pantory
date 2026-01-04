using UnityEngine;

[CreateAssetMenu(fileName = "InitialFinancesData", menuName = "Game Data/Initial Finances Data", order = 1)]
public class InitialFinancesData : ScriptableObject
{
    public long initialCredit = 1000;

    /// <summary>
    /// Editor에서 값 검증 (음수 값 방지)
    /// </summary>
    private void OnValidate()
    {
        if (initialCredit < 0) initialCredit = 0;
    }
}
