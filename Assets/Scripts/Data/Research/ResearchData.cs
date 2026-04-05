using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResearch", menuName = "Game Data/Research Data")]
public class ResearchData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string displayName;
    public ResearchType researchType;
    public Sprite icon;
    [TextArea] public string description;
    public bool isDefaultUnlocked;
    public long researchPointCost;
    public List<ResearchData> unlockResearchList;
    public List<EffectData> effects;
}
