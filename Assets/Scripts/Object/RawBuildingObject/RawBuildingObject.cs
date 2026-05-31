using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 상단 +Y 영역에 고정 배치되는 대표 원자재 건물 오브젝트.
/// RawBuildingInfoPanel UI를 통해 가동 갯수(Count)를 조절받으며,
/// 1개의 빌딩 오브젝트 내부에서 갯수에 비례한 비용, 인력, 틱 속도를 곱해 가상 병렬 시뮬레이션을 수행합니다.
/// </summary>
public partial class RawBuildingObject : MonoBehaviour, IResourceNode, IBuilding
{
    [SerializeField] private SpriteRenderer _viewRenderer;
    [SerializeField] private BoxCollider2D _collider;

    private RawMaterialFactoryData _buildingData;
    private ResourceData _selectedResource;

    private int _rawMaterialCount = 0;
    private int _assignedWorkers = 0;
    private int _assignedTechnicians = 0;
    private float _workProgress = 0f;

    private Vector2Int _origin;
    private Vector2Int _size;

    public RawMaterialFactoryData BuildingData => _buildingData;
    public string BuildingDataId => _buildingData != null ? _buildingData.id : null;
    public bool IsRemovalAnimating => false;
    public int RawMaterialCount => _rawMaterialCount;
    public int AssignedWorkers => _assignedWorkers;
    public int AssignedTechnicians => _assignedTechnicians;

    public int RequiredEmployeeSlots => Mathf.Max(0, _buildingData != null ? _buildingData.requiredEmployees * _rawMaterialCount : 0);
    public int MaxWorkerSlots => (_buildingData != null && _buildingData.isProfessional) ? 0 : RequiredEmployeeSlots;
    public int MaxTechnicianSlots => RequiredEmployeeSlots;

    public bool TrySetRawMaterialCount(int newCount)
    {
        if (_buildingData == null) return false;
        if (newCount < 0) return false;

        int oldCount = _rawMaterialCount;
        if (oldCount == newCount) return true;

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null) return false;

        long maintenanceDelta = _buildingData.maintenanceCost * (newCount - oldCount);
        long assetValueDelta = _buildingData.buildCost * (newCount - oldCount);

        if (newCount > oldCount)
        {
            int maxCount = _buildingData.usePlacedCountLimit
                ? dataManager.Building.GetMaxPlacedCount(_buildingData)
                : int.MaxValue;
            if (maxCount > 0 && newCount > maxCount)
                return false;

            long cost = _buildingData.buildCost * (newCount - oldCount);
            if (cost > 0 && dataManager.Finances.Credit < cost)
            {
                return false;
            }
            if (cost > 0)
            {
                dataManager.Finances.ModifyCredit(-cost);
            }
        }
        else
        {
            long refund = _buildingData.buildCost * (oldCount - newCount);
            if (refund > 0)
            {
                dataManager.Finances.ModifyCredit(refund);
            }
        }

        _rawMaterialCount = newCount;
        PlayCountChangeAnimation();
        dataManager.Finances.ModifyPlacedBuildingMaintenance(maintenanceDelta, assetValueDelta);

        int required = RequiredEmployeeSlots;
        int currentAssigned = _assignedWorkers + _assignedTechnicians;
        if (currentAssigned > required)
        {
            int toRemove = currentAssigned - required;
            if (_buildingData.isProfessional)
            {
                TryApplyEmployeeDelta(EmployeeType.Technician, -toRemove);
            }
            else
            {
                int removed = 0;
                if (_assignedWorkers > 0)
                {
                    int rm = Mathf.Min(toRemove, _assignedWorkers);
                    TryApplyEmployeeDelta(EmployeeType.Worker, -rm);
                    removed += rm;
                }
                if (removed < toRemove && _assignedTechnicians > 0)
                {
                    int rm = toRemove - removed;
                    TryApplyEmployeeDelta(EmployeeType.Technician, -rm);
                }
            }
        }

        return true;
    }

    public void TryAutoAssignEmployeesToFill()
    {
        int required = RequiredEmployeeSlots;
        if (required <= 0) return;

        DataManager dataManager = DataManager.Instance;
        if (dataManager?.Employee == null) return;

        dataManager.Employee.TryEnsureRequiredManagers();

        if (_buildingData.isProfessional)
        {
            while (_assignedWorkers + _assignedTechnicians < required)
            {
                if (!TryApplyEmployeeDeltaWithAutoHire(EmployeeType.Technician, 1))
                    break;
            }
        }
        else
        {
            while (_assignedWorkers + _assignedTechnicians < required)
            {
                if (TryApplyEmployeeDeltaWithAutoHire(EmployeeType.Worker, 1))
                    continue;

                if (TryApplyEmployeeDeltaWithAutoHire(EmployeeType.Technician, 1))
                    continue;

                break;
            }
        }

        dataManager.Employee.TryEnsureRequiredManagers();
    }

    private bool TryApplyEmployeeDeltaWithAutoHire(EmployeeType type, int delta)
    {
        if (delta <= 0)
            return TryApplyEmployeeDelta(type, delta);

        DataManager dataManager = DataManager.Instance;
        int requiredTotal = RequiredEmployeeSlots;
        int room = requiredTotal - (_assignedWorkers + _assignedTechnicians);
        if (room <= 0)
            return false;

        if (type == EmployeeType.Worker)
        {
            int addAmount = Mathf.Min(delta, room, MaxWorkerSlots - _assignedWorkers);
            if (addAmount <= 0 || !dataManager.Employee.TryAssignEmployeeWithAutoHire(EmployeeType.Worker, addAmount))
                return false;

            _assignedWorkers += addAmount;
            return true;
        }

        if (type == EmployeeType.Technician)
        {
            int addAmount = Mathf.Min(delta, room, MaxTechnicianSlots - _assignedTechnicians);
            if (addAmount <= 0 || !dataManager.Employee.TryAssignEmployeeWithAutoHire(EmployeeType.Technician, addAmount))
                return false;

            _assignedTechnicians += addAmount;
            return true;
        }

        return false;
    }

    public bool TryApplyEmployeeDelta(EmployeeType type, int delta)
    {
        if (delta == 0) return true;
        DataManager dataManager = DataManager.Instance;

        int requiredTotal = RequiredEmployeeSlots;

        if (delta > 0)
        {
            int room = requiredTotal - (_assignedWorkers + _assignedTechnicians);
            if (room <= 0) return false;

            if (type == EmployeeType.Worker)
            {
                int addAmount = Mathf.Min(
                    delta,
                    room,
                    MaxWorkerSlots - _assignedWorkers,
                    dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Worker));
                if (addAmount <= 0 || !dataManager.Employee.TryAssignEmployee(EmployeeType.Worker, addAmount)) return false;
                _assignedWorkers += addAmount;
                return true;
            }

            if (type == EmployeeType.Technician)
            {
                int addAmount = Mathf.Min(
                    delta,
                    room,
                    MaxTechnicianSlots - _assignedTechnicians,
                    dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Technician));
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

    public void ReleaseAssignedEmployees()
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.Employee == null)
        {
            _assignedWorkers = 0;
            _assignedTechnicians = 0;
            return;
        }

        if (_assignedWorkers > 0)
        {
            int removedWorkers = dataManager.Employee.UnassignUpTo(EmployeeType.Worker, _assignedWorkers);
            _assignedWorkers = Mathf.Max(0, _assignedWorkers - removedWorkers);
        }

        if (_assignedTechnicians > 0)
        {
            int removedTechnicians = dataManager.Employee.UnassignUpTo(EmployeeType.Technician, _assignedTechnicians);
            _assignedTechnicians = Mathf.Max(0, _assignedTechnicians - removedTechnicians);
        }
    }

    public float GetWorkProgressDeltaPerTick(DataManager dataManager)
    {
        if (_rawMaterialCount <= 0) return 0f;

        int assigned = _assignedWorkers + _assignedTechnicians;
        int required = RequiredEmployeeSlots;

        if (assigned <= 0)
            return required <= 0 ? 0.05f : 0f;

        float staffingFill = required <= 0 ? 1f : Mathf.Clamp01((float)assigned / required);
        EmployeeEntry w = dataManager.Employee.GetEmployeeEntry(EmployeeType.Worker);
        EmployeeEntry t = dataManager.Employee.GetEmployeeEntry(EmployeeType.Technician);
        float effW = Mathf.Clamp01(w.state.currentEfficiency);
        float effT = Mathf.Clamp01(t.state.currentEfficiency);
        float effAvg = (_assignedWorkers * effW + _assignedTechnicians * effT) / assigned;

        return Mathf.Clamp01(staffingFill * effAvg);
    }

    public void TickSimulation(DataManager dataManager)
    {
        if (_rawMaterialCount <= 0 || _selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;

        float delta = GetWorkProgressDeltaPerTick(dataManager);
        if (delta <= 0f) return;

        _workProgress += delta;

        while (_workProgress >= 1f)
        {
            int amount = _rawMaterialCount;
            if (dataManager.Resource.ModifyResourceCount(_selectedResource.id, amount))
            {
                ResourceFlowFx.TryPlayToWarehouse(_selectedResource, transform.position);
            }
            _workProgress -= 1f;
        }
    }

    public bool TryPush(ResourcePacket resourcePacket)
    {
        return false; // 원자재 건물은 입력을 지원하지 않음
    }

    public PlacedBuildingSaveData ExportSaveData()
    {
        PlacedBuildingSaveData data = new PlacedBuildingSaveData();
        data.buildingDataId = _buildingData.id;
        data.originX = _origin.x;
        data.originY = _origin.y;
        data.rotation = 0;
        data.selectedResourceId = _selectedResource != null ? _selectedResource.id : null;
        data.workProgress = _workProgress;
        data.assignedWorkers = _assignedWorkers;
        data.assignedTechnicians = _assignedTechnicians;
        data.rawMaterialScale = _rawMaterialCount;

        return data;
    }

    public void ImportSaveData(PlacedBuildingSaveData saveData, DataManager dataManager)
    {
        _workProgress = Mathf.Clamp01(saveData.workProgress);
        _assignedWorkers = Mathf.Max(0, saveData.assignedWorkers);
        _assignedTechnicians = Mathf.Max(0, saveData.assignedTechnicians);
        
        _rawMaterialCount = saveData.rawMaterialScale;

        if (!string.IsNullOrEmpty(saveData.selectedResourceId))
        {
            ResourceData savedResource = dataManager.Resource.GetResourceEntry(saveData.selectedResourceId)?.data;
            if (savedResource != null)
                _selectedResource = savedResource;
        }
    }

    public int ForceRemoveAssignedEmployees(EmployeeType type, int count)
    {
        if (count <= 0) return 0;
        if (type == EmployeeType.Worker)
        {
            int toRemove = Mathf.Min(count, _assignedWorkers);
            _assignedWorkers -= toRemove;
            return toRemove;
        }
        else
        {
            int toRemove = Mathf.Min(count, _assignedTechnicians);
            _assignedTechnicians -= toRemove;
            return toRemove;
        }
    }

    private void PlayCountChangeAnimation()
    {
        if (_viewRenderer == null) return;

        _viewRenderer.transform.DOKill();
        Vector3 baseScale = new Vector3(_size.x, _size.y, 1f);
        _viewRenderer.transform.localScale = baseScale;

        Vector3 punchAmount = baseScale * 0.22f;
        _viewRenderer.transform.DOPunchScale(punchAmount, 0.35f, 12, 0.7f);
    }
}
