using System;
using UnityEngine;

/// <summary>
/// 실제 적용 중인 효과의 런타임 인스턴스입니다.
/// 남은 시간을 추적합니다.
/// </summary>
[Serializable]
public class EffectState
{
    [Tooltip("이펙트 대상 종류")]
    public EffectTargetType targetType;
    [Tooltip("전역 이펙트 여부")]
    public bool isGlobalEffect;
    [Tooltip("이펙트 ID")]
    public string id;
    [Tooltip("표시 이름")]
    public string displayName;
    [Tooltip("변경 스탯")]
    public EffectStatType statType;
    [Tooltip("수치 적용 방식")]
    public ModifierType modifierType;
    [Tooltip("적용 수치")]
    public float value;
    [Tooltip("원본 지속 일수(0 이하면 영구)")]
    public float durationDays;
    [Tooltip("남은 지속 일수")]
    public float remainingDays;
    [Tooltip("대상 ID(isGlobalEffect가 false일 때)")]
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
