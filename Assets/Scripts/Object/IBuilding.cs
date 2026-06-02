using UnityEngine;

public interface IBuilding
{
    string BuildingDataId { get; }
    int AssignedWorkers { get; }
    int AssignedTechnicians { get; }
    bool IsRemovalAnimating { get; }
    GameObject gameObject { get; }

    void ReleaseAssignedEmployees();
    bool TryApplyEmployeeDelta(EmployeeType type, int delta);
    int ForceRemoveAssignedEmployees(EmployeeType type, int count);
    void TickSimulation(DataManager dataManager);
    PlacedBuildingSaveData ExportSaveData();
}
