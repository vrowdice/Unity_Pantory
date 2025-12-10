using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 연구 데이터를 관리하고 연구력(RP) 생산, 연구 해금, 효과 적용을 담당하는 핸들러
/// </summary>
public class ResearchDataHandler
{
    private readonly GameDataManager _manager;
    private Dictionary<string, ResearchEntry> _researchEntries = new();

    // ----------------- 전역 연구력 (Global RP) -----------------
    public long CurrentResearchPoints { get; private set; }

    // ----------------- 특허 수익 설정 -----------------
    // true면 RP가 쌓이는 대신 자동으로 크레딧으로 전환됨 (후반부용)
    public bool IsAutoPatentMode { get; set; } = false; 
    private const int RP_TO_CREDIT_RATIO = 10; // 10 RP = 1 Credit (밸런스 조절 필요)

    // ----------------- 이벤트 -----------------
    public event Action OnResearchPointsChanged;
    public event Action<string> OnResearchUnlocked; // 해금된 연구 ID 전달

    public ResearchDataHandler(GameDataManager manager)
    {
        _manager = manager;
        AutoLoadAllResearch();
    }

    // ========================================================================
    // 1. 초기화 및 로드
    // ========================================================================

    /// <summary>
    /// 모든 연구 데이터를 자동으로 로드합니다.
    /// </summary>
    public void AutoLoadAllResearch()
    {
#if UNITY_EDITOR
        // 에디터 모드: AssetDatabase를 사용하여 모든 ResearchData 찾기
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ResearchData");
        int loadedCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ResearchData researchData = UnityEditor.AssetDatabase.LoadAssetAtPath<ResearchData>(assetPath);
            
            if (researchData != null && !string.IsNullOrEmpty(researchData.id))
            {
                if (!_researchEntries.ContainsKey(researchData.id))
                {
                    var entry = new ResearchEntry
                    {
                        researchId = researchData.id,
                        researchData = researchData,
                        researchState = new ResearchState { isCompleted = false }
                    };
                    _researchEntries.Add(researchData.id, entry);
                    loadedCount++;
                }
            }
        }
        
        Debug.Log($"[Research] Editor load completed: {loadedCount} research entries loaded.");
#else
        // 빌드 모드: Resources 폴더에서 로드
        ResearchData[] dataList = Resources.LoadAll<ResearchData>("Datas/Research");
        if (dataList != null && dataList.Length > 0)
        {
            foreach (var data in dataList)
            {
                if (string.IsNullOrEmpty(data.id) || _researchEntries.ContainsKey(data.id))
                    continue;

                var entry = new ResearchEntry
                {
                    researchId = data.id,
                    researchData = data,
                    researchState = new ResearchState { isCompleted = false }
                };
                _researchEntries.Add(data.id, entry);
            }
            Debug.Log($"[Research] Runtime load completed: {_researchEntries.Count} research entries loaded.");
        }
        else
        {
            Debug.LogWarning("[Research] No ResearchData found in Resources/Datas/Research. Make sure ResearchData files are placed in the Resources folder.");
        }
#endif
    }

    // ========================================================================
    // 2. 일일 로직 (RP 생산 및 특허)
    // ========================================================================

    /// <summary>
    /// 하루가 지날 때 호출 (GameDataManager.HandleDayChanged에서 연결)
    /// </summary>
    public void OnDayChanged()
    {
        // 1. 직원들에 의한 총 연구력 생산량 계산
        long generatedRP = CalculateDailyRPProduction();

        if (generatedRP <= 0) return;

        // 2. 특허 수익 모드인지 확인
        if (IsAutoPatentMode)
        {
            // 돈으로 환전 (RP -> Credit)
            long creditsEarned = generatedRP / RP_TO_CREDIT_RATIO;
            if (creditsEarned > 0)
            {
                _manager?.Finances?.AddCredit(creditsEarned);
                Debug.Log($"[Research] Patent Revenue: Sold {generatedRP} RP for {creditsEarned} Credits.");
            }
        }
        else
        {
            // RP 누적
            AddResearchPoints(generatedRP);
        }
    }

    /// <summary>
    /// 현재 고용된 연구원들의 효율을 기반으로 하루 생산 RP를 계산합니다.
    /// </summary>
    private long CalculateDailyRPProduction()
    {
        if (_manager?.Employee == null)
            return 0;

        // 모든 직원 중 Researcher 타입 찾기
        var allEmployees = _manager.Employee.GetAllEmployees();
        if (allEmployees == null || allEmployees.Count == 0)
            return 0;

        long totalRP = 0;

        foreach (var entry in allEmployees.Values)
        {
            if (entry?.employeeData == null || entry.employeeState == null)
                continue;

            // Researcher 타입인 직원만 계산
            if (entry.employeeData.role == EmployeeType.Researcher)
            {
                // 기본 생산량 (예: 1명당 100)
                long baseOutputPerHead = 100; 
                
                // 직원 효율 (만족도 등에 영향받음)
                float efficiency = entry.employeeState.currentEfficiency;

                // 할당된 직원 수만큼 생산
                long rpFromThisType = (long)(entry.employeeState.assignedCount * baseOutputPerHead * efficiency);
                totalRP += rpFromThisType;
            }
        }

        return totalRP;
    }

    // ========================================================================
    // 3. 연구 해금 로직 (즉시 완료)
    // ========================================================================

    /// <summary>
    /// 연구 해금을 시도합니다.
    /// </summary>
    public bool TryUnlockResearch(string researchId)
    {
        if (!_researchEntries.TryGetValue(researchId, out var entry))
        {
            Debug.LogWarning($"[Research] Invalid ID: {researchId}");
            return false;
        }

        // 1. 이미 완료된 연구인지 확인
        if (entry.researchState.isCompleted)
        {
            Debug.Log($"[Research] Already completed: {entry.researchData.displayName}");
            return false;
        }

        // 2. 선행 연구(Prerequisites) 확인
        if (!CheckPrerequisites(entry.researchData))
        {
            Debug.Log($"[Research] Prerequisites not met for: {entry.researchData.displayName}");
            return false;
        }

        // 3. 비용(RP) 확인
        if (CurrentResearchPoints < entry.researchData.researchPointCost)
        {
            Debug.Log($"[Research] Not enough RP. Need: {entry.researchData.researchPointCost}, Have: {CurrentResearchPoints}");
            return false;
        }

        // --- 해금 진행 ---

        // 4. RP 차감
        CurrentResearchPoints -= entry.researchData.researchPointCost;
        OnResearchPointsChanged?.Invoke();

        // 5. 상태 업데이트
        entry.researchState.isCompleted = true;

        // 6. 효과(Effect) 적용 [핵심 연동]
        ApplyResearchEffects(entry.researchData);

        Debug.Log($"[Research] UNLOCKED: {entry.researchData.displayName}");
        OnResearchUnlocked?.Invoke(researchId);

        return true;
    }

    /// <summary>
    /// 선행 연구들이 모두 완료되었는지 확인합니다.
    /// </summary>
    public bool CheckPrerequisites(ResearchData data)
    {
        if (data.prerequisiteIds == null || data.prerequisiteIds.Count == 0)
            return true;

        foreach (var preId in data.prerequisiteIds)
        {
            if (string.IsNullOrEmpty(preId))
                continue;

            if (_researchEntries.TryGetValue(preId, out var preEntry))
            {
                if (!preEntry.researchState.isCompleted) 
                    return false;
            }
            else
            {
                Debug.LogWarning($"[Research] Missing prerequisite ID: {preId}");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 연구 완료 시 효과를 적용합니다.
    /// </summary>
    private void ApplyResearchEffects(ResearchData data)
    {
        if (data.unlockEffects == null || data.unlockEffects.Count == 0)
            return;

        if (_manager?.Effect == null)
        {
            Debug.LogWarning("[Research] EffectDataHandler is null. Cannot apply research effects.");
            return;
        }

        foreach (var effect in data.unlockEffects)
        {
            if (effect == null)
                continue;

            // EffectDataHandler로 효과 전송
            _manager.Effect.AddEffect(effect);
        }
    }

    // ========================================================================
    // 4. 유틸리티
    // ========================================================================

    /// <summary>
    /// 연구력을 추가합니다.
    /// </summary>
    public void AddResearchPoints(long amount)
    {
        if (amount <= 0) return;
        
        CurrentResearchPoints += amount;
        OnResearchPointsChanged?.Invoke();
    }

    /// <summary>
    /// 연구 엔트리를 가져옵니다.
    /// </summary>
    public ResearchEntry GetResearchEntry(string id)
    {
        if (_researchEntries.TryGetValue(id, out var entry))
        {
            return entry;
        }
        return null;
    }
    
    /// <summary>
    /// 모든 연구 엔트리를 반환합니다.
    /// </summary>
    public List<ResearchEntry> GetAllResearchEntries()
    {
        return new List<ResearchEntry>(_researchEntries.Values);
    }

    /// <summary>
    /// 특정 연구가 완료되었는지 확인합니다.
    /// </summary>
    public bool IsResearchCompleted(string researchId)
    {
        if (_researchEntries.TryGetValue(researchId, out var entry))
        {
            return entry.researchState.isCompleted;
        }
        return false;
    }

    /// <summary>
    /// 특정 티어의 연구가 모두 완료되었는지 확인 (다음 티어 해금 조건용)
    /// </summary>
    public bool IsTierCompleted(int tier)
    {
        var tierResearches = _researchEntries.Values
            .Where(e => e.researchData != null && e.researchData.tier == tier)
            .ToList();

        if (tierResearches.Count == 0) 
            return true; // 해당 티어 연구가 없으면 완료로 간주

        return tierResearches.All(e => e.researchState.isCompleted);
    }

    /// <summary>
    /// 특정 티어의 연구 목록을 반환합니다.
    /// </summary>
    public List<ResearchEntry> GetResearchEntriesByTier(int tier)
    {
        return _researchEntries.Values
            .Where(e => e.researchData != null && e.researchData.tier == tier)
            .ToList();
    }
}

