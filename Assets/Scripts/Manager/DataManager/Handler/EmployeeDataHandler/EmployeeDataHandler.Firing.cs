using UnityEngine;

/// <summary>
/// 직원 해고 및 만족도 관리
/// </summary>
public partial class EmployeeDataHandler
{
    /// <summary>
    /// 특정 직원 유형을 해고합니다. 인원이 부족하면 실패합니다.
    /// 대량 해고 시 공포와 분노로 인해 만족도가 기하급수적으로 하락하며, 다른 직군에도 영향을 미칩니다.
    /// </summary>
    public bool TryFireEmployee(EmployeeType type, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeDataHandler] Fire count must be greater than 0. (input: {count})");
            return true;
        }

        if (!TryGetEntry(type, out EmployeeEntry entry))
            return false;

        if (entry.state.count >= count)
        {
            long totalFiringCost = entry.data.firingCost * (long)count;
            if (totalFiringCost != 0)
            {
                _dataManager?.Finances?.ModifyCredit(-totalFiringCost);
            }

            int currentTotal = entry.state.count;
            float fireRatio = (float)count / Mathf.Max(1f, currentTotal);
            float penalty = 0f;
            if (_initialEmployeeData != null)
            {
                float maxPanic = _initialEmployeeData.maxFirePanic;
                float minPenalty = _initialEmployeeData.minFireSatisfactionPenalty;
                penalty = fireRatio * maxPanic;
                penalty = Mathf.Max(minPenalty, penalty);

                if (_initialEmployeeData.enableManagerMitigation &&
                    TryGetEntry(EmployeeType.Manager, out EmployeeEntry managerEntry) && managerEntry.state.count > 0)
                {
                    int totalEmployees = GetTotalEmployeeCount();

                    if (totalEmployees > 0)
                    {
                        int coveragePerManager = _initialEmployeeData.managerCoverage;
                        float managerCoverage = (float)managerEntry.state.count * coveragePerManager / totalEmployees;
                        if (managerCoverage >= 1.0f)
                        {
                            penalty *= _initialEmployeeData.managerMitigationRatio;
                        }
                        else
                        {
                            penalty *= 1.0f - (managerCoverage * (1.0f - _initialEmployeeData.managerMitigationRatio));
                        }
                    }
                }
            }
            else
            {
                penalty = Mathf.Max(1f, fireRatio * 100f);
            }

            entry.state.count -= count;
            entry.state.currentSatisfaction = Mathf.Clamp(
                entry.state.currentSatisfaction - penalty,
                -100f, 100f
            );

            float crossPenaltyRatio = _initialEmployeeData != null
                ? _initialEmployeeData.crossEmployeeTypeSatisfactionPenaltyRatio
                : 0f;

            foreach (EmployeeEntry otherEntry in _employees.Values)
            {
                if (otherEntry != entry && otherEntry.state.count > 0)
                {
                    float crossPenalty = penalty * crossPenaltyRatio;
                    otherEntry.state.currentSatisfaction = Mathf.Clamp(
                        otherEntry.state.currentSatisfaction - crossPenalty,
                        -100f, 100f
                    );
                    UpdateEfficiencyFromSatisfaction(otherEntry);
                }
            }

            UpdateEfficiencyFromSatisfaction(entry);
            
            UpdateSalary(entry);
            OnEmployeeChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[EmployeeDataHandler] {entry.data.displayName} not enough employees! (required: {count}, available: {entry.state.count})");
            return false;
        }
    }
}

