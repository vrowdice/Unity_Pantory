using System;
using UnityEngine;

/// <summary>
/// 실제 적용 중인 효과의 런타임 인스턴스입니다.
/// 남은 시간을 추적합니다.
/// </summary>
[Serializable]
public class EffectState
{
    public string id;              // 효과 ID (예: "Tech_Automation_1")
    public string displayName;     // 표시 이름
    public StatType statType;      // 대상 스탯
    public ModifierType type;      // 연산 방식
    public float value;            // 적용 값
    public string targetCategory;  // 특정 카테고리(예: "Electronics")에만 적용. null이면 전역.
    public float durationDays;     // 지속 시간 (게임 내 '일' 단위). 0 이하 = 영구 효과.
    public float remainingDays;    // 남은 지속 시간 (일 단위)
    public bool IsPermanent => durationDays <= 0;
}

