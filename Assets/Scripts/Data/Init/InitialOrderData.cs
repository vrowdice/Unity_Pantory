using UnityEngine;

[CreateAssetMenu(fileName = "InitialOrderData", menuName = "Init Game Data/Initial Order Data")]
public class InitialOrderData : ScriptableObject
{
    [Header("Basic Settings")]
    [Tooltip("동시에 표시·유지할 수 있는 의뢰 최대 개수")]
    public int maxOrderItems = 5;
    [Tooltip("매일 새 의뢰가 발생할 기본 확률(0.1 = 10%)")]
    public float baseOrderChance = 0.1f;

    [Header("Probability Correction (Pity System)")]
    [Tooltip("매일 증가할 추가 확률")]
    public float orderChanceIncrement = 0.05f;
    [Tooltip("의뢰 없이 이 기간이 지나면 100% 확률로 발생")]
    public int guaranteedOrderDay = 10;
    [Tooltip("기업 의뢰가 열리는 기업가치")]
    public long companyOrderAvailableWealth;
    [Tooltip("정부 의뢰가 열리는 기업가치")]
    public long governmentOrderAvailableWealth;
    [Tooltip("의뢰 수락 전 대기 일수(이 기간 후 자동 만료 등)")]
    public int orderAcceptanceDelayDays = 7;
}
