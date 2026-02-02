using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 초기 시장 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 시장 밸런싱을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialMarketData", menuName = "Game Data/Initial Market Data")]
public class InitialMarketActorData : ScriptableObject
{
    [Header("Business Success Rate")]
    [Tooltip("비즈니스 성공률의 최소값 (0.8 = 80% 효율)")]
    [Range(0.5f, 1.5f)]
    public float businessSuccessMin = 0.8f;
    
    [Tooltip("비즈니스 성공률의 최대값 (1.3 = 130% 효율)")]
    [Range(0.5f, 1.5f)]
    public float businessSuccessMax = 1.3f;
    
    [Header("Cost Variation")]
    [Tooltip("비용 변동률의 최소값 (0.9 = 90% 비용)")]
    [Range(0.5f, 1.5f)]
    public float costVariationMin = 0.9f;
    
    [Tooltip("비용 변동률의 최대값 (1.1 = 110% 비용)")]
    [Range(0.5f, 1.5f)]
    public float costVariationMax = 1.1f;
    
    [Header("Profit Limiting")]
    [Tooltip("일일 순이익 상한선 (이 값을 초과하면 제한 배율이 적용됩니다)")]
    [Min(0)]
    public long maxDailyNetProfit = 100000;
    
    [Tooltip("일일 순이익이 상한선을 초과할 때 적용되는 배율 (0.5 = 50%로 제한)")]
    [Range(0.1f, 1.0f)]
    public float profitLimitMultiplier = 0.5f;
}

