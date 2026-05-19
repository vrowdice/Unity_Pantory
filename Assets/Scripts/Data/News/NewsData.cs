using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 뉴스 템플릿을 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewNewsData", menuName = "Game Data/News Data")]
public class NewsData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("뉴스 템플릿 고유 ID")]
    public string id;
    [Tooltip("뉴스 제목(UI 표시)")]
    public string displayName;
    [Tooltip("속보 여부(강조 표시 등)")]
    public bool isBreakingNews;
    [TextArea(2, 6)]
    [Tooltip("뉴스 본문")]
    public string description;
    [Tooltip("뉴스 목록·팝업 아이콘")]
    public Sprite icon;
    [Tooltip("발생 시 적용할 이펙트 목록")]
    public List<EffectData> effects;
    [Tooltip("뉴스 지속 일수. 0 이하면 영구")]
    public int durationDays;
}
