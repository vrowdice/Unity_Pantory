using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MarketActorState
{
    [Tooltip("당일 자산 변동량")]
    public long currentChangeWealth;
    [Tooltip("현재 보유 자산(기업가치)")]
    public long wealth;
    [Tooltip("플레이어와의 신뢰도")]
    public int trust;
}
