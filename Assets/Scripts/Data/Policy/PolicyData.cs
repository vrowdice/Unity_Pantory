using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPolicy", menuName = "Game Data/Policy Data")]
public class PolicyData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("정책 고유 ID")]
    public string id;
    [Tooltip("정책 이름(UI 표시)")]
    public string displayName;
    [TextArea]
    [Tooltip("정책 설명")]
    public string description;
    [Tooltip("정책 목록 아이콘")]
    public Sprite icon;

    [Tooltip("신규 게임에서 이 정책을 켠 상태로 시작할지")]
    public bool isActiveByDefault;

    [Header("Daily credit cost")]
    [Tooltip("하루마다 소모되는 크레딧(0 이상). 수입·지급은 없음")]
    public long dailyCreditCost;

    [Header("Modification lock")]
    [Tooltip("정책 ON/OFF를 바꾼 뒤, 다시 바꿀 수 없게 유지되는 개월 수. 0이면 InitialPolicyData.PolicyExpirationMonths를 사용")]
    [Min(0)]
    public int modificationLockMonths;

    [Header("Effects")]
    [Tooltip("정책 활성화 시 적용되는 이펙트 목록")]
    public List<EffectData> effects = new List<EffectData>();
}
