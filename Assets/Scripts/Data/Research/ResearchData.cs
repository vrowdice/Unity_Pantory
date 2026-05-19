using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResearch", menuName = "Game Data/Research Data")]
public class ResearchData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("연구 고유 ID")]
    public string id;
    [Tooltip("연구 트리·UI에 표시할 이름")]
    public string displayName;
    [Tooltip("연구 분류(건물 해금·원자재 탐색·생산 효율 등)")]
    public ResearchType researchType;
    [Tooltip("연구 노드 아이콘")]
    public Sprite icon;
    [TextArea]
    [Tooltip("연구 설명")]
    public string description;
    [Tooltip("신규 게임에서 연구 포인트 없이 해금된 상태로 시작")]
    public bool isDefaultUnlocked;
    [Tooltip("완료에 필요한 연구 포인트(RP)")]
    public long researchPointCost;
    [Tooltip("이 연구를 시작하기 전에 완료해야 하는 선행 연구")]
    public List<ResearchData> unlockResearchList;
    [Tooltip("연구 완료 시 영구 적용되는 이펙트")]
    public List<EffectData> effects;
}
