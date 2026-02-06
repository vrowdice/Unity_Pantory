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
    [Tooltip("이 건물에서 생산 가능한 자원 타입 리스트")]
    public List<ResourceType> allowedResourceTypes;
    
    [Header("Producible Resources")]
    [Tooltip("이 건물에서 생산할 수 있는 자원 리소스 데이터 목록")]
    [SerializeField] private List<ResourceData> _producibleResources = new List<ResourceData>();
    
    [Tooltip("입력 위치 (건물 기준 상대 좌표)")]
    public Vector2Int inputPosition = new Vector2Int(-1, 1);
    
    [Tooltip("출력 위치 (건물 기준 상대 좌표)")]
    public Vector2Int outputPosition = new Vector2Int(2, 1);

    /// <summary>
    /// 인스펙터에서 설정한 생산 가능 자원 목록을 반환합니다.
    /// </summary>
    public List<ResourceData> ProducibleResources => _producibleResources;

    public override bool IsProductionBuilding => true;
    public override Vector2Int InputPosition => inputPosition;
    public override Vector2Int OutputPosition => outputPosition;
    public override System.Collections.Generic.List<ResourceType> AllowedResourceTypes => allowedResourceTypes;

    private void OnValidate()
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

