using UnityEngine;

/// <summary>
/// 초기 리소스 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 초기 리소스 밸런싱을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialResourceData", menuName = "Game Data/Initial Resource Data", order = 1)]
public class InitialResourceData : ScriptableObject
{
    [Header("Initial Resource Settings")]
    [Tooltip("게임 시작 시 부여되는 은화")]
    public long initialCredit = 1000;

    /// <summary>
    /// Editor에서 값 검증 (음수 값 방지)
    /// </summary>
    private void OnValidate()
    {
        // Prevent negative values
        if (initialCredit < 0) initialCredit = 0;
    }
}

