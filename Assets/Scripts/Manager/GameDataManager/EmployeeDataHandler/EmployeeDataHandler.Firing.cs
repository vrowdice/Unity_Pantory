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
    /// <param name="employeeId">해고할 직원 유형 ID</param>
    /// <param name="count">해고할 인원 수</param>
    /// <returns>성공 시 true, 인원 부족 시 false</returns>
    public bool TryFireEmployee(string employeeId, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeService] Fire count must be greater than 0. (input: {count})");
            return true;
        }

        if (!_employees.TryGetValue(employeeId, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {employeeId}");
            return false;
        }

        if (entry.employeeState.count >= count)
        {
            // 1. 해고 전 총 인원 저장
            int currentTotal = entry.employeeState.count;
            
            // 2. 해고 비율 계산 (0.0 ~ 1.0)
            float fireRatio = (float)count / Mathf.Max(1f, currentTotal);
            
            // 3. 비율 기반 패널티 계산
            // 공식: (해고 인원 / 전체 인원) × 최대 공포치
            // 예: 50명 중 1명 해고 (2%) = 0.02 × 100 = 2점
            //     50명 중 25명 해고 (50%) = 0.5 × 100 = 50점
            float penalty = 0f;
            if (_salarySettings != null)
            {
                float maxPanic = _salarySettings.maxFirePanic;
                float minPenalty = _salarySettings.minFireSatisfactionPenalty;
                
                // 비율 기반 패널티 계산
                penalty = fireRatio * maxPanic;
                
                // 최소 패널티 보장 (아무리 적어도 최소한의 영향은 있음)
                penalty = Mathf.Max(minPenalty, penalty);
                
                // 4. [선택적] 관리자에 의한 완화 (Management Mitigation)
                if (_salarySettings.enableManagerMitigation)
                {
                    var managerEntry = GetEmployeeEntry("manager");
                    if (managerEntry != null && managerEntry.employeeState != null && managerEntry.employeeState.count > 0)
                    {
                        // 전체 직원 수 계산
                        int totalEmployees = 0;
                        foreach (var emp in _employees.Values)
                        {
                            if (emp?.employeeState != null)
                            {
                                totalEmployees += emp.employeeState.count;
                            }
                        }
                        
                        if (totalEmployees > 0)
                        {
                            // 관리자 1명당 커버 가능한 직원 수
                            int coveragePerManager = _salarySettings.managerCoverage;
                            float managerCoverage = (float)managerEntry.employeeState.count * coveragePerManager / totalEmployees;
                            
                            // 충분한 관리 커버 시 패널티 감소
                            if (managerCoverage >= 1.0f)
                            {
                                penalty *= _salarySettings.managerMitigationRatio; // 50% 감소
                            }
                            else
                            {
                                // 부분적 완화
                                float mitigation = 1.0f - (managerCoverage * (1.0f - _salarySettings.managerMitigationRatio));
                                penalty *= mitigation;
                            }
                        }
                    }
                }
            }
            else
            {
                // 설정이 없으면 기본값 사용
                penalty = Mathf.Max(1f, fireRatio * 100f);
            }
            
            // 5. 실제 해고 실행
            entry.employeeState.count -= count;
            
            // 6. 만족도 적용 (해당 직군)
            entry.employeeState.currentSatisfaction = Mathf.Clamp(
                entry.employeeState.currentSatisfaction - penalty,
                -100f, 100f
            );
            
            // 7. [파급 효과] 다른 직군에게도 공포 전파 (연대 책임)
            float crossPenaltyRatio = _salarySettings != null 
                ? _salarySettings.crossEmployeeTypeSatisfactionPenaltyRatio 
                : 0.3f;
            
            foreach (var otherEntry in _employees.Values)
            {
                if (otherEntry != entry && otherEntry.employeeState != null && otherEntry.employeeState.count > 0)
                {
                    float crossPenalty = penalty * crossPenaltyRatio;
                    otherEntry.employeeState.currentSatisfaction = Mathf.Clamp(
                        otherEntry.employeeState.currentSatisfaction - crossPenalty,
                        -100f, 100f
                    );
                    
                    // 다른 직군의 효율성도 재계산
                    UpdateEfficiencyFromSatisfaction(otherEntry);
                }
            }
            
            // 해고된 직군의 효율성 재계산
            UpdateEfficiencyFromSatisfaction(entry);
            
            // 로그 출력 (디버깅용)
            Debug.Log($"[HR] Fired {count}/{currentTotal} ({fireRatio:P1}). Satisfaction Penalty: -{penalty:F1}");
            
            UpdateSalary(entry);
            OnEmployeeChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[EmployeeService] {entry.employeeData.displayName} not enough employees! (required: {count}, available: {entry.employeeState.count})");
            return false;
        }
    }
}

