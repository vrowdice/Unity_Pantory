using UnityEngine;

[CreateAssetMenu(fileName = "InitialFinancesData", menuName = "Init Game Data/Initial Finances Data")]
public class InitialFinancesData : ScriptableObject
{
    [Tooltip("신규 게임 시작 시 보유 크레딧")]
    public long initialCredit = 1000;
    [Range(0f, 1f)]
    [Tooltip("잔액이 마이너스일 때 하루마다 부과되는 이자율(0.05 = 5%)")]
    public float negativeInterestRate = 0.05f;

    [Tooltip("Wealth가 0 미만일 때 파산까지 남은 개월 수")]
    public int bankruptcyGraceMonths = 3;

    /// <summary>
    /// Editor에서 값 검증 (유효하지 않은 값 방지)
    /// </summary>
    private void OnValidate()
    {
        if (initialCredit < 0) initialCredit = 0;
        if (bankruptcyGraceMonths < 1) bankruptcyGraceMonths = 1;
    }
}
