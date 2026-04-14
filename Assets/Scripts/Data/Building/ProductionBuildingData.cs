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
    public List<ResourceType> allowedResourceTypes;
    
    [Header("Producible Resources")]
    public List<ResourceData> _producibleResources = new List<ResourceData>();

    [Header("Simulation")]
    public int ticksPerBatch = 24;
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

