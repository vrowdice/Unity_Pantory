using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMarketActorData", menuName = "Game Data/Market Actor Data")]
public class MarketActorData : ScriptableObject
{
    [Tooltip("시장 주체 고유 ID")]
    public string id;
    [Tooltip("UI에 표시할 이름")]
    public string displayName;
    [Tooltip("목록·의뢰 UI용 아이콘")]
    public Sprite icon;
    [TextArea(3, 10)]
    [Tooltip("설명 문구")]
    public string description;
    [Tooltip("주체 유형(정부·기업·개인)")]
    public MarketActorType marketActorType;

    [Tooltip("시뮬레이션 시작 시 보유 자산(기업가치 계산 등에 사용)")]
    public long baseWealth;
    [Tooltip("일일 생산·거래 규모 기준값")]
    public long baseProductionCount;
    [Tooltip("시뮬레이션에서 소비하는 자원 목록")]
    public List<ResourceData> comsumeResourceList = new List<ResourceData>();
    [Tooltip("시뮬레이션에서 생산·공급하는 자원 목록")]
    public List<ResourceData> productionResourceList = new List<ResourceData>();
}
