using UnityEngine;

[CreateAssetMenu(fileName = "New Effect Data", menuName = "Game Data/Effect Data")]
public class EffectData : ScriptableObject
{
    public EffectTargetType targetType;
    public bool isGlobalEffect;
    public string id;              // 효과 ID (예: "Tech_Automation_1")
    public string displayName;     // 표시 이름
    public EffectStatType statType;// 대상 스탯
    public ModifierType type;      // 연산 방식
    public float value;            // 적용 값
    public float durationDays;     // 지속 시간 (게임 내 '일' 단위). 0 이하 = 영구 효과.
}
