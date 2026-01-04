using UnityEngine;

/// <summary>
/// 초기 리소스 데이터를 저장하는 ScriptableObject
/// Inspector를 통해 초기 리소스 밸런싱을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "InitialResourceData", menuName = "Game Data/Initial Resource Data", order = 1)]
public class InitialResourceData : ScriptableObject
{
    [Range(0f, 0.1f)] public float volatilityMultiplier = 0.01f;
    [Range(0f, 5f)] public float maxChangePriceMultiplier = 1.2f;
}

