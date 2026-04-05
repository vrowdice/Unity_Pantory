using System;
using UnityEngine;

/// <summary>
/// 실제 적용 중인 효과의 런타임 인스턴스입니다.
/// 남은 시간을 추적합니다.
/// </summary>
[Serializable]
public class EffectState
{
    public EffectTargetType targetType;
    public bool isGlobalEffect;
    public string id;
    public string displayName;
    public EffectStatType statType;
    public ModifierType modifierType;
    public float value;
    public float durationDays;
    public float remainingDays;
    public string targetId;
    
    public bool IsPermanent => durationDays <= 0;
    public bool IsExpired => remainingDays <= 0;

    public EffectState(EffectData data)
    {
        targetType = data.targetType;
        isGlobalEffect = data.isGlobalEffect;
        id = data.id;
        displayName = data.displayName;
        statType = data.statType;
        modifierType = data.modifierType;
        value = data.value;
        durationDays = data.durationDays;
        remainingDays = data.durationDays;
        targetId = data.targetId;
    }

    public bool ProcessDayPass(int date)
    {
        if (IsPermanent) return false;

        remainingDays -= date;
        if (remainingDays <= 0)
        {
            remainingDays = 0;
            return true;
        }
        return false;
    }
}

