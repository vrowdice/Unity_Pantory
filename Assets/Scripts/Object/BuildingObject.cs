using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 메인 씬에서 그리드에 설치된 건물 오브젝트의 데이터 + 뷰 + 콜라이더를 모두 관리합니다.
/// 건물 데이터 타입에 따라 시간 틱 시 자원 입출력/생산을 처리합니다.
/// </summary>
public class BuildingObject : MonoBehaviour, IResourceNode
{
    [SerializeField] private BuildingData _buildingData;
    [SerializeField] private Vector2Int _origin;
    [SerializeField] private Vector2Int _size;
    [SerializeField] private int _rotation;
    [SerializeField] private SpriteRenderer _viewObjRenderer;
    [SerializeField] private BoxCollider2D _collider;

    [Header("Output Indicators")]
    [SerializeField] private GameObject _outputIndicatorPrefab;

    public List<Vector2Int> OutputGridPositions { get; private set; }

    [Header("Resource Buffer")]
    [SerializeField] private int _maxCapacity = 1;
    private readonly Queue<ResourcePacket> _buffer = new Queue<ResourcePacket>();

    private int _productionProgress;
    private int _outputRoundRobinIndex;

    [Header("Player Selection (런타임)")]
    [Tooltip("가공/원자재: 생산할 자원. 하역: 창고에서 뺄 자원. 인스펙터 또는 클릭 UI에서 설정")]
    [SerializeField] private ResourceData _selectedResource;

    public BuildingData BuildingData => _buildingData;
    public ResourceData SelectedResource => _selectedResource;
    public Vector2Int Origin => _origin;
    public Vector2Int Size => _size;
    public int Rotation => _rotation;

    public bool IsEmpty => _buffer.Count == 0;
    public bool IsFull => _buffer.Count >= _maxCapacity;

    public void Init(BuildingData buildingData, Vector2Int origin, Vector2Int rotatedSize, int rotation)
    {
        _buildingData = buildingData;
        _origin = origin;
        _size = rotatedSize;
        _rotation = rotation;

        ApplyCapacityFromBuildingData();

        RebuildOutputGridPositions();

        transform.localRotation = Quaternion.Euler(0f, 0f, -rotation * 90f);

        if (_viewObjRenderer != null)
        {
            _viewObjRenderer.sprite = buildingData != null ? buildingData.buildingSprite : null;
            _viewObjRenderer.transform.localRotation = Quaternion.identity;
            _viewObjRenderer.transform.localScale = new Vector3(rotatedSize.x, rotatedSize.y, 1);
        }

        if (_collider != null)
        {
            _collider.size = new Vector2(rotatedSize.x, rotatedSize.y);
            _collider.offset = Vector2.zero;
        }

        UpdateOutputIndicators();
    }

    private void ApplyCapacityFromBuildingData()
    {
        if (_buildingData == null)
        {
            return;
        }

        if (_buildingData is LoadStationData)
        {
            _maxCapacity = 1;
            return;
        }

        if (_buildingData is UnloadStationData unloadData)
        {
            _maxCapacity = Mathf.Max(1, unloadData.pullPerHour);
            return;
        }

        if (_buildingData is RawMaterialFactoryData)
        {
            _maxCapacity = 0;
            return;
        }

        if (_buildingData is ProductionBuildingData prodData)
        {
            _maxCapacity = Mathf.Max(1, prodData.inputBufferCapacity);
        }
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null)
        {
            return false;
        }

        if (_buildingData is RawMaterialFactoryData)
        {
            return false;
        }

        if (_maxCapacity <= 0)
        {
            return false;
        }

        if (_buffer.Count >= _maxCapacity)
        {
            return false;
        }

        _buffer.Enqueue(packet);
        return true;
    }

    public bool TryPeek(out ResourcePacket packet)
    {
        if (_buffer.Count == 0)
        {
            packet = null;
            return false;
        }

        packet = _buffer.Peek();
        return true;
    }

    public bool TryPop(out ResourcePacket packet)
    {
        if (_buffer.Count == 0)
        {
            packet = null;
            return false;
        }

        packet = _buffer.Dequeue();
        return true;
    }

    /// <summary>
    /// 시간 틱(예: 1시간)마다 호출. 자원 창고 연동 및 생산 진행을 처리합니다.
    /// </summary>
    public void TickSimulation(DataManager dataManager)
    {
        if (_buildingData == null || dataManager == null)
        {
            return;
        }

        if (_buildingData is RawMaterialFactoryData rawFactory)
        {
            TickRawMaterialFactory(dataManager, rawFactory);
            return;
        }

        if (_buildingData is LoadStationData)
        {
            TickLoadStation(dataManager);
            return;
        }

        if (_buildingData is UnloadStationData unloadData)
        {
            TickUnloadStation(dataManager, unloadData);
            return;
        }

        if (_buildingData is ProductionBuildingData prodData)
        {
            TickProductionBuilding(prodData);
        }
    }

    /// <summary>
    /// 플레이어가 이 건물에서 생산/반출할 자원을 선택합니다. 가공·원자재·하역소에서 사용.
    /// </summary>
    public bool TrySetSelectedResource(ResourceData resource)
    {
        if (_buildingData is ProductionBuildingData prod)
        {
            if (resource != null && !IsResourceAllowedForProduction(prod, resource))
            {
                return false;
            }

            _selectedResource = resource;
            _productionProgress = 0;
            return true;
        }

        if (_buildingData is RawMaterialFactoryData raw)
        {
            if (resource != null && !IsResourceAllowedForRawFactory(raw, resource))
            {
                return false;
            }

            _selectedResource = resource;
            return true;
        }

        if (_buildingData is UnloadStationData)
        {
            _selectedResource = resource;
            return true;
        }

        return false;
    }

    private static bool IsResourceAllowedForProduction(ProductionBuildingData prod, ResourceData resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.id))
        {
            return false;
        }

        List<ResourceData> list = prod.ProducibleResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item != null && item.id == resource.id)
                {
                    return true;
                }
            }

            return false;
        }

        List<ResourceType> types = prod.AllowedResourceTypes;
        if (types != null && types.Count > 0)
        {
            return types.Contains(resource.type);
        }

        return true;
    }

    private static bool IsResourceAllowedForRawFactory(RawMaterialFactoryData raw, ResourceData resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.id))
        {
            return false;
        }

        List<ResourceData> list = raw.ProducibleRawResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item != null && item.id == resource.id)
                {
                    return true;
                }
            }

            return false;
        }

        return resource.type == ResourceType.raw;
    }

    private void TickRawMaterialFactory(DataManager dataManager, RawMaterialFactoryData rawFactory)
    {
        if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id))
        {
            return;
        }

        if (!IsResourceAllowedForRawFactory(rawFactory, _selectedResource))
        {
            return;
        }

        dataManager.Resource.ModifyResourceCount(_selectedResource.id, 1);
    }

    private void TickLoadStation(DataManager dataManager)
    {
        while (_buffer.Count > 0)
        {
            if (!TryPeek(out ResourcePacket packet))
            {
                break;
            }

            if (!dataManager.Resource.ModifyResourceCount(packet.Id, packet.Amount))
            {
                break;
            }

            TryPop(out _);
        }
    }

    private void TickUnloadStation(DataManager dataManager, UnloadStationData unloadData)
    {
        if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id))
        {
            return;
        }

        string resourceId = _selectedResource.id;
        int pulls = Mathf.Max(0, unloadData.pullPerHour);

        for (int i = 0; i < pulls; i++)
        {
            if (!dataManager.Resource.ModifyResourceCount(resourceId, -1))
            {
                break;
            }

            ResourcePacket packet = new ResourcePacket(resourceId, 1);
            if (!TryPush(packet))
            {
                dataManager.Resource.ModifyResourceCount(resourceId, 1);
                break;
            }
        }
    }

    private void TickProductionBuilding(ProductionBuildingData prodData)
    {
        if (prodData.ticksPerBatch <= 0)
        {
            return;
        }

        if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id))
        {
            return;
        }

        if (!IsResourceAllowedForProduction(prodData, _selectedResource))
        {
            return;
        }

        string outputId = _selectedResource.id;

        _productionProgress++;

        if (_productionProgress < prodData.ticksPerBatch)
        {
            return;
        }

        int need = Mathf.Max(0, prodData.inputResourcesPerBatch);
        if (_buffer.Count < need)
        {
            _productionProgress = prodData.ticksPerBatch;
            return;
        }

        List<ResourcePacket> taken = new List<ResourcePacket>();
        for (int i = 0; i < need; i++)
        {
            if (!TryPop(out ResourcePacket p))
            {
                break;
            }

            taken.Add(p);
        }

        if (taken.Count < need)
        {
            foreach (ResourcePacket p in taken)
            {
                TryPush(p);
            }

            _productionProgress = prodData.ticksPerBatch;
            return;
        }

        ResourcePacket outputPacket = new ResourcePacket(outputId, 1);
        if (!TryPush(outputPacket))
        {
            foreach (ResourcePacket p in taken)
            {
                TryPush(p);
            }

            _productionProgress = prodData.ticksPerBatch;
            return;
        }

        _productionProgress = 0;
    }

    /// <summary>
    /// 건물 → 도로 배분 시 번갈아 출력할 다음 인덱스를 반환하고 증가시킵니다.
    /// </summary>
    public void AdvanceOutputRoundRobin()
    {
        int count = OutputGridPositions != null ? OutputGridPositions.Count : 0;
        if (count <= 0)
        {
            return;
        }

        _outputRoundRobinIndex = (_outputRoundRobinIndex + 1) % count;
    }

    public int OutputRoundRobinStartIndex => _outputRoundRobinIndex;

    private void RebuildOutputGridPositions()
    {
        OutputGridPositions = BuildingCalculationUtils.GetOutputGridPositions(_origin, _size, _rotation);
    }

    private void UpdateOutputIndicators()
    {
        if (_buildingData == null)
        {
            return;
        }

        if (_outputIndicatorPrefab == null)
        {
            return;
        }

        foreach (Vector3 localPos in BuildingCalculationUtils.GetOutputLocalPositions(_size))
        {
            GameObject indicator = Instantiate(_outputIndicatorPrefab, transform);
            indicator.transform.localPosition = localPos;
            indicator.transform.localRotation = Quaternion.identity;
        }
    }
}
