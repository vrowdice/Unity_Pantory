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
    /// <param name="type">해고할 직원 유형</param>
    /// <param name="count">해고할 인원 수</param>
    /// <returns>성공 시 true, 인원 부족 시 false</returns>
    /// <summary>
    /// EmployeeDataHandler 해고 파트 (Firing)
    /// </summary>
    public bool TryFireEmployee(EmployeeType type, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning($"[EmployeeService] Fire count must be greater than 0. (input: {count})");
            return true;
        }

        if (!_employees.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"[EmployeeService] Unregistered employee type: {type}");
            return false;
        }

        if (entry.state.count >= count)
        {
            // 1. 해고 전 총 인원 저장
            int currentTotal = entry.state.count;
            
            // 2. 해고 비율 계산 (0.0 ~ 1.0)
            float fireRatio = (float)count / Mathf.Max(1f, currentTotal);
            
            // 3. 비율 기반 패널티 계산
            // 공식: (해고 인원 / 전체 인원) × 최대 공포치
            // 예: 50명 중 1명 해고 (2%) = 0.02 × 100 = 2점
            //     50명 중 25명 해고 (50%) = 0.5 × 100 = 50점
            float penalty = 0f;
            if (_initialEmployeeData != null)
            {
                float maxPanic = _initialEmployeeData.maxFirePanic;
                float minPenalty = _initialEmployeeData.minFireSatisfactionPenalty;
                
                // 비율 기반 패널티 계산
                penalty = fireRatio * maxPanic;
                
                // 최소 패널티 보장 (아무리 적어도 최소한의 영향은 있음)
                penalty = Mathf.Max(minPenalty, penalty);
                
                // 4. [선택적] 관리자에 의한 완화 (Management Mitigation)
                if (_initialEmployeeData.enableManagerMitigation)
                {
                    var managerEntry = GetEmployeeEntry(EmployeeType.Manager);
                    if (managerEntry != null && managerEntry.state != null && managerEntry.state.count > 0)
                    {
                        // 전체 직원 수 계산
                        int totalEmployees = 0;
                        foreach (var emp in _employees.Values)
                        {
                            if (emp?.state != null)
                            {
                                totalEmployees += emp.state.count;
                            }
                        }
                        
                        if (totalEmployees > 0)
                        {
                            // 관리자 1명당 커버 가능한 직원 수
                            int coveragePerManager = _initialEmployeeData.managerCoverage;
                            float managerCoverage = (float)managerEntry.state.count * coveragePerManager / totalEmployees;
                            
                            // 충분한 관리 커버 시 패널티 감소
                            if (managerCoverage >= 1.0f)
                            {
                                penalty *= _initialEmployeeData.managerMitigationRatio; // 50% 감소
                            }
                            else
                            {
                                // 부분적 완화
                                float mitigation = 1.0f - (managerCoverage * (1.0f - _initialEmployeeData.managerMitigationRatio));
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
            entry.state.count -= count;
            
            // 6. 만족도 적용 (해당 직군)
            entry.state.currentSatisfaction = Mathf.Clamp(
                entry.state.currentSatisfaction - penalty,
                -100f, 100f
            );
            
            // 7. [파급 효과] 다른 직군에게도 공포 전파 (연대 책임)
            float crossPenaltyRatio = _initialEmployeeData != null 
                ? _initialEmployeeData.crossEmployeeTypeSatisfactionPenaltyRatio 
                : 0.3f;
            
            foreach (var otherEntry in _employees.Values)
            {
                if (otherEntry != entry && otherEntry.state != null && otherEntry.state.count > 0)
                {
                    float crossPenalty = penalty * crossPenaltyRatio;
                    otherEntry.state.currentSatisfaction = Mathf.Clamp(
                        otherEntry.state.currentSatisfaction - crossPenalty,
                        -100f, 100f
                    );
                    
                    // 다른 직군의 효율성도 재계산
                    UpdateEfficiencyFromSatisfaction(otherEntry);
                }
            }
            
            // 해고된 직군의 효율성 재계산
            UpdateEfficiencyFromSatisfaction(entry);
            
            UpdateSalary(entry);
            OnEmployeeChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[EmployeeService] {entry.data.displayName} not enough employees! (required: {count}, available: {entry.state.count})");
            return false;
        }
    }
}

