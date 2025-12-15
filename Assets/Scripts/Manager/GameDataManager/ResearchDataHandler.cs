using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// 연구 데이터를 관리하고 연구력(RP) 생산, 연구 해금, 효과 적용을 담당하는 핸들러
/// </summary>
public class ResearchDataHandler
{
    private readonly GameDataManager _gameDataManager;
    private Dictionary<string, ResearchEntry> _researchEntries = new();
    private long _researchPoint;
    private bool _isAutoPatentMode = false;

    public long ResearchPoint => _researchPoint;
    public bool IsAutoPatentMode => _isAutoPatentMode;

    public event Action OnResearchPointsChanged;
    public event Action<string> OnResearchUnlocked;

    public ResearchDataHandler(GameDataManager manager, List<ResearchData> researchDataList = null)
    {
        _gameDataManager = manager;
        
        if (researchDataList != null && researchDataList.Count > 0)
        {
            // 리스트에서 딕셔너리로 등록
            foreach (var data in researchDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id))
                    continue;

                if (_researchEntries.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[Research] Duplicate research ID: {data.id}");
                    continue;
                }

                var entry = new ResearchEntry
                {
                    researchId = data.id,
                    researchData = data,
                    researchState = new ResearchState { isCompleted = false }
                };
                _researchEntries.Add(data.id, entry);
            }
            Debug.Log($"[Research] Initialized with {researchDataList.Count} research entries from list.");
        }
    }

    /// <summary>
    /// 하루가 지날 때 호출 (GameDataManager.HandleDayChanged에서 연결)
    /// </summary>
    public void OnDayChanged()
    {
        long generatedRP = CalculateDailyRPProduction();

        if (generatedRP <= 0) return;

        if (IsAutoPatentMode)
        {
            _gameDataManager.Finances.AddCredit(generatedRP);
        }
        else
        {
            AddResearchPoints(generatedRP);
        }
    }

    /// <summary>
    /// 현재 고용된 연구원들의 효율을 기반으로 하루 생산 RP를 계산합니다.
    /// </summary>
    public long CalculateDailyRPProduction()
    {
        EmployeeEntry employee = _gameDataManager.Employee.GetEmployeeEntry(EmployeeType.Researcher);
        long totalRP = (long)(employee.state.count * _gameDataManager.InitialEmployeeData.researchPointsPerResearcher * employee.state.currentEfficiency);
        return totalRP;
    }

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
        if (ResearchPoint < entry.researchData.researchPointCost)
        {
            Debug.Log($"[Research] Not enough RP. Need: {entry.researchData.researchPointCost}, Have: {ResearchPoint}");
            return false;
        }

        // 4. RP 차감
        _researchPoint -= entry.researchData.researchPointCost;
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
        if (data.appleyEffects == null || data.appleyEffects.Count == 0)
            return;

        foreach (EffectState effect in data.appleyEffects)
        {
            if (effect == null)
                continue;

            _gameDataManager.Effect.ApplyEffect(effect);
        }
    }

    /// <summary>
    /// 연구력을 추가합니다.
    /// </summary>
    public void AddResearchPoints(long amount)
    {
        if (amount <= 0) return;
        
        _researchPoint += amount;
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