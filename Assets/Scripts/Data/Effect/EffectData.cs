using UnityEngine;

[CreateAssetMenu(fileName = "New Effect Data", menuName = "Game Data/Effect Data")]
public class EffectData : ScriptableObject
{
    public EffectTargetType targetType;
    public bool isGlobalEffect;
    public string id;
    public string displayName;
    public EffectStatType statType;
    public ModifierType modifierType;
    public float value;
    public float durationDays;

    public string targetId;
}
