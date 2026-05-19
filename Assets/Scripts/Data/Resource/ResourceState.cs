using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 자원의 현재 상태 (데이터 저장용).
/// 이펙트는 EffectDataHandler에서 인스턴스별로 관리합니다.
/// </summary>
[Serializable]
public class ResourceState
{
    [Tooltip("플레이어·창고 보유 수량")]
    public int count;
    [Tooltip("생산 스레드(건물)에서의 일일 변동")]
    public int threadDeltaCount;
    [Tooltip("시장 시뮬레이션 일일 변동")]
    public int marketDeltaCount;
    [Tooltip("당일 순 변동량(UI 표시용)")]
    public int currnetChangeCount;

    [Tooltip("이벤트·뉴스 등으로 조정된 기준가")]
    public long currentEventValue;
    [Tooltip("현재 시장 거래가")]
    public long currentValue;
    [Tooltip("당일 가격 변동량")]
    public long currentChangeValue;

    [Tooltip("과거 가격 기록(그래프용). InitialResourceData.priceHistoryCapacity 참조")]
    public List<float> _priceHistory;

    public ResourceState()
    {
        _priceHistory = new List<float>();
        currentValue = 0;
        count = 0;
        threadDeltaCount = 0;
    }
}
