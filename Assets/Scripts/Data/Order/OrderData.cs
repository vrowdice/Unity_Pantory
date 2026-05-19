using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewOrderData", menuName = "Game Data/Order Data")]
public class OrderData : ScriptableObject
{
    [Tooltip("의뢰 템플릿 고유 ID")]
    public string id;
    [Tooltip("의뢰 제목(UI 표시)")]
    public string displayName;
    [Tooltip("의뢰를 보내는 시장 주체. 비우면 sender 없음")]
    public MarketActorData senderActorData;
    [Tooltip("의뢰 유형 필터(정부·기업·개인). InitialOrderData의 기업가치 조건과 연동")]
    public MarketActorType marketActorType;
    [Tooltip("수락 후 완료까지 주어지는 일수")]
    public int durationDays;
    [Tooltip("완료 시 시장 주체 신뢰도 보상")]
    public int rewardTrust;
    [Tooltip("보상·요구량 산정 시 플레이어 기업가치에 곱하는 비율")]
    public float scaleFactor;
    [Tooltip("이 의뢰에서 요구될 수 있는 자원 후보 목록")]
    public List<ResourceData> potentialResources;
    [Tooltip("요구 자원의 시장가 대비 가격 배율(보상 산정에 사용)")]
    public float priceMultiplier;
}
