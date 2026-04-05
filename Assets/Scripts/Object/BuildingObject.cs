using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 그리드에 설치된 건물: 뷰, 입출력 큐, 시간 틱 시뮬, 직원, 클릭 UI.
/// </summary>
public class BuildingObject : MonoBehaviour, IResourceNode
{
    [SerializeField] private SpriteRenderer _viewObjRenderer;
    [SerializeField] private BoxCollider2D _collider;

    [Header("Output Indicators")]
    [SerializeField] private GameObject _outputIndicatorPrefab;
    [SerializeField] private float _outgoingIconScale = 0.01f;

    [Header("Player Selection")]
    [SerializeField] private ResourceData _selectedResource;

    [Header("Employees")]
    [SerializeField] private int _assignedWorkers;
    [SerializeField] private int _assignedTechnicians;

    private GameObject _outgoingIconContainer;
    private readonly Queue<ResourcePacket> _inputBuffer = new Queue<ResourcePacket>();
    private readonly Queue<ResourcePacket> _outputBuffer = new Queue<ResourcePacket>();

    private MainRunner _mainRunner;
    private BuildingData _buildingData;
    private Vector2Int _origin;
    private Vector2Int _size;
    private int _rotation;
    private float _workProgress;
    private int _maxInputCapacity;
    private int _maxOutputCapacity;

    public BuildingData BuildingData => _buildingData;
    public Vector2Int Origin => _origin;
    public Vector2Int Size => _size;
    public List<Vector2Int> OutputGridPositions { get; private set; }

    public int AssignedWorkers => _assignedWorkers;
    public int AssignedTechnicians => _assignedTechnicians;
    public int RequiredEmployeeSlots => Mathf.Max(0, _buildingData.requiredEmployees);
    public int RequiredTechnicianMinimum => _buildingData.isProfessional && _buildingData.requiredEmployees > 0 ? 1 : 0;
    public int MaxWorkerSlots => Mathf.Max(0, RequiredEmployeeSlots - RequiredTechnicianMinimum);
    public int MaxTechnicianSlots => RequiredEmployeeSlots;

    public void Init(MainRunner runner, BuildingData buildingData, Vector2Int origin, Vector2Int rotatedSize, int rotation)
    {
        _mainRunner = runner;
        _buildingData = buildingData;
        _origin = origin;
        _size = rotatedSize;
        _rotation = rotation;
        _maxInputCapacity = Mathf.Max(0, buildingData.maxInputBufferCapacity);
        _maxOutputCapacity = Mathf.Max(0, buildingData.maxOutputBufferCapacity);

        transform.localRotation = Quaternion.Euler(0f, 0f, -rotation * 90f);

        _viewObjRenderer.sprite = buildingData.buildingSprite;
        _viewObjRenderer.transform.localRotation = Quaternion.identity;
        _viewObjRenderer.transform.localScale = new Vector3(rotatedSize.x, rotatedSize.y, 1);
        _collider.size = new Vector2(rotatedSize.x, rotatedSize.y);
        _collider.offset = Vector2.zero;

        RebuildOutputGridPositions();
        UpdateOutputIndicators();
        RefreshOutgoingResourceIcons();

        if (_selectedResource == null && buildingData is RawMaterialFactoryData raw && raw.DefaultRawResource != null)
            _selectedResource = raw.DefaultRawResource;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (_mainRunner != null && (_mainRunner.IsPlacementMode || _mainRunner.IsRemovalMode)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1)) return;
        if (_collider == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        if (!_collider.OverlapPoint(mouseWorld)) return;

        UIManager.Instance?.ShowBuildingInfoPopup(this);
    }

    /// <summary>
    /// UI 레시피 그리드용. 선택 레시피 기준 입력/출력 ID 목록을 채웁니다.
    /// </summary>
    public void GetRecipeDisplayData(List<string> inputIds, List<string> outputIds, out string currentResourceId)
    {
        inputIds.Clear();
        outputIds.Clear();
        currentResourceId = _selectedResource != null ? _selectedResource.id : null;
        if (_selectedResource == null) return;

        if (_selectedResource.requirements != null)
        {
            foreach (ResourceRequirement req in _selectedResource.requirements)
            {
                if (req.resource == null) continue;
                int count = Mathf.Max(1, req.count);
                for (int i = 0; i < count; i++)
                    inputIds.Add(req.resource.id);
            }
        }
        _selectedResource.AppendBatchOutputIds(outputIds);
    }

    public bool TrySetSelectedResource(ResourceData data)
    {
        if (data == null) return false;
        if (_buildingData is ProductionBuildingData prod && !IsResourceAllowedForProduction(prod, data))
            return false;
        if (_buildingData is RawMaterialFactoryData raw && !IsResourceAllowedForRawFactory(raw, data))
            return false;

        _selectedResource = data;
        RefreshOutgoingResourceIcons();
        return true;
    }

    public float GetProductionProgressNormalized()
    {
        if (_buildingData is ProductionBuildingData || _buildingData is UnloadStationData || _buildingData is LoadStationData)
            return Mathf.Clamp01(_workProgress);
        return 0f;
    }

    public float GetAverageAssignedEfficiencyNormalized(DataManager dataManager)
    {
        if (dataManager == null || _buildingData == null) return 0f;
        int total = _assignedWorkers + _assignedTechnicians;
        if (total <= 0)
            return RequiredEmployeeSlots <= 0 ? 1f : 0f;

        GetGlobalEmployeeEfficienciesNormalized(dataManager, out float effW, out float effT);
        return Mathf.Clamp01((_assignedWorkers * effW + _assignedTechnicians * effT) / total);
    }

    public float GetWorkProgressDeltaPerTick(DataManager dataManager)
    {
        if (dataManager == null || !UsesStaffedBatchSimulation()) return 0f;

        int assigned = _assignedWorkers + _assignedTechnicians;
        int required = RequiredEmployeeSlots;

        if (assigned <= 0)
            return required <= 0 ? 1f : 0f;

        float staffingFill = required <= 0 ? 1f : Mathf.Clamp01((float)assigned / required);
        GetGlobalEmployeeEfficienciesNormalized(dataManager, out float effW, out float effT);
        float effAvg = (_assignedWorkers * effW + _assignedTechnicians * effT) / assigned;
        return Mathf.Clamp01(staffingFill * effAvg);
    }

    private bool UsesStaffedBatchSimulation()
    {
        return _buildingData is ProductionBuildingData
            || _buildingData is UnloadStationData
            || _buildingData is LoadStationData;
    }

    private static void GetGlobalEmployeeEfficienciesNormalized(DataManager dm, out float effW, out float effT)
    {
        effW = 0f;
        effT = 0f;
        if (dm == null) return;
        EmployeeEntry w = dm.Employee.GetEmployeeEntry(EmployeeType.Worker);
        EmployeeEntry t = dm.Employee.GetEmployeeEntry(EmployeeType.Technician);
        effW = w != null ? Mathf.Clamp01(w.state.currentEfficiency) : 0f;
        effT = t != null ? Mathf.Clamp01(t.state.currentEfficiency) : 0f;
    }

    public bool TryApplyEmployeeDelta(EmployeeType type, int delta)
    {
        if (delta == 0) return true;
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null) return false;

        int requiredTotal = RequiredEmployeeSlots;

        if (delta > 0)
        {
            int room = requiredTotal - (_assignedWorkers + _assignedTechnicians);
            if (room <= 0) return false;

            if (type == EmployeeType.Worker)
            {
                int addAmount = Mathf.Min(delta, room, MaxWorkerSlots - _assignedWorkers, dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker));
                if (addAmount <= 0 || !dataManager.Employee.TryAssignEmployee(EmployeeType.Worker, addAmount)) return false;
                _assignedWorkers += addAmount;
                return true;
            }

            if (type == EmployeeType.Technician)
            {
                int addAmount = Mathf.Min(delta, room, MaxTechnicianSlots - _assignedTechnicians, dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician));
                if (addAmount <= 0 || !dataManager.Employee.TryAssignEmployee(EmployeeType.Technician, addAmount)) return false;
                _assignedTechnicians += addAmount;
                return true;
            }

            return false;
        }

        int removeAmount = -delta;
        if (type == EmployeeType.Worker)
        {
            removeAmount = Mathf.Min(removeAmount, _assignedWorkers);
            if (removeAmount <= 0 || !dataManager.Employee.TryUnassignEmployee(EmployeeType.Worker, removeAmount)) return false;
            _assignedWorkers -= removeAmount;
            return true;
        }

        if (type == EmployeeType.Technician)
        {
            removeAmount = Mathf.Min(removeAmount, _assignedTechnicians);
            if (removeAmount <= 0 || !dataManager.Employee.TryUnassignEmployee(EmployeeType.Technician, removeAmount)) return false;
            _assignedTechnicians -= removeAmount;
            return true;
        }

        return false;
    }

    public bool TryPush(ResourcePacket packet)
    {
        if (packet == null || _buildingData is RawMaterialFactoryData || _buildingData is UnloadStationData) return false;

        if (_buildingData is LoadStationData || _buildingData is ProductionBuildingData)
        {
            if (_maxInputCapacity <= 0 || _inputBuffer.Count >= _maxInputCapacity) return false;
            _inputBuffer.Enqueue(packet);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 출력(생산·하역) 또는 상역 입력 큐 맨 앞을 이웃에 넣을 수 있을 때만 전달합니다. 실패 시 큐는 그대로입니다.
    /// </summary>
    public bool TryForwardTo(IResourceNode destination)
    {
        if (destination == null) return false;

        if (_buildingData is ProductionBuildingData || _buildingData is UnloadStationData)
        {
            if (_outputBuffer.Count == 0) return false;
            ResourcePacket packet = _outputBuffer.Peek();
            if (!destination.TryPush(packet)) return false;
            _outputBuffer.Dequeue();
            return true;
        }

        if (_buildingData is LoadStationData)
        {
            if (_inputBuffer.Count == 0) return false;
            ResourcePacket packet = _inputBuffer.Peek();
            if (!destination.TryPush(packet)) return false;
            _inputBuffer.Dequeue();
            return true;
        }

        return false;
    }

    public Dictionary<string, int> GetRuntimeInputResourceCounts() => AggregateQueueCounts(_inputBuffer);
    public Dictionary<string, int> GetRuntimeOutputResourceCounts() => AggregateQueueCounts(_outputBuffer);

    public void TickSimulation(DataManager dataManager)
    {
        if (_buildingData is RawMaterialFactoryData rawFactory)
        {
            TickRawMaterialFactory(dataManager, rawFactory);
            return;
        }

        if (_buildingData is LoadStationData)
        {
            TickStaffedBatchWork(dataManager, () => CanCompleteLoadBatch(dataManager), () => TryCompleteLoadBatch(dataManager));
            return;
        }

        if (_buildingData is UnloadStationData)
        {
            if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;
            TickStaffedBatchWork(dataManager, () => CanCompleteUnloadBatch(dataManager), () => TryCompleteUnloadBatch(dataManager));
            return;
        }

        if (_buildingData is ProductionBuildingData)
        {
            if (_selectedResource == null) return;
            TickStaffedBatchWork(dataManager, CanCompleteProductionBatch, TryCompleteProductionBatch);
        }
    }

    private void TickStaffedBatchWork(DataManager dataManager, System.Func<bool> canCompleteBatch, System.Func<bool> tryCompleteBatch)
    {
        float delta = GetWorkProgressDeltaPerTick(dataManager);
        if (delta <= 0f) return;

        _workProgress += delta;
        if (_workProgress > 1f && !canCompleteBatch()) _workProgress = 1f;

        while (_workProgress >= 1f)
        {
            if (!tryCompleteBatch()) break;
            _workProgress -= 1f;
        }
    }

    private void TickRawMaterialFactory(DataManager dataManager, RawMaterialFactoryData rawFactory)
    {
        if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;
        if (!IsResourceAllowedForRawFactory(rawFactory, _selectedResource)) return;
        dataManager.Resource.ModifyResourceCount(_selectedResource.id, 1);
    }

    private bool CanCompleteLoadBatch(DataManager dataManager)
    {
        if (!(_buildingData is LoadStationData l) || dataManager == null || _inputBuffer.Count == 0) return false;
        ResourcePacket p = _inputBuffer.Peek();
        int move = Mathf.Min(Mathf.Max(1, l.pushPerHour), p.Amount);
        return dataManager.Resource.GetResourceEntry(p.Id) != null && move > 0;
    }

    private bool TryCompleteLoadBatch(DataManager dataManager)
    {
        if (!(_buildingData is LoadStationData l) || dataManager == null || _inputBuffer.Count == 0) return false;
        ResourcePacket p = _inputBuffer.Peek();
        int move = Mathf.Min(Mathf.Max(1, l.pushPerHour), p.Amount);
        if (!dataManager.Resource.ModifyResourceCount(p.Id, move)) return false;
        ConsumeFromInputFront(move);
        return true;
    }

    private void ConsumeFromInputFront(int amountFromFrontPacket)
    {
        ResourcePacket p = _inputBuffer.Dequeue();
        int remainder = p.Amount - amountFromFrontPacket;
        if (remainder <= 0) return;

        List<ResourcePacket> tail = new List<ResourcePacket>();
        while (_inputBuffer.Count > 0)
            tail.Add(_inputBuffer.Dequeue());

        _inputBuffer.Enqueue(new ResourcePacket(p.Id, remainder));
        foreach (ResourcePacket t in tail)
            _inputBuffer.Enqueue(t);
    }

    private bool CanCompleteUnloadBatch(DataManager dataManager)
    {
        if (!(_buildingData is UnloadStationData u) || _selectedResource == null) return false;
        int outCap = u.outputBufferCapacity > 0 ? u.outputBufferCapacity : _maxOutputCapacity;
        if (outCap > 0 && _outputBuffer.Count >= outCap) return false;
        ResourceEntry entry = dataManager.Resource.GetResourceEntry(_selectedResource.id);
        if (entry == null) return false;
        return entry.state.count >= u.pullPerHour;
    }

    private bool TryCompleteUnloadBatch(DataManager dataManager)
    {
        if (!(_buildingData is UnloadStationData u) || _selectedResource == null) return false;
        int pull = u.pullPerHour;
        if (!dataManager.Resource.ModifyResourceCount(_selectedResource.id, -pull)) return false;
        _outputBuffer.Enqueue(new ResourcePacket(_selectedResource.id, pull));
        return true;
    }

    private bool CanCompleteProductionBatch()
    {
        if (_selectedResource == null) return false;

        Dictionary<string, int> need = GetInputNeedForBatch(_selectedResource);
        if (need.Count > 0 && !InputBufferSatisfies(need)) return false;

        Dictionary<string, int> outputs = _selectedResource.GetBatchOutputCounts();
        int newPackets = outputs.Count;
        if (_maxOutputCapacity > 0 && _outputBuffer.Count + newPackets > _maxOutputCapacity) return false;

        return true;
    }

    private bool TryCompleteProductionBatch()
    {
        if (_selectedResource == null) return false;

        Dictionary<string, int> need = GetInputNeedForBatch(_selectedResource);
        if (need.Count > 0)
        {
            if (!InputBufferSatisfies(need)) return false;
            if (!TryConsumeFromInputBuffer(need)) return false;
        }

        Dictionary<string, int> outputs = _selectedResource.GetBatchOutputCounts();
        foreach (KeyValuePair<string, int> kvp in outputs)
        {
            if (string.IsNullOrEmpty(kvp.Key)) continue;
            _outputBuffer.Enqueue(new ResourcePacket(kvp.Key, Mathf.Max(1, kvp.Value)));
        }

        return true;
    }

    private static Dictionary<string, int> GetInputNeedForBatch(ResourceData recipe)
    {
        Dictionary<string, int> need = new Dictionary<string, int>();
        if (recipe == null) return need;
        if (recipe.requirements != null)
        {
            foreach (ResourceRequirement req in recipe.requirements)
            {
                if (req.resource == null) continue;
                int c = Mathf.Max(1, req.count);
                if (need.TryGetValue(req.resource.id, out int existing)) need[req.resource.id] = existing + c;
                else need[req.resource.id] = c;
            }
        }

        return need;
    }

    private bool InputBufferSatisfies(Dictionary<string, int> need)
    {
        Dictionary<string, int> have = AggregateQueueCounts(_inputBuffer);
        foreach (KeyValuePair<string, int> kvp in need)
        {
            if (!have.TryGetValue(kvp.Key, out int h) || h < kvp.Value)
                return false;
        }
        return true;
    }

    private bool TryConsumeFromInputBuffer(Dictionary<string, int> need)
    {
        if (!InputBufferSatisfies(need)) return false;

        List<ResourcePacket> snapshot = new List<ResourcePacket>();
        while (_inputBuffer.Count > 0)
            snapshot.Add(_inputBuffer.Dequeue());

        List<ResourcePacket> working = new List<ResourcePacket>(snapshot);
        Dictionary<string, int> remaining = new Dictionary<string, int>(need);

        for (int i = 0; i < working.Count; i++)
        {
            ResourcePacket p = working[i];
            if (p == null || string.IsNullOrEmpty(p.Id)) continue;

            if (!remaining.TryGetValue(p.Id, out int rem) || rem <= 0)
                continue;

            if (p.Amount <= rem)
            {
                rem -= p.Amount;
                remaining[p.Id] = rem;
                working.RemoveAt(i);
                i--;
            }
            else
            {
                working[i] = new ResourcePacket(p.Id, p.Amount - rem);
                remaining[p.Id] = 0;
            }
        }

        foreach (KeyValuePair<string, int> kvp in remaining)
        {
            if (kvp.Value > 0)
            {
                foreach (ResourcePacket p in snapshot)
                    _inputBuffer.Enqueue(p);
                return false;
            }
        }

        foreach (ResourcePacket p in working)
            _inputBuffer.Enqueue(p);

        return true;
    }

    private static bool IsResourceAllowedForProduction(ProductionBuildingData prod, ResourceData resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.id)) return false;

        List<ResourceData> list = prod.ProducibleResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item != null && item.id == resource.id) return true;
            }
            return false;
        }

        List<ResourceType> types = prod.AllowedResourceTypes;
        return types == null || types.Count == 0 || types.Contains(resource.type);
    }

    private static bool IsResourceAllowedForRawFactory(RawMaterialFactoryData raw, ResourceData resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.id)) return false;

        List<ResourceData> list = raw.ProducibleRawResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item != null && item.id == resource.id) return true;
            }
            return false;
        }

        return resource.type == ResourceType.raw;
    }

    private void RebuildOutputGridPositions()
    {
        OutputGridPositions = BuildingCalculationUtils.GetOutputGridPositions(_origin, _size, _rotation);
    }

    private void UpdateOutputIndicators()
    {
        if (_outputIndicatorPrefab == null || _buildingData is LoadStationData) return;
        foreach (Vector3 localPos in BuildingCalculationUtils.GetOutputLocalPositions(_size))
        {
            GameObject indicator = Instantiate(_outputIndicatorPrefab, transform);
            indicator.transform.localPosition = localPos;
            indicator.transform.localRotation = Quaternion.identity;
        }
    }

    public void RefreshOutgoingResourceIcons()
    {
        ClearOutgoingIconContainer();

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || UIManager.Instance == null) return;

        Transform worldCanvas = gameManager.GetWorldCanvas();
        if (worldCanvas == null) return;

        Dictionary<string, int> counts = BuildOutgoingResourceCounts();
        if (counts == null || counts.Count == 0) return;
        Vector3 worldPosition = transform.position + new Vector3(0f, 0f, -1f);

        _outgoingIconContainer = UIManager.Instance.CreateProductionIconContainer(
            worldCanvas,
            $"BuildingOutgoingIcons_{gameObject.GetInstanceID()}",
            worldPosition,
            _outgoingIconScale,
            counts);
    }

    private Dictionary<string, int> BuildOutgoingResourceCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        if (_buildingData == null || _buildingData is LoadStationData)
            return counts;

        if (_buildingData is ProductionBuildingData || _buildingData is UnloadStationData || _buildingData is RawMaterialFactoryData)
        {
            if (_selectedResource == null) return counts;
            return _selectedResource.GetBatchOutputCounts();
        }

        return counts;
    }

    private void ClearOutgoingIconContainer()
    {
        if (_outgoingIconContainer == null) return;
        PoolingManager.Instance?.ClearChildrenToPool(_outgoingIconContainer.transform);
        Destroy(_outgoingIconContainer);
        _outgoingIconContainer = null;
    }

    private static Dictionary<string, int> AggregateQueueCounts(Queue<ResourcePacket> queue)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (ResourcePacket p in queue)
        {
            if (p == null || string.IsNullOrEmpty(p.Id)) continue;
            if (counts.TryGetValue(p.Id, out int v)) counts[p.Id] = v + p.Amount;
            else counts[p.Id] = p.Amount;
        }

        return counts;
    }

    private void OnDestroy()
    {
        ClearOutgoingIconContainer();
    }
}
