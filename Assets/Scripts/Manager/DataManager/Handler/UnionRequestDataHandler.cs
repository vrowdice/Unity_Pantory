using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 노조 요구(UnionRequestData) 템플릿과 연합 위기 중 활성 요구 인스턴스를 관리합니다.
/// </summary>
public class UnionRequestDataHandler : IDataHandlerEvents
{
    private readonly DataManager _dataManager;
    private readonly InitialUnionMainEventData _initialUnionMainEventData;

    private readonly Dictionary<string, UnionRequestData> _unionRequestDataDict;
    private readonly List<UnionRequestData> _unionRequestDataList = new List<UnionRequestData>();
    private readonly List<UnionRequestState> _activeUnionRequestList = new List<UnionRequestState>();

    private int _daysSinceLastUnionRequest;
    private float _currentUnionRequestChance;

    public event Action<UnionRequestState> OnUnionRequestChanged;

    public UnionRequestDataHandler(
        DataManager dataManager,
        List<UnionRequestData> unionRequestDataList,
        InitialUnionMainEventData initialUnionMainEventData)
    {
        _dataManager = dataManager;
        _unionRequestDataList = unionRequestDataList ?? new List<UnionRequestData>();
        _initialUnionMainEventData = initialUnionMainEventData;
        _unionRequestDataDict = new Dictionary<string, UnionRequestData>();

        foreach (UnionRequestData data in _unionRequestDataList)
        {
            if (data == null || string.IsNullOrEmpty(data.id))
            {
                continue;
            }

            if (_unionRequestDataDict.ContainsKey(data.id))
            {
                continue;
            }

            _unionRequestDataDict[data.id] = data;
        }

        ResetUnionRequestChance();
    }

    public void ResetForNewUnionChapter()
    {
        _activeUnionRequestList.Clear();
        ResetUnionRequestChance();
        OnUnionRequestChanged?.Invoke(null);
    }

    public void HandleUnionDayChanged()
    {
        if (_dataManager?.MainEvent == null)
        {
            return;
        }

        if (_dataManager.MainEvent.CurrentEventType != MainEventType.Union)
        {
            return;
        }

        UnionStateModule unionModule = _dataManager.MainEvent.UnionModule;
        if (unionModule == null || unionModule.IsComplete)
        {
            return;
        }

        ProcessExpiredRequests();
        TryGenerateUnionRequest();
    }

    public UnionRequestData GetUnionRequestData(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return _unionRequestDataDict.TryGetValue(id, out UnionRequestData data) ? data : null;
    }

    public Dictionary<string, UnionRequestData> GetAllUnionRequestData()
    {
        return new Dictionary<string, UnionRequestData>(_unionRequestDataDict);
    }

    public List<UnionRequestState> GetActiveUnionRequestList()
    {
        return new List<UnionRequestState>(_activeUnionRequestList);
    }

    public bool CanFulfillUnionRequest(UnionRequestState request)
    {
        return CanFulfillRequest(request);
    }

    public bool TryFulfillUnionRequest(UnionRequestState request)
    {
        if (request == null || request.isFulfilled || !_activeUnionRequestList.Contains(request))
        {
            return false;
        }

        if (!CanFulfillRequest(request))
        {
            return false;
        }

        if (request.requireCredit > 0 && !_dataManager.Finances.ModifyCredit(-request.requireCredit))
        {
            return false;
        }

        if (HasResourceRequirement(request)
            && !_dataManager.Resource.ModifyResourceCount(request.requireResourceId, -request.requireResourceCount))
        {
            return false;
        }

        if (HasPolicyRequirement(request))
        {
            PolicyEntry policyEntry = _dataManager.Policy.GetPolicyEntry(request.requiredPolicyId);
            if (policyEntry == null || !policyEntry.state.isActive)
            {
                if (!_dataManager.Policy.TrySetPolicyActive(request.requiredPolicyId, true))
                {
                    return false;
                }
            }
        }

        UnionRequestData template = GetUnionRequestData(request.id);
        request.isFulfilled = true;
        UnionStateModule unionModule = _dataManager.MainEvent?.UnionModule;
        if (unionModule != null && template != null && template.rewardCohesion > 0)
        {
            unionModule.AddCohesionProgress(template.rewardCohesion);
        }

        _activeUnionRequestList.Remove(request);
        OnUnionRequestChanged?.Invoke(request);
        return true;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        foreach (UnionRequestState state in _activeUnionRequestList)
        {
            saveData.activeUnionRequests.Add(CloneState(state));
        }

        saveData.unionRequestDaysSinceLast = _daysSinceLastUnionRequest;
        saveData.unionRequestCurrentChance = _currentUnionRequestChance;
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        _activeUnionRequestList.Clear();
        foreach (UnionRequestState state in saveData.activeUnionRequests)
        {
            _activeUnionRequestList.Add(CloneState(state));
        }

        _daysSinceLastUnionRequest = saveData.unionRequestDaysSinceLast;
        _currentUnionRequestChance = saveData.unionRequestCurrentChance;
    }

    public void ClearAllSubscriptions()
    {
        OnUnionRequestChanged = null;
    }

    private void ProcessExpiredRequests()
    {
        for (int i = _activeUnionRequestList.Count - 1; i >= 0; i--)
        {
            UnionRequestState request = _activeUnionRequestList[i];
            request.remainingDays--;
            if (request.remainingDays <= 0)
            {
                _activeUnionRequestList.RemoveAt(i);
                OnUnionRequestChanged?.Invoke(request);
            }
        }
    }

    private void TryGenerateUnionRequest()
    {
        if (_initialUnionMainEventData == null)
        {
            return;
        }

        if (_activeUnionRequestList.Count >= _initialUnionMainEventData.maxActiveUnionRequests)
        {
            ResetUnionRequestChance();
            return;
        }

        if (_unionRequestDataDict.Count == 0)
        {
            return;
        }

        _currentUnionRequestChance += _initialUnionMainEventData.unionRequestChanceIncrement;
        _daysSinceLastUnionRequest++;

        float randomValue = UnityEngine.Random.Range(0f, 1f);
        if (randomValue > _currentUnionRequestChance
            && _daysSinceLastUnionRequest < _initialUnionMainEventData.guaranteedUnionRequestDay)
        {
            return;
        }

        GenerateUnionRequest();
        ResetUnionRequestChance();
    }

    private void GenerateUnionRequest()
    {
        List<UnionRequestData> availableTemplates = _unionRequestDataDict.Values
            .Where(data => data != null && !_activeUnionRequestList.Any(active => active.id == data.id))
            .Where(IsTemplateEligible)
            .ToList();

        if (availableTemplates.Count == 0)
        {
            return;
        }

        UnionRequestData selectedTemplate = availableTemplates[UnityEngine.Random.Range(0, availableTemplates.Count)];
        UnionRequestState newState = CreateUnionRequestInstance(selectedTemplate);
        if (newState == null)
        {
            return;
        }

        _activeUnionRequestList.Add(newState);
        OnUnionRequestChanged?.Invoke(newState);
    }

    private UnionRequestState CreateUnionRequestInstance(UnionRequestData template)
    {
        if (template == null)
        {
            return null;
        }

        UnionRequestState newState = new UnionRequestState(template);
        int employeeCount = _dataManager.Employee != null
            ? _dataManager.Employee.GetTotalEmployeeCount()
            : 1;
        int employeeBaseline = _initialUnionMainEventData != null
            ? Mathf.Max(1, _initialUnionMainEventData.unionEmployeeCountToStart)
            : 1;
        float employeeScale = employeeCount / (float)employeeBaseline;

        long scaledCredit = template.requireCredit;
        if (scaledCredit > 0)
        {
            float creditScale = Mathf.Max(0.1f, employeeScale * template.creditScaleFactor);
            scaledCredit = (long)Math.Round(scaledCredit * creditScale, MidpointRounding.AwayFromZero);
            scaledCredit = Math.Max(1L, scaledCredit);
        }

        newState.requireCredit = scaledCredit;

        if (template.requireResource != null
            && template.requireResource.resource != null
            && !string.IsNullOrEmpty(template.requireResource.resource.id))
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(template.requireResource.resource.id);
            if (resourceEntry != null)
            {
                long playerWealth = _dataManager.Finances != null ? _dataManager.Finances.Wealth : 0L;
                float resourceScale = Mathf.Max(0.1f, template.resourceScaleFactor);
                int baseCount = Mathf.Max(1, template.requireResource.count);
                int scaledCount = baseCount;
                if (playerWealth > 0 && resourceEntry.state.currentValue > 0)
                {
                    float allocatedWealth = playerWealth * resourceScale;
                    scaledCount = Mathf.RoundToInt(allocatedWealth / resourceEntry.state.currentValue);
                    scaledCount = Mathf.Max(baseCount, scaledCount);
                }

                newState.requireResourceId = template.requireResource.resource.id;
                newState.requireResourceCount = scaledCount;
            }
        }

        if (template.requirePolicy != null && !string.IsNullOrEmpty(template.requirePolicy.id))
        {
            PolicyEntry policyEntry = _dataManager.Policy.GetPolicyEntry(template.requirePolicy.id);
            if (policyEntry == null || !policyEntry.state.isActive)
            {
                newState.requiredPolicyId = template.requirePolicy.id;
            }
        }

        int minDays = _initialUnionMainEventData.minUnionRequestDeadlineDays;
        int maxDays = _initialUnionMainEventData.maxUnionRequestDeadlineDays;
        if (maxDays < minDays)
        {
            maxDays = minDays;
        }

        newState.remainingDays = UnityEngine.Random.Range(minDays, maxDays + 1);

        if (!HasAnyRequirement(newState))
        {
            return null;
        }

        return newState;
    }

    private bool IsTemplateEligible(UnionRequestData template)
    {
        if (template == null)
        {
            return false;
        }

        bool hasCredit = template.requireCredit > 0;
        bool hasResources = template.requireResource != null
            && template.requireResource.resource != null
            && !string.IsNullOrEmpty(template.requireResource.resource.id);
        bool hasUnmetPolicy = false;
        if (template.requirePolicy != null && !string.IsNullOrEmpty(template.requirePolicy.id))
        {
            PolicyEntry entry = _dataManager.Policy.GetPolicyEntry(template.requirePolicy.id);
            hasUnmetPolicy = entry == null || !entry.state.isActive;
        }

        return hasCredit || hasResources || hasUnmetPolicy;
    }

    private bool CanFulfillRequest(UnionRequestState request)
    {
        if (_dataManager.Finances == null || _dataManager.Resource == null || _dataManager.Policy == null)
        {
            return false;
        }

        if (!HasAnyRequirement(request))
        {
            return false;
        }

        if (request.requireCredit > 0 && _dataManager.Finances.Credit < request.requireCredit)
        {
            return false;
        }

        if (HasResourceRequirement(request))
        {
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(request.requireResourceId);
            if (entry == null || entry.state.count < request.requireResourceCount)
            {
                return false;
            }
        }

        if (HasPolicyRequirement(request))
        {
            PolicyEntry entry = _dataManager.Policy.GetPolicyEntry(request.requiredPolicyId);
            if (entry == null)
            {
                return false;
            }

            if (!entry.state.isActive && entry.state.remainingMonths > 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasAnyRequirement(UnionRequestState request)
    {
        return request != null
            && (request.requireCredit > 0
                || HasResourceRequirement(request)
                || HasPolicyRequirement(request));
    }

    private static bool HasResourceRequirement(UnionRequestState request)
    {
        return request != null
            && !string.IsNullOrEmpty(request.requireResourceId)
            && request.requireResourceCount > 0;
    }

    private static bool HasPolicyRequirement(UnionRequestState request)
    {
        return request != null && !string.IsNullOrEmpty(request.requiredPolicyId);
    }

    private void ResetUnionRequestChance()
    {
        if (_initialUnionMainEventData == null)
        {
            _currentUnionRequestChance = 0f;
            _daysSinceLastUnionRequest = 0;
            return;
        }

        _currentUnionRequestChance = _initialUnionMainEventData.baseUnionRequestChance;
        _daysSinceLastUnionRequest = 0;
    }

    private static UnionRequestState CloneState(UnionRequestState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<UnionRequestState>(json);
    }
}
