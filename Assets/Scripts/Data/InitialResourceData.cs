using UnityEngine;

/// <summary>
/// 초기 리소스 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 초기 리소스 밸런싱을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialResourceData", menuName = "Init Game Data/Initial Resource Data")]
public class InitialResourceData : ScriptableObject
{
    [Header("Price Volatility")]
    [Tooltip("가격 변동성 배율")]
    [Range(0f, 0.1f)]
    public float volatilityMultiplier = 0.01f;
    
    [Tooltip("최대 가격 변동 배율")]
    [Range(0f, 5f)]
    public float maxChangePriceMultiplier = 1.2f;
    
    [Header("Price Change Probability")]
    [Tooltip("기본 가격 상승 확률 (0.5 = 50%)")]
    [Range(0f, 1f)]
    public float baseIncreaseProbability = 0.5f;
    
    [Tooltip("오프셋에 적용되는 확률 배율 (0.5 = 오프셋의 50%만큼 확률 조정)")]
    [Range(0f, 1f)]
    public float probabilityOffsetMultiplier = 0.5f;
    
    [Tooltip("최소 가격 상승 확률")]
    [Range(0f, 1f)]
    public float minIncreaseProbability = 0.1f;
    
    [Tooltip("최대 가격 상승 확률")]
    [Range(0f, 1f)]
    public float maxIncreaseProbability = 0.9f;
    
    [Header("Price Change Amount")]
    [Tooltip("변동량 랜덤 범위 최소값 (0.8 = 80%)")]
    [Range(0.1f, 2f)]
    public float changeAmountRandomMin = 0.8f;
    
    [Tooltip("변동량 랜덤 범위 최대값 (1.2 = 120%)")]
    [Range(0.1f, 2f)]
    public float changeAmountRandomMax = 1.2f;
    
    [Tooltip("최소 변동량 (이 값보다 작으면 이 값으로 설정)")]
    [Min(1)]
    public long minChangeAmount = 1;

    [Header("Price History")]
    [Tooltip("가격 히스토리 최대 개수 (그래프 등에 사용)")]
    [Range(10, 120)]
    public int priceHistoryCapacity = 60;

    [Header("Anti-Exploit Settings")]
    [Range(0f, 0.5f)]
    public float transactionFee = 0.05f;
}

