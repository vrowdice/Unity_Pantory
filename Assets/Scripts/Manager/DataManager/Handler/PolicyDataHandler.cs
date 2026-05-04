using System;
using System.Collections.Generic;
using UnityEngine;

public class PolicyDataHandler : IDataHandlerEvents
{
    private const string PolicyEffectInstancePrefix = "FactoryPolicy:";

    private readonly DataManager _dataManager;
    private readonly InitialPolicyData _initialFactoryPolicyData;
    private readonly Dictionary<string, PolicyEntry> _policies = new Dictionary<string, PolicyEntry>();

    public event Action OnFactoryRegulationChanged;

    public PolicyDataHandler(DataManager dataManager, List<PolicyData> policyDataList, InitialPolicyData initialData)
    {
        _dataManager = dataManager;
        _initialFactoryPolicyData = initialData;

        if (_initialFactoryPolicyData == null)
        {
            Debug.LogWarning("[FactoryPolicyDataHandler] InitialPolicyData is not assigned.");
        }

        if (policyDataList == null)
        {
            return;
        }

        foreach (PolicyData data in policyDataList)
        {
            if (data == null || string.IsNullOrEmpty(data.id))
            {
                continue;
            }

            if (_policies.ContainsKey(data.id))
            {
                Debug.LogWarning($"[FactoryPolicyDataHandler] Duplicate policy ID: {data.id}");
                continue;
            }

            PolicyEntry entry = new PolicyEntry
            {
                data = data,
                state = new PolicyState
                {
                    isActive = data.isActiveByDefault
                }
            };
            _policies.Add(data.id, entry);
        }
    }

    public PolicyEntry GetPolicyEntry(string policyId)
    {
        return _policies.TryGetValue(policyId, out PolicyEntry entry) ? entry : null;
    }

    public Dictionary<string, PolicyEntry> GetAllPolicyEntries()
    {
        return new Dictionary<string, PolicyEntry>(_policies);
    }

    public bool TrySetPolicyActive(string policyId, bool active)
    {
        if (!_policies.TryGetValue(policyId, out PolicyEntry entry) || entry.data == null)
        {
            Debug.LogWarning($"[FactoryPolicyDataHandler] Unknown policy id: {policyId}");
            return false;
        }

        if (entry.state.isActive == active)
        {
            return true;
        }

        entry.state.isActive = active;
        if (active)
        {
            ApplyPolicyEffects(entry.data);
        }
        else
        {
            RemovePolicyEffects(entry.data);
        }

        OnFactoryRegulationChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 신규 세션 초기화 직후, 활성화된 정책의 이펙트를 한 번 적용합니다.
    /// </summary>
    public void ReapplyEffectsFromActivePolicies()
    {
        foreach (PolicyEntry entry in _policies.Values)
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
        foreach (PolicyEntry entry in _policies.Values)
        {
            if (entry?.data == null)
            {
                continue;
            }

            RemovePolicyEffects(entry.data);
        }

        foreach (PolicyEntry entry in _policies.Values)
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
        if (!string.IsNullOrEmpty(effect.targetId))
        {
            return effect.targetId;
        }

        return PolicyEffectInstancePrefix + policyId;
    }

    public void ClearAllSubscriptions()
    {
        OnFactoryRegulationChanged = null;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        foreach (PolicyEntry entry in _policies.Values)
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
            if (_policies.TryGetValue(policySave.policyId, out PolicyEntry entry))
            {
                entry.state = policySave.state;
            }
        }

        SyncEffectsFromState();
        OnFactoryRegulationChanged?.Invoke();
    }

    private static PolicyState CloneState(PolicyState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<PolicyState>(json);
    }
}
