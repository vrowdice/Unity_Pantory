using UnityEngine;

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
    public bool TryAssignEmployee(EmployeeType type, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeDataHandler] Assign count must be greater than 0. (input: {count})");
            return false;
        }

        if (!TryGetEntry(type, out EmployeeEntry entry))
            return false;

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

        if (!TryGetEntry(type, out EmployeeEntry entry))
            return false;

        if (entry.state.assignedCount >= count)
        {
            entry.state.assignedCount -= count;
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
        if (!TryGetEntry(type, out EmployeeEntry entry)) return 0;
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
        if (!TryGetEntry(type, out EmployeeEntry entry)) return 0;
        return entry.state.assignedCount;
    }

    /// <summary>
    /// 요청 수만큼 할당 인원을 강제로 해제합니다. 실제 해제된 수를 반환합니다.
    /// </summary>
    public int UnassignUpTo(EmployeeType type, int count)
    {
        if (count <= 0) return 0;
        if (!TryGetEntry(type, out EmployeeEntry entry)) return 0;

        int removed = Mathf.Min(count, entry.state.assignedCount);
        if (removed <= 0) return 0;

        entry.state.assignedCount -= removed;
        OnEmployeeChanged?.Invoke();
        return removed;
    }

    /// <summary>
    /// 크레딧 범위 내에서 고용 가능한 최대 인원 수.
    /// </summary>
    public int GetMaxAffordableHireCount(EmployeeType type)
    {
        if (!TryGetEntry(type, out EmployeeEntry entry) || entry.data == null)
            return 0;

        if (_dataManager?.Finances == null)
            return 0;

        long hiringCost = entry.data.hiringCost;
        if (hiringCost <= 0)
            return int.MaxValue;

        long credit = _dataManager.Finances.Credit;
        if (credit < hiringCost)
            return 0;

        return (int)Mathf.Min(int.MaxValue, credit / hiringCost);
    }

    /// <summary>
    /// 크레딧이 허용하는 만큼만 고용합니다. 실제 고용된 수를 반환합니다.
    /// </summary>
    public int TryHireUpToAffordable(EmployeeType type, int count)
    {
        if (count <= 0)
            return 0;

        int affordableCount = GetMaxAffordableHireCount(type);
        int hireCount = Mathf.Min(count, affordableCount);
        if (hireCount <= 0)
            return 0;

        HireEmployee(type, hireCount);
        return hireCount;
    }

    /// <summary>
    /// 할당 가능 인원이 부족하면 자동 고용 후 할당을 시도합니다.
    /// </summary>
    public bool TryAssignEmployeeWithAutoHire(EmployeeType type, int count)
    {
        if (count <= 0)
            return false;

        int availableCount = GetAvailableEmployeeCount(type);
        if (availableCount < count)
            TryHireUpToAffordable(type, count - availableCount);

        return TryAssignEmployee(type, count);
    }
}

