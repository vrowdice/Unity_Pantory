using UnityEngine;

/// <summary>
/// 초기 연구 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 연구 관련 초기 설정을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialResearchData", menuName = "Init Game Data/Initial Research Data")]
public class InitialResearchData : ScriptableObject
{
    [Header("Research Tier Settings")]
    [Tooltip("연구 시스템의 최대 티어 수 (0부터 시작)")]
    [Range(0, 10)]
    public int maxTier = 3;

    [Header("Initial Research Settings")]
    [Tooltip("게임 시작 시 부여되는 연구 포인트")]
    public long initialResearchPoint = 0;

    /// <summary>
    /// Editor에서 값 검증
    /// </summary>
    private void OnValidate()
    {
        // Prevent negative values
        if (maxTier < 0) maxTier = 0;
        if (initialResearchPoint < 0) initialResearchPoint = 0;
    }
}

