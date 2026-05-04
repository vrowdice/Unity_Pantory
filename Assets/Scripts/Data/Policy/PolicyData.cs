using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPolicy", menuName = "Game Data/Policy Data")]
public class PolicyData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Tooltip("신규 게임에서 이 정책을 켠 상태로 시작할지")]
    public bool isActiveByDefault;

    [Header("Effects")]
    public List<EffectData> effects = new List<EffectData>();
}
