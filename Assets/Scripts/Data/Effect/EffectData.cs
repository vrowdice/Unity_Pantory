using UnityEngine;

[CreateAssetMenu(fileName = "New Effect Data", menuName = "Game Data/Effect Data")]
public class EffectData : ScriptableObject
{
    [Tooltip("이펙트가 적용될 대상 종류(스레드·직원·연구·자원·건물)")]
    public EffectTargetType targetType;
    [Tooltip("true면 특정 ID 없이 전역 적용. false면 targetId로 대상 지정")]
    public bool isGlobalEffect;
    [Tooltip("이펙트 고유 ID")]
    public string id;
    [Tooltip("UI·로그에 표시할 이름")]
    public string displayName;
    [Tooltip("변경할 스탯 종류(효율·가격·만족도 등)")]
    public EffectStatType statType;
    [Tooltip("수치 적용 방식(고정값·% 가산·% 곱)")]
    public ModifierType modifierType;
    [Tooltip("modifierType에 따른 수치(Flat=절대값, PercentAdd=0.1이면 +10%)")]
    public float value;
    [Tooltip("지속 일수. 0 이하면 영구 이펙트")]
    public float durationDays;

    [Tooltip("isGlobalEffect가 false일 때 대상 ID(자원 id, 건물 id, 직원 타입 문자열 등)")]
    public string targetId;
}
