using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResearch", menuName = "Data/Research Data")]
public class ResearchData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public int tier; // 티어 구분
    public string displayName;
    [TextArea] public string description;
    
    [Header("Costs & Requirements")]
    public long researchPointCost; // 해금에 필요한 RP (예: 5000)
    public List<string> prerequisiteIds; // 선행 연구 ID 목록 (단순 체인용)

    [Header("Rewards")]
    // 연구 완료 시 적용될 효과들 (이전에 만든 EffectData 활용)
    public List<EffectData> unlockEffects;
}
