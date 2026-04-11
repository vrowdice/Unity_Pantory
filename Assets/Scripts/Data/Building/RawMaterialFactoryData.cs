using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 원자재(ResourceType.raw)만을 생산하는 공장 데이터 ScriptableObject입니다.
/// 생산 가능 목록은 부모의 <see cref="ProductionBuildingData.ProducibleResources"/> 한 곳에서만 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRawMaterialFactory", menuName = "Game Data/Building Data/Raw Material Factory")]
public class RawMaterialFactoryData : ProductionBuildingData
{
    [SerializeField, HideInInspector, FormerlySerializedAs("_producibleRawResources")]
    private List<ResourceData> _legacyProducibleRawResources;

    /// <summary>
    /// 기본 원자재(첫 번째 항목)를 반환합니다. 없으면 null.
    /// </summary>
    public ResourceData DefaultRawResource =>
        ProducibleResources != null && ProducibleResources.Count > 0 ? ProducibleResources[0] : null;

    private void OnValidate()
    {
        usePlacedCountLimit = true;

        allowedResourceTypes ??= new List<ResourceType>();
        allowedResourceTypes.RemoveAll(type => type != ResourceType.raw);

        if (!allowedResourceTypes.Contains(ResourceType.raw))
        {
            allowedResourceTypes.Add(ResourceType.raw);
        }

        MigrateLegacyProducibleRawIntoSharedList();

        _producibleResources ??= new List<ResourceData>();
        for (int i = _producibleResources.Count - 1; i >= 0; i--)
        {
            ResourceData data = _producibleResources[i];
            if (data == null)
            {
                continue;
            }

            if (data.type != ResourceType.raw)
            {
                _producibleResources.RemoveAt(i);
            }
        }

        base.OnValidate();
    }

    private void MigrateLegacyProducibleRawIntoSharedList()
    {
        if (_legacyProducibleRawResources == null || _legacyProducibleRawResources.Count == 0) return;

        _producibleResources ??= new List<ResourceData>();
        foreach (ResourceData data in _legacyProducibleRawResources)
        {
            if (data == null) continue;
            if (!_producibleResources.Contains(data))
                _producibleResources.Add(data);
        }

        _legacyProducibleRawResources.Clear();
    }
}
