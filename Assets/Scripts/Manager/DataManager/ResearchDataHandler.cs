using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 연구 데이터를 관리하고 연구력(RP) 생산, 연구 해금, 효과 적용을 담당하는 핸들러
/// </summary>
public class ResearchDataHandler : IDataHandlerEvents, ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    private Dictionary<string, ResearchEntry> _researchEntryList = new();
    private long _researchPoint;
    private bool _isAutoPatentMode = false;

    public long ResearchPoint => _researchPoint;
    public bool IsAutoPatentMode => _isAutoPatentMode;

    public event Action OnResearchPointsChanged;

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnResearchPointsChanged = null;
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

                if (_researchEntryList.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[ResearchDataHandler] Duplicate research ID: {data.id}");
                    continue;
                }

                ResearchEntry entry = new ResearchEntry
                {
                    data = data,
                    state = new ResearchState
                    {
                        isUnlocked = data.isDefaultUnlocked,
                        isCompleted = data.isDefaultUnlocked
                    }
                };
                _researchEntryList.Add(data.id, entry);
            }
        }

        if (_dataManager.InitialResearchData != null)
        {
            _researchPoint = _dataManager.InitialResearchData.initialResearchPoint;
        }

        RefreshUnlockedResearchStates();
    }

    /// <summary>
    /// 현재 완료/기본 해금 상태를 기준으로 모든 연구의 isUnlocked를 재계산합니다.
    /// </summary>
    public void RefreshUnlockedResearchStates()
    {
        foreach (ResearchEntry entry in _researchEntryList.Values)
        {
            bool unlockedByDefault = entry.data != null && entry.data.isDefaultUnlocked;
            entry.state.isUnlocked = unlockedByDefault || entry.state.isCompleted;
        }
        
        foreach (ResearchEntry entry in _researchEntryList.Values)
        {
            if (!entry.state.isCompleted || entry.data == null || entry.data.unlockResearchList == null)
                continue;

            foreach (ResearchData next in entry.data.unlockResearchList)
            {
                if (next == null || string.IsNullOrEmpty(next.id))
                    continue;

                if (_researchEntryList.TryGetValue(next.id, out ResearchEntry target))
                {
                    target.state.isUnlocked = true;
                }
            }
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
        if (!_researchEntryList.TryGetValue(researchId, out ResearchEntry entry)) return false;
        if (entry.state.isCompleted) return false;
        if (ResearchPoint < entry.data.researchPointCost) return false;

        _researchPoint -= entry.data.researchPointCost;
        entry.state.isCompleted = true;
        ApplyResearchEffects(entry.data);
        foreach(ResearchData researchData in entry.data.unlockResearchList)
        {
            _researchEntryList[researchData.id].state.isCompleted = false;
            _researchEntryList[researchData.id].state.isUnlocked = true;
        }

        OnResearchPointsChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 현재 완료된 모든 연구의 이펙트를 에셋 정의대로 다시 적용합니다. 세이브 로드·씬 진입 후 연구 상태와 효과를 맞출 때 사용합니다.
    /// </summary>
    public void ReapplyEffectsFromCompletedResearch()
    {
        foreach (ResearchEntry entry in _researchEntryList.Values)
        {
            if (!entry.state.isCompleted || entry.data == null)
                continue;

            ApplyResearchEffects(entry.data);
        }
    }

    /// <summary>
    /// 연구 완료 시 효과를 적용합니다.
    /// ScriptableObject 원본 데이터를 보호하기 위해 복제본을 생성하여 적용합니다.
    /// </summary>
    private void ApplyResearchEffects(ResearchData data)
    {
        _dataManager.Effect.ApplyEffectDefinitions(data?.effects);
    }

    /// <summary>
    /// 연구력을 추가합니다.
    /// </summary>
    public void AddResearchPoints(long amount)
    {
        if (amount <= 0) return;
        
        _researchPoint += amount;
    }

    /// <summary>
    /// 연구 엔트리를 가져옵니다.
    /// </summary>
    public ResearchEntry GetResearchEntry(string id)
    {
        if (_researchEntryList.TryGetValue(id, out ResearchEntry entry))
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
        return new List<ResearchEntry>(_researchEntryList.Values);
    }

    /// <summary>
    /// 특정 연구가 완료되었는지 확인합니다.
    /// </summary>
    public bool IsResearchCompleted(string researchId)
    {
        if (_researchEntryList.TryGetValue(researchId, out ResearchEntry entry))
        {
            return entry.state.isCompleted;
        }

        return false;
    }
}