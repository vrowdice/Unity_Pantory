using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 뉴스 템플릿을 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewNewsData", menuName = "Game Data/News Data")]
public class NewsData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string displayName;
    [TextArea(2, 6)]
    public string description;
    public Sprite icon;
    public List<EffectData> effects;
    public int durationDays;
}
