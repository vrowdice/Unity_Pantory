using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정책(PolicyData) 목록과 활성 상태를 관리하는 핸들러.
/// </summary>
public class PolicyDataHandler : IDataHandlerEvents
{
    private const string PolicyEffectInstancePrefix = "FactoryPolicy:";

    private readonly DataManager _dataManager;
    private readonly InitialPolicyData _initialPolicyData;

    private readonly List<PolicyData> _policyDataList = new List<PolicyData>();
    private readonly Dictionary<string, PolicyEntry> _policyEntries = new Dictionary<string, PolicyEntry>();

    public event Action OnPolicyChanged;

    public PolicyDataHandler(DataManager dataManager, List<PolicyData> policyDataList, InitialPolicyData initialPolicyData)
    {
        _dataManager = dataManager;
        _initialPolicyData = initialPolicyData;
        _policyDataList = policyDataList ?? new List<PolicyData>();

        if (_initialPolicyData == null)
        {
            Debug.LogWarning("[PolicyDataHandler] InitialPolicyData is not assigned.");
        }

        foreach (PolicyData data in _policyDataList)
        {
            if (data == null || string.IsNullOrEmpty(data.id))
            {
                continue;
            }

            if (_policyEntries.ContainsKey(data.id))
            {
                Debug.LogWarning($"[PolicyDataHandler] Duplicate policy ID: {data.id}");
                continue;
            }

            _policyEntries[data.id] = new PolicyEntry
            {
                data = data,
                state = new PolicyState
                {
                    isActive = data.isActiveByDefault
                }
            };
        }
    }

    /// <summary>
    /// 활성 정책의 일일 크레딧 소모 합입니다. Finances 일일 정산에서 차감합니다.
    /// </summary>
    public long CalculateDailyPolicyCost()
    {
        float multiplier = GetDailyPolicyCostMultiplier();
        long totalCost = 0;
        foreach (PolicyEntry entry in _policyEntries.Values)
        {
            if (entry?.data == null || !entry.state.isActive)
            {
                continue;
            }

            if (entry.data.dailyCreditCost <= 0)
            {
                continue;
            }

            double scaled = entry.data.dailyCreditCost * (double)multiplier;
            long scaledCost = (long)Math.Round(scaled, MidpointRounding.AwayFromZero);
            if (scaledCost > 0)
            {
                totalCost += scaledCost;
            }
        }

        return totalCost;
    }

    public long CalculateScaledDailyPolicyCost(long baseDailyCost)
    {
        if (baseDailyCost <= 0)
        {
            return 0;
        }

        float multiplier = GetDailyPolicyCostMultiplier();
        double scaled = baseDailyCost * (double)multiplier;
        long scaledCost = (long)Math.Round(scaled, MidpointRounding.AwayFromZero);
        return Math.Max(0L, scaledCost);
    }

    private float GetDailyPolicyCostMultiplier()
    {
        if (_initialPolicyData == null || !_initialPolicyData.useWealthBasedDailyPolicyCostMultiplier)
        {
            return 1f;
        }

        long wealth = _dataManager?.Finances != null ? _dataManager.Finances.Wealth : 0L;
        long divisor = _initialPolicyData.policyCostWealthDivisor;
        if (divisor <= 0)
        {
            return 1f;
        }

        float raw = wealth <= 0 ? 0f : (float)((double)wealth / divisor);
        float min = _initialPolicyData.policyCostMultiplierMin;
        float max = _initialPolicyData.policyCostMultiplierMax;

        float clamped = Mathf.Max(0f, raw);
        if (max > 0f)
        {
            clamped = Mathf.Clamp(clamped, min, max);
        }
        else
        {
            clamped = Mathf.Max(min, clamped);
        }

        return clamped;
    }

    public PolicyData GetPolicyData(string policyId)
    {
        PolicyEntry entry = GetPolicyEntry(policyId);
        return entry?.data;
    }

    public PolicyEntry GetPolicyEntry(string policyId)
    {
        return _policyEntries.TryGetValue(policyId, out PolicyEntry entry) ? entry : null;
    }

    public Dictionary<string, PolicyEntry> GetAllPolicyEntries()
    {
        return new Dictionary<string, PolicyEntry>(_policyEntries);
    }

    public bool TrySetPolicyActive(string policyId, bool active)
    {
        if (!_policyEntries.TryGetValue(policyId, out PolicyEntry entry) || entry.data == null)
        {
            Debug.LogWarning($"[PolicyDataHandler] Unknown policy id: {policyId}");
            return false;
        }

        if (entry.state != null && entry.state.remainingMonths > 0)
        {
            return false;
        }

        if (entry.state.isActive == active)
        {
            return true;
        }

        entry.state.isActive = active;
        entry.state.remainingMonths = GetPolicyExpirationMonths();
        if (active)
        {
            ApplyPolicyEffects(entry.data);
        }
        else
        {
            RemovePolicyEffects(entry.data);
        }

        OnPolicyChanged?.Invoke();
        return true;
    }

    public void HandleMonthChanged()
    {
        bool changed = false;
        foreach (PolicyEntry entry in _policyEntries.Values)
        {
            if (entry?.state == null)
            {
                continue;
            }

            if (entry.state.remainingMonths > 0)
            {
                entry.state.remainingMonths--;
                if (entry.state.remainingMonths < 0)
                {
                    entry.state.remainingMonths = 0;
                }
                changed = true;
            }
        }

        if (changed)
        {
            OnPolicyChanged?.Invoke();
        }
    }

    private int GetPolicyExpirationMonths()
    {
        return _initialPolicyData != null ? Mathf.Max(0, _initialPolicyData.PolicyExpirationMonths) : 0;
    }

    /// <summary>
    /// 신규 세션 초기화 직후, 활성화된 정책의 이펙트를 한 번 적용합니다.
    /// </summary>
    public void ReapplyEffectsFromActivePolicies()
    {
        foreach (PolicyEntry entry in _policyEntries.Values)
        {
            if (entry?.data == null || !entry.state.isActive)
            {
                continue;
            }

            ApplyPolicyEffects(entry.data);
        }
    }

    /// <summary>
    /// 저장된 활성 여부에 맞춰 정책 이펙트를 전부 제거한 뒤, 활성 정책만 다시 적용합니다.
    /// </summary>
    public void SyncEffectsFromState()
    {
        foreach (PolicyEntry entry in _policyEntries.Values)
        {
            if (entry?.data == null)
            {
                continue;
            }

            RemovePolicyEffects(entry.data);
        }

        foreach (PolicyEntry entry in _policyEntries.Values)
        {
            if (entry?.data == null || !entry.state.isActive)
            {
                continue;
            }

            ApplyPolicyEffects(entry.data);
        }
    }

    private void ApplyPolicyEffects(PolicyData data)
    {
        if (data?.effects == null || _dataManager?.Effect == null)
        {
            return;
        }

        foreach (EffectData effect in data.effects)
        {
            if (effect == null)
            {
                continue;
            }

            string instanceId = ResolvePolicyEffectInstanceId(effect, data.id);
            _dataManager.Effect.ApplyEffect(effect, float.NaN, instanceId);
        }
    }

    private void RemovePolicyEffects(PolicyData data)
    {
        if (data?.effects == null || _dataManager?.Effect == null)
        {
            return;
        }

        foreach (EffectData effect in data.effects)
        {
            if (effect == null)
            {
                continue;
            }

            string instanceId = ResolvePolicyEffectInstanceId(effect, data.id);
            _dataManager.Effect.RemoveEffect(effect, instanceId);
        }
    }

    private static string ResolvePolicyEffectInstanceId(EffectData effect, string policyId)
    {
        // Global effects must be applied globally (instanceId=null),
        // otherwise they end up in instance-effects bucket and won't affect global calculations.
        if (effect.isGlobalEffect)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(effect.targetId))
        {
            return effect.targetId;
        }

        return PolicyEffectInstancePrefix + policyId;
    }

    public void ClearAllSubscriptions()
    {
        OnPolicyChanged = null;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        foreach (PolicyEntry entry in _policyEntries.Values)
        {
            if (entry?.data == null || string.IsNullOrEmpty(entry.data.id))
            {
                continue;
            }

            saveData.factoryPolicies.Add(new PolicyStateSaveData(entry.data.id, CloneState(entry.state)));
        }
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        foreach (PolicyStateSaveData policySave in saveData.factoryPolicies)
        {
            if (_policyEntries.TryGetValue(policySave.policyId, out PolicyEntry entry))
            {
                entry.state = policySave.state;
            }
        }

        SyncEffectsFromState();
        OnPolicyChanged?.Invoke();
    }

    private static PolicyState CloneState(PolicyState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<PolicyState>(json);
    }
}
