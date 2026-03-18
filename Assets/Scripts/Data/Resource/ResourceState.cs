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
    public int count;
    public int threadDeltaCount;
    public int marketDeltaCount;
    public int currnetChangeCount;

    public long currentEventValue;
    public long currentValue;
    public long currentChangeValue;

    public List<float> _priceHistory;

    public ResourceState()
    {
        _priceHistory = new List<float>();
        currentValue = 0;
        count = 0;
        threadDeltaCount = 0;
    }
}
