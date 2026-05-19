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

        foreach (UnionRequestState.ResourceRequirementState resourceReq in request.resourceRequirements)
        {
            if (!_dataManager.Resource.ModifyResourceCount(resourceReq.resourceId, -resourceReq.count))
            {
                return false;
            }
        }

        foreach (string policyId in request.requiredPolicyIds)
        {
            PolicyEntry policyEntry = _dataManager.Policy.GetPolicyEntry(policyId);
            if (policyEntry != null && policyEntry.state.isActive)
            {
                continue;
            }

            if (!_dataManager.Policy.TrySetPolicyActive(policyId, true))
            {
                return false;
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

        if (template.requireResourceRequirementList != null)
        {
            long playerWealth = _dataManager.Finances != null ? _dataManager.Finances.Wealth : 0L;
            float resourceScale = Mathf.Max(0.1f, template.resourceScaleFactor);

            foreach (ResourceRequirement requirement in template.requireResourceRequirementList)
            {
                if (requirement?.resource == null || string.IsNullOrEmpty(requirement.resource.id))
                {
                    continue;
                }

                ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(requirement.resource.id);
                if (resourceEntry == null)
                {
                    continue;
                }

                int baseCount = Mathf.Max(1, requirement.count);
                int scaledCount = baseCount;
                if (playerWealth > 0 && resourceEntry.state.currentValue > 0)
                {
                    float allocatedWealth = playerWealth * resourceScale;
                    scaledCount = Mathf.RoundToInt(allocatedWealth / resourceEntry.state.currentValue);
                    scaledCount = Mathf.Max(baseCount, scaledCount);
                }

                newState.resourceRequirements.Add(new UnionRequestState.ResourceRequirementState
                {
                    resourceId = requirement.resource.id,
                    count = scaledCount
                });
            }
        }

        if (template.requirePolicyList != null)
        {
            foreach (PolicyData policy in template.requirePolicyList)
            {
                if (policy == null || string.IsNullOrEmpty(policy.id))
                {
                    continue;
                }

                PolicyEntry policyEntry = _dataManager.Policy.GetPolicyEntry(policy.id);
                if (policyEntry != null && policyEntry.state.isActive)
                {
                    continue;
                }

                newState.requiredPolicyIds.Add(policy.id);
            }
        }

        int minDays = _initialUnionMainEventData.minUnionRequestDeadlineDays;
        int maxDays = _initialUnionMainEventData.maxUnionRequestDeadlineDays;
        if (maxDays < minDays)
        {
            maxDays = minDays;
        }

        newState.remainingDays = UnityEngine.Random.Range(minDays, maxDays + 1);
        return newState;
    }

    private bool IsTemplateEligible(UnionRequestData template)
    {
        if (template == null)
        {
            return false;
        }

        bool hasCredit = template.requireCredit > 0;
        bool hasResources = template.requireResourceRequirementList != null
            && template.requireResourceRequirementList.Any(req => req != null && req.resource != null);
        bool hasUnmetPolicy = template.requirePolicyList != null
            && template.requirePolicyList.Any(policy =>
            {
                if (policy == null || string.IsNullOrEmpty(policy.id))
                {
                    return false;
                }

                PolicyEntry entry = _dataManager.Policy.GetPolicyEntry(policy.id);
                return entry == null || !entry.state.isActive;
            });

        return hasCredit || hasResources || hasUnmetPolicy;
    }

    private bool CanFulfillRequest(UnionRequestState request)
    {
        if (_dataManager.Finances == null || _dataManager.Resource == null || _dataManager.Policy == null)
        {
            return false;
        }

        if (request.requireCredit > 0 && _dataManager.Finances.Credit < request.requireCredit)
        {
            return false;
        }

        foreach (UnionRequestState.ResourceRequirementState resourceReq in request.resourceRequirements)
        {
            ResourceEntry entry = _dataManager.Resource.GetResourceEntry(resourceReq.resourceId);
            if (entry == null || entry.state.count < resourceReq.count)
            {
                return false;
            }
        }

        foreach (string policyId in request.requiredPolicyIds)
        {
            if (_dataManager.Policy.GetPolicyEntry(policyId) == null)
            {
                return false;
            }
        }

        return true;
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
