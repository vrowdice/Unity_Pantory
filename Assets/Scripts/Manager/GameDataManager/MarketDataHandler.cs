using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 행위자 데이터를 관리하고 하루 단위로 공급/수요를 시뮬레이션해 자원 가격에 반영하는 핸들러
/// </summary>
public class MarketDataHandler
{
    private readonly Dictionary<string, MarketActorEntry> _actors = new();
    private readonly GameDataManager _gameDataManager;

    public event Action OnMarketUpdated;

    public MarketDataHandler(GameDataManager manager)
    {
        _gameDataManager = manager;
        AutoLoadAllActors();
    }

    public void RegisterActor(MarketActorData data)
    {
        if (data == null || string.IsNullOrEmpty(data.id))
        {
            Debug.LogWarning("[MarketDataHandler] Invalid actor data.");
            return;
        }

        if (_actors.ContainsKey(data.id))
        {
            Debug.LogWarning($"[MarketDataHandler] Actor already registered: {data.id}");
            return;
        }

        _actors[data.id] = new MarketActorEntry(data);
    }

    public void RegisterActors(IEnumerable<MarketActorData> actors)
    {
        if (actors == null)
        {
            return;
        }

        foreach (var actor in actors)
        {
            RegisterActor(actor);
        }
    }

    public Dictionary<string, MarketActorEntry> GetAllActors()
    {
        return new Dictionary<string, MarketActorEntry>(_actors);
    }

    public void TickDailyMarket()
    {
        if (_gameDataManager?.Resource == null || _actors.Count == 0)
        {
            return;
        }

        Dictionary<string, ResourceEntry> resourceSnapshot = _gameDataManager.Resource.GetAllResources();
        if (resourceSnapshot == null || resourceSnapshot.Count == 0)
        {
            return;
        }

        var totalSupply = new Dictionary<string, float>();
        var totalDemand = new Dictionary<string, float>();

        foreach (var entry in _actors.Values)
        {
            RefreshActor(entry);
            SimulateProvider(entry, resourceSnapshot, totalSupply);
            SimulateConsumer(entry, resourceSnapshot, totalDemand);
        }

        ApplyPriceAdjustments(resourceSnapshot, totalSupply, totalDemand);
        OnMarketUpdated?.Invoke();
    }

    private void RefreshActor(MarketActorEntry entry)
    {
        if (entry?.data == null)
        {
            return;
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            RefreshConsumer(entry);
        }

        if (entry.data.roles.HasFlag(MarketRoleFlags.Provider))
        {
            RefreshProvider(entry);
        }
    }

    private void RefreshConsumer(MarketActorEntry entry)
    {
        var profile = entry.data.consumerProfile;
        var state = entry.state.consumer;
        if (profile == null || state == null)
        {
            return;
        }

        if (!profile.persistentOrders || state.currentBudget <= 0f)
        {
            state.currentBudget = profile.budgetRange.GetRandomBudget();
        }

        state.desireTimer = Mathf.Max(0f, state.desireTimer - 1f);
        if (state.desireTimer <= 0f)
        {
            state.desireTimer = Mathf.Max(1f, profile.patienceSeconds / 86400f); // 하루 단위로 환산
        }
    }

    private void RefreshProvider(MarketActorEntry entry)
    {
        var state = entry.state.provider;
        if (state == null)
        {
            return;
        }

        state.cooldownTimer = Mathf.Max(0f, state.cooldownTimer - 1f);
        state.productionProgress = 0f;
        state.priceDelta = 0f;
    }

    private void SimulateProvider(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalSupply)
    {
        if (!entry.data.roles.HasFlag(MarketRoleFlags.Provider))
        {
            return;
        }

        var profile = entry.data.providerProfile;
        var state = entry.state.provider;
        if (profile == null || state == null)
        {
            return;
        }

        foreach (var preference in profile.outputs)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id))
            {
                continue;
            }

            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry))
            {
                continue;
            }

            float baseline = resourceEntry.resourceState.GetEffectiveBaseline(resourceEntry.resourceData.baseValue);
            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
            float priceRatio = currentPrice / baseline;
            float priceSignal = 1f + (priceRatio - 1f) * preference.priceSensitivity;
            priceSignal = Mathf.Max(0.1f, priceSignal);

            float quantity = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float batchModifier = profile.allowBatchSelling ? 1f : 0.5f;
            float output = quantity * priceSignal * profile.basePriceModifier * batchModifier;

            AddToMap(totalSupply, preference.resource.id, output);
            UpdateStock(state.stocks, preference.resource, Mathf.RoundToInt(output));

            state.priceDelta = priceSignal - 1f;
        }
    }

    private void SimulateConsumer(
        MarketActorEntry entry,
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> totalDemand)
    {
        if (!entry.data.roles.HasFlag(MarketRoleFlags.Consumer))
        {
            return;
        }

        var profile = entry.data.consumerProfile;
        var state = entry.state.consumer;
        if (profile == null || state == null)
        {
            return;
        }

        foreach (var preference in profile.desiredResources)
        {
            if (preference?.resource == null || string.IsNullOrEmpty(preference.resource.id))
            {
                continue;
            }

            if (!resources.TryGetValue(preference.resource.id, out var resourceEntry))
            {
                continue;
            }

            float baseline = resourceEntry.resourceState.GetEffectiveBaseline(resourceEntry.resourceData.baseValue);
            float currentPrice = Mathf.Max(0.01f, resourceEntry.resourceState.currentValue);
            float priceRatio = currentPrice / baseline;
            float priceSignal = 1f + (1f - priceRatio) * preference.priceSensitivity;
            priceSignal = Mathf.Max(0.1f, priceSignal);

            float appetite = SampleQuantity(preference.desiredMin, preference.desiredMax);
            float urgencyBoost = 1f + preference.urgency;
            float desiredAmount = appetite * priceSignal * urgencyBoost;

            if (desiredAmount <= 0f)
            {
                continue;
            }

            float finalAmount = desiredAmount;
            if (profile.allowBulkBuying && state.currentBudget > 0f)
            {
                float affordable = state.currentBudget / currentPrice;
                finalAmount = Mathf.Min(desiredAmount, affordable);
                state.currentBudget = Mathf.Max(0f, state.currentBudget - finalAmount * currentPrice);
                UpdateStock(state.holdings, preference.resource, Mathf.RoundToInt(finalAmount));
            }

            AddToMap(totalDemand, preference.resource.id, finalAmount);
        }
    }

    private void ApplyPriceAdjustments(
        Dictionary<string, ResourceEntry> resources,
        Dictionary<string, float> supply,
        Dictionary<string, float> demand)
    {
        foreach (var entry in resources.Values)
        {
            if (entry?.resourceData == null || entry.resourceState == null)
            {
                continue;
            }

            string resourceId = entry.resourceData.id;
            float totalSupply = supply.TryGetValue(resourceId, out var s) ? s : 0f;
            float totalDemand = demand.TryGetValue(resourceId, out var d) ? d : 0f;

            entry.resourceState.lastSupply = totalSupply;
            entry.resourceState.lastDemand = totalDemand;

            float imbalance = totalDemand - totalSupply;
            float turnover = Mathf.Max(1f, totalSupply + totalDemand);
            float normalizedImbalance = Mathf.Clamp(imbalance / turnover, -1f, 1f);

            entry.resourceState.lastImbalance = imbalance;
            entry.resourceState.lastNormalizedImbalance = normalizedImbalance;

            float baseline = entry.resourceState.GetEffectiveBaseline(entry.resourceData.baseValue);
            entry.resourceState.AdvanceOneDay();

            float currentPrice = Mathf.Max(0.01f, entry.resourceState.currentValue);
            float deviation = baseline > 0.01f ? (currentPrice - baseline) / baseline : 0f;

            float marketPressure = normalizedImbalance * entry.resourceData.marketSensitivity;
            float meanReversion = -deviation * entry.resourceData.meanReversionStrength;
            float scarcity = Mathf.Max(0f, normalizedImbalance) * entry.resourceData.scarcityWeight;

            float rate = marketPressure + meanReversion + scarcity;
            rate = Mathf.Clamp(rate, -entry.resourceData.maxDailySwing, entry.resourceData.maxDailySwing);

            entry.resourceState.priceChangeRate = rate;
            entry.resourceState.currentValue = Mathf.Max(0.01f, currentPrice * (1f + rate));
        }
    }

    private static void AddToMap(Dictionary<string, float> map, string resourceId, float value)
    {
        if (map == null || string.IsNullOrEmpty(resourceId) || value == 0f)
        {
            return;
        }

        if (map.TryGetValue(resourceId, out var current))
        {
            map[resourceId] = current + value;
        }
        else
        {
            map[resourceId] = value;
        }
    }

    private static void UpdateStock(List<ResourceStock> stocks, ResourceData resource, long delta)
    {
        if (stocks == null || resource == null || delta == 0)
        {
            return;
        }

        for (int i = 0; i < stocks.Count; i++)
        {
            if (stocks[i]?.resource == resource)
            {
                stocks[i].amount = Math.Max(0, stocks[i].amount + delta);
                return;
            }
        }

        stocks.Add(new ResourceStock
        {
            resource = resource,
            amount = Math.Max(0, delta)
        });
    }

    private static float SampleQuantity(long min, long max)
    {
        if (max < min)
        {
            (min, max) = (max, min);
        }

        float sampleMin = Mathf.Max(0f, min);
        float sampleMax = Mathf.Max(sampleMin, max);

        if (Mathf.Approximately(sampleMin, sampleMax))
        {
            return sampleMin;
        }

        return UnityEngine.Random.Range(sampleMin, sampleMax + 1f);
    }

    private void AutoLoadAllActors()
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MarketActorData");
        int count = 0;

        foreach (var guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<MarketActorData>(assetPath);
            if (data == null)
            {
                continue;
            }

            RegisterActor(data);
            count++;
        }

        Debug.Log($"[MarketDataHandler] Auto load completed: {count} actors registered.");
#else
        Debug.LogWarning("[MarketDataHandler] Auto load is only available in the editor. Register actors manually at runtime.");
#endif
    }
}

