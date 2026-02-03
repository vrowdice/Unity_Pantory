using UnityEngine;

[CreateAssetMenu(fileName = "InitialNewsData", menuName = "Game Data/Initial News Data")]
public class InitialNewsData : ScriptableObject
{
    [Header("Basic Settings")]
    public int maxNewsItems = 5;
    public float baseNewsChance = 0.1f;

    [Header("Probability Correction (Pity System)")]
    [Tooltip("매일 증가할 추가 확률")]
    public float newsChanceIncrement = 0.05f;

    [Tooltip("뉴스 없이 이 기간이 지나면 100% 확률로 발생")]
    public int guaranteedNewsDay = 10;

    [Header("Resource Price Effect (PercentAdd only)")]
    [Tooltip("자원 가격 이펙트의 최대 증가율 (PercentAdd 값, 예: 0.2 = +20%)")]
    public float maxResourcePricePer = 0.2f;
    [Tooltip("자원 가격 이펙트의 최소 증가율 (PercentAdd 값, 예: -0.2 = -20%)")]
    public float minResourcePricePer = -0.2f;
}
