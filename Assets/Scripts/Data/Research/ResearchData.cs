using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResearch", menuName = "Game Data/Research Data")]
public class ResearchData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public int tier;
    public string displayName;
    [TextArea] public string description;
    
    [Header("Costs & Requirements")]
    public long researchPointCost;
    public List<string> prerequisiteIds;

    [Header("Rewards")]
    public List<EffectData> unlockEffects;
}
