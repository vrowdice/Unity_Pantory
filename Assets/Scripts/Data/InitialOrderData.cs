using UnityEngine;

[CreateAssetMenu(fileName = "InitialOrderData", menuName = "Init Game Data/Initial Order Data")]
public class InitialOrderData : ScriptableObject
{
    [Header("Basic Settings")]
    public int maxOrderItems = 5;
    public float baseOrderChance = 0.1f;

    [Header("Probability Correction (Pity System)")]
    [Tooltip("매일 증가할 추가 확률")]
    public float orderChanceIncrement = 0.05f;

    [Tooltip("의뢰 없이 이 기간이 지나면 100% 확률로 발생")]
    public int guaranteedOrderDay = 10;
}
