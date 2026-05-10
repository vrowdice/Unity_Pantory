using UnityEngine;

[CreateAssetMenu(fileName = "InitialPolicyData", menuName = "Init Game Data/Initial Policy Data")]
public class InitialPolicyData : ScriptableObject
{
    public int PolicyExpirationMonths;

    [Header("Policy cost scaling")]
    [Tooltip("기업가치(Wealth)에 따라 정책 일일 유지비(dailyCreditCost)에 배율을 적용합니다.")]
    public bool useWealthBasedDailyPolicyCostMultiplier = true;

    [Tooltip("정책 유지비 배율 = clamp(Wealth / policyCostWealthDivisor, min, max). 예: 1,000,000이면 Wealth=1,000,000일 때 1.0x")]
    public long policyCostWealthDivisor = 1000000;

    [Tooltip("정책 유지비 배율 최소값")]
    public float policyCostMultiplierMin = 1f;

    [Tooltip("정책 유지비 배율 최대값 (폭주 방지). 0 이하면 상한 없음")]
    public float policyCostMultiplierMax = 20f;
}
