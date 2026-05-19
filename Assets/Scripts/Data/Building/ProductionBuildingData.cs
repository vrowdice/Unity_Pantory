using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 생산 건물 데이터
/// 자원을 생산하거나 가공하는 건물에 사용됩니다.
/// </summary>
[CreateAssetMenu(fileName = "NewProductionBuilding", menuName = "Game Data/Building Data/Production")]
public class ProductionBuildingData : BuildingData
{
    [Header("Production")]
    [Tooltip("이 건물이 다룰 수 있는 자원 카테고리 목록")]
    public List<ResourceType> allowedResourceTypes;
    
    [Header("Producible Resources")]
    [Tooltip("실제로 생산·가공할 수 있는 자원 데이터 목록(allowedResourceTypes와 일치해야 함)")]
    public List<ResourceData> _producibleResources = new List<ResourceData>();

    [Header("Simulation")]
    [Tooltip("배치 1회 완료까지 걸리는 시뮬레이션 틱 수")]
    public int ticksPerBatch = 24;
    [Tooltip("배치당 소모되는 입력 자원 개수(레시피 requirements 기준)")]
    public int inputResourcesPerBatch = 1;

    public override bool IsProductionBuilding => true;
    public override List<ResourceType> AllowedResourceTypes => allowedResourceTypes;
    public List<ResourceData> ProducibleResources => _producibleResources;

    protected virtual void OnValidate()
    {
        _producibleResources ??= new List<ResourceData>();
        if (allowedResourceTypes != null && allowedResourceTypes.Count > 0)
        {
            for (int i = _producibleResources.Count - 1; i >= 0; i--)
            {
                ResourceData data = _producibleResources[i];
                if (data == null)
                {
                    continue;
                }

                if (!allowedResourceTypes.Contains(data.type))
                {
                    _producibleResources.RemoveAt(i);
                }
            }
        }
    }
}
