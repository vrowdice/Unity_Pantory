using System;
using UnityEngine;

/// <summary>
/// 효과의 정적 데이터 정의 (ScriptableObject나 하드코딩 데이터로 사용)
/// </summary>
[Serializable]
public class EffectData
{
    public string id;              // 효과 ID (예: "Tech_Automation_1")
    public string displayName;     // 표시 이름
    public StatType statType;      // 대상 스탯
    public ModifierType type;      // 연산 방식
    public float value;            // 적용 값
    
    [Header("Targeting")]
    public string targetCategory;  // 특정 카테고리(예: "Electronics")에만 적용. null이면 전역.

    [Header("Duration")]
    public float durationDays;     // 지속 시간 (게임 내 '일' 단위). 0 이하 = 영구 효과.
}

