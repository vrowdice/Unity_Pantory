using UnityEngine;

public partial class BuildingObject
{
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
}

