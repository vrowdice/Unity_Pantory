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
    private readonly DataManager _dataManager;
    private Dictionary<string, ResearchEntry> _researchEntries = new();
    private long _researchPoint;
    private bool _isAutoPatentMode = false;

    public long ResearchPoint => _researchPoint;
    public bool IsAutoPatentMode => _isAutoPatentMode;

    public event Action OnResearchPointsChanged;
    public event Action<string> OnResearchUnlocked;

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnResearchPointsChanged = null;
        OnResearchUnlocked = null;
    }

    public ResearchDataHandler(DataManager dataManager, List<ResearchData> researchDataList = null)
    {
        _dataManager = dataManager;
        
        if (researchDataList != null && researchDataList.Count > 0)
        {
            foreach (ResearchData data in researchDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id))
                    continue;

                if (_researchEntries.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[Research] Duplicate research ID: {data.id}");
                    continue;
                }

                ResearchEntry entry = new ResearchEntry
                {
                    data = data,
                    state = new ResearchState { isCompleted = false }
                };
                _researchEntries.Add(data.id, entry);
            }
        }

        if (_dataManager.InitialResearchData != null)
        {
            _researchPoint = _dataManager.InitialResearchData.initialResearchPoint;
        }
    }

    /// <summary>
    /// 하루가 지날 때 호출 (GameDataManager.HandleDayChanged에서 연결)
    /// </summary>
    public void HandleDayChanged()
    {
        long generatedRP = CalculateDailyRPProduction();

        if (generatedRP <= 0) return;

        if (IsAutoPatentMode)
        {
            _dataManager.Finances.ModifyCredit(generatedRP);
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
        EmployeeEntry employee = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Researcher);
        long totalRP = (long)(employee.state.count * _dataManager.InitialEmployeeData.researchPointsPerResearcher * employee.state.currentEfficiency);
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
        if (entry.state.isCompleted)
        {
            Debug.Log($"[Research] Already completed: {entry.data.displayName}");
            return false;
        }

        // 2. 선행 연구(Prerequisites) 확인
        if (!CheckPrerequisites(entry.data))
        {
            Debug.Log($"[Research] Prerequisites not met for: {entry.data.displayName}");
            return false;
        }

        // 3. 비용(RP) 확인
        if (ResearchPoint < entry.data.researchPointCost)
        {
            Debug.Log($"[Research] Not enough RP. Need: {entry.data.researchPointCost}, Have: {ResearchPoint}");
            return false;
        }

        // 4. RP 차감
        _researchPoint -= entry.data.researchPointCost;
        OnResearchPointsChanged?.Invoke();
        entry.state.isCompleted = true;
        ApplyResearchEffects(entry.data);
        Debug.Log($"[Research] UNLOCKED: {entry.data.displayName}");
        OnResearchUnlocked?.Invoke(researchId);

        return true;
    }

    /// <summary>
    /// 선행 연구들이 모두 완료되었는지 확인합니다.
    /// </summary>
    public bool CheckPrerequisites(ResearchData data)
    {
        if (data.prerequisiteResearchs == null || data.prerequisiteResearchs.Count == 0)
            return true;

        foreach (ResearchData item in data.prerequisiteResearchs)
        {
            foreach(KeyValuePair<string, ResearchEntry> entry in _researchEntries)
            {
                if(item == entry.Value.data)
                {
                    if(!entry.Value.state.isCompleted)
                    {
                        return false; 
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 연구 완료 시 효과를 적용합니다.
    /// ScriptableObject 원본 데이터를 보호하기 위해 복제본을 생성하여 적용합니다.
    /// </summary>
    private void ApplyResearchEffects(ResearchData data)
    {
        if (data.effects == null || data.effects.Count == 0)
        {
            return;
        }

        foreach (EffectData originalEffect in data.effects)
        {
            _dataManager.Effect.ApplyEffect(originalEffect);
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
            return entry.state.isCompleted;
        }

        return false;
    }

    /// <summary>
    /// 특정 티어의 연구 목록을 반환합니다.
    /// </summary>
    public List<ResearchEntry> GetResearchEntriesByTier(int tier)
    {
        List<ResearchEntry> researchs = new List<ResearchEntry>();

        foreach(KeyValuePair<string ,ResearchEntry> item in _researchEntries)
        {
            if(item.Value.data.tier == tier)
            {
                researchs.Add(item.Value);
            }
        }

        return researchs;
    }
}