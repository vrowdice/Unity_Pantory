using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 직원 할당 관리 (assignedCount)
/// </summary>
public partial class EmployeeDataHandler
{
    /// <summary>
    /// 특정 직원 유형의 할당된 인원 수를 증가시킵니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="count">증가시킬 인원 수</param>
    /// <returns>성공 시 true, 인원 부족 시 false</returns>
    /// <summary>
    /// EmployeeDataHandler 할당 파트 (Assignment)
    /// </summary>
    public bool TryAssignEmployee(EmployeeType type, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeDataHandler] Assign count must be greater than 0. (input: {count})");
            return false;
        }

        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {type}");
            return false;
        }

        // 할당 가능한 인원 수 확인 (고용된 인원 - 이미 할당된 인원)
        int availableCount = entry.state.count - entry.state.assignedCount;
        
        if (availableCount >= count)
        {
            entry.state.assignedCount += count;
            OnEmployeeChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[EmployeeDataHandler] Not enough available employees for {type}: (requested: {count}, available: {availableCount}, total: {entry.state.count}, assigned: {entry.state.assignedCount})");
            return false;
        }
    }

    /// <summary>
    /// 특정 직원 유형의 할당된 인원 수를 감소시킵니다.
    /// 할당 해제는 단순히 업무 배치를 해제하는 것이므로 만족도에 영향을 주지 않습니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <param name="count">감소시킬 인원 수</param>
    /// <returns>성공 시 true, 할당된 인원 부족 시 false</returns>
    public bool TryUnassignEmployee(EmployeeType type, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unassign count must be greater than 0. (input: {count})");
            return false;
        }

        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {type}");
            return false;
        }

        if (entry.state.assignedCount >= count)
        {
            entry.state.assignedCount -= count;
            // assignedCount는 음수가 될 수 없음
            if (entry.state.assignedCount < 0)
            {
                entry.state.assignedCount = 0;
            }
            
            OnEmployeeChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[EmployeeDataHandler] Not enough assigned employees for {type}: (requested: {count}, assigned: {entry.state.assignedCount})");
            return false;
        }
    }

    /// <summary>
    /// 특정 직원 유형의 할당 가능한 인원 수를 반환합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <returns>할당 가능한 인원 수 (고용된 인원 - 이미 할당된 인원)</returns>
    public int GetAvailableEmployeeCount(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {type}");
            return 0;
        }

        int available = entry.state.count - entry.state.assignedCount;
        return Mathf.Max(0, available);
    }

    /// <summary>
    /// 특정 직원 유형의 할당된 인원 수를 반환합니다.
    /// </summary>
    /// <param name="type">직원 유형</param>
    /// <returns>할당된 인원 수</returns>
    public int GetAssignedEmployeeCount(EmployeeType type)
    {
        if (!_employees.TryGetValue(type, out EmployeeEntry entry))
        {
            Debug.LogWarning($"[EmployeeDataHandler] Unregistered employee type: {type}");
            return 0;
        }

        return entry.state.assignedCount;
    }

    /// <summary>
    /// 모든 배치된 스레드 인스턴스의 직원 할당 상태를 동기화합니다.
    /// ThreadPlacementDataHandler의 실제 배치된 인스턴스들의 currentWorkers와 currentTechnicians를 기반으로 assignedCount를 업데이트합니다.
    /// 배치된 인원은 다른 스레드에 배치될 수 없도록 보장합니다.
    /// </summary>
    public void SyncAssignedCountsFromThreads(ThreadPlacementDataHandler threadPlacementHandler)
    {
        if (threadPlacementHandler == null)
            return;

        // 모든 직원의 assignedCount를 0으로 초기화
        foreach (EmployeeEntry entry in _employees.Values)
        {
            entry.state.assignedCount = 0;
        }

        // 실제 배치된 모든 스레드 인스턴스의 직원 할당을 집계
        Dictionary<Vector2Int, ThreadPlacementState> allPlacements = threadPlacementHandler.GetAllPlacedThreads();
        if (allPlacements == null)
        {
            OnEmployeeChanged?.Invoke();
            return;
        }

        foreach (ThreadPlacementState placement in allPlacements.Values)
        {
            if (placement == null || placement.RuntimeState == null)
                continue;

            ThreadState threadState = placement.RuntimeState;

            // Worker 할당 집계
            if (threadState.currentWorkers > 0)
            {
                EmployeeEntry workerEntry = GetEmployeeEntry(EmployeeType.Worker);
                if (workerEntry != null)
                {
                    workerEntry.state.assignedCount += threadState.currentWorkers;
                }
            }

            // Technician 할당 집계
            if (threadState.currentTechnicians > 0)
            {
                EmployeeEntry technicianEntry = GetEmployeeEntry(EmployeeType.Technician);
                if (technicianEntry != null)
                {
                    technicianEntry.state.assignedCount += threadState.currentTechnicians;
                }
            }
        }

        OnEmployeeChanged?.Invoke();
    }
}

