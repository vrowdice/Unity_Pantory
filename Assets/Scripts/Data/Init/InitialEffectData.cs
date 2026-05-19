using UnityEngine;

[CreateAssetMenu(fileName = "InitialEffectData", menuName = "Init Game Data/Initial Effect Data")]
public class InitialEffectData : ScriptableObject
{
    [Tooltip("관리자 부족 시 적용되는 이펙트(직원 효율·만족도 등)")]
    public EffectData managementDeficitEffect;
    [Tooltip("급여 레벨에 따른 만족도 변화 이펙트")]
    public EffectData salarySatisfactionEffect;
    [Tooltip("만족도에 따른 효율 변화 이펙트")]
    public EffectData satisfactionEfficiencyEffect;
}
