using UnityEngine;

/// <summary>
/// 초기 리소스 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 초기 리소스 밸런싱을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialResourceData", menuName = "Game Data/Initial Resource Data", order = 1)]
public class InitialResourceData : ScriptableObject
{
    [Header("Initial Resource Settings")]
    public long initialCredit = 1000;
    [Range(0f, 0.1f)] public float volatilityMultiplier = 0.01f;
    [Range(0f, 10f)] public float maxChangePriceMultiplier = 1.5f;

    /// <summary>
    /// Editor에서 값 검증 (음수 값 방지)
    /// </summary>
    private void OnValidate()
    {
        if (initialCredit < 0) initialCredit = 0;
    }
}

