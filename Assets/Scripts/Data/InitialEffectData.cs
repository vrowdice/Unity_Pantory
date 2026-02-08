using UnityEngine;

[CreateAssetMenu(fileName = "InitialEffectData", menuName = "Init Game Data/Initial Effect Data")]
public class InitialEffectData : ScriptableObject
{
    public EffectData managementDeficitEffect;
    public EffectData salarySatisfactionEffect;
    public EffectData satisfactionEfficiencyEffect;
    public EffectData priceEventEffect;
}
