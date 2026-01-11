using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원자재(ResourceType.raw)만을 생산하는 공장 데이터 ScriptableObject입니다.
/// 생산 가능한 원자재를 직접 참조로 지정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRawMaterialFactory", menuName = "Game Data/Building Data/Raw Material Factory", order = 7)]
public class RawMaterialFactoryData : ProductionBuildingData
{
    [Header("Raw Material Factory Settings")]
    [Tooltip("이 공장에서 생산할 수 있는 원자재 리소스 데이터 목록")]
    [SerializeField] private List<ResourceData> _producibleRawResources = new List<ResourceData>();

    /// <summary>
    /// 인스펙터에서 설정한 생산 가능 원자재 목록을 반환합니다.
    /// </summary>
    public List<ResourceData> ProducibleRawResources => _producibleRawResources;

    /// <summary>
    /// 기본 원자재(첫 번째 항목)를 반환합니다. 없으면 null.
    /// </summary>
    public ResourceData DefaultRawResource => _producibleRawResources != null && _producibleRawResources.Count > 0
        ? _producibleRawResources[0]
        : null;

    private void OnValidate()
    {
        // allowedResourceTypes가 null이면 초기화
        allowedResourceTypes ??= new List<ResourceType>();

        // Raw 이외의 타입은 제거하고 Raw만 유지
        allowedResourceTypes.RemoveAll(type => type != ResourceType.raw);

        if (!allowedResourceTypes.Contains(ResourceType.raw))
        {
            allowedResourceTypes.Add(ResourceType.raw);
        }

        // 생산 가능 원자재 목록이 null이면 새로 생성
        _producibleRawResources ??= new List<ResourceData>();

        // Raw 타입이 아닌 자원은 목록에서 제거
        for (int i = _producibleRawResources.Count - 1; i >= 0; i--)
        {
            ResourceData data = _producibleRawResources[i];
            if (data == null)
            {
                continue;
            }

            if (data.type != ResourceType.raw)
            {
                Debug.LogWarning($"{name}: {data.name}은(는) 원자재 타입이 아니므로 제거되었습니다.");
                _producibleRawResources.RemoveAt(i);
            }
        }
    }
}
