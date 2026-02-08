using UnityEngine;

[CreateAssetMenu(fileName = "InitialFinancesData", menuName = "Init Game Data/Initial Finances Data")]
public class InitialFinancesData : ScriptableObject
{
    public long initialCredit = 1000;
    [Range(0f, 1f)]
    public float negativeInterestRate = 0.05f;

    /// <summary>
    /// Editor���� �� ���� (���� �� ����)
    /// </summary>
    private void OnValidate()
    {
        if (initialCredit < 0) initialCredit = 0;
    }
}
