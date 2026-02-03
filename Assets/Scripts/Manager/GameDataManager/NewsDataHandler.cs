using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 뉴스 항목을 관리하는 핸들러.
/// </summary>
public class NewsDataHandler : IDataHandlerEvents, IDayChangeHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialNewsData _initialNewsData;

    private readonly Dictionary<string, NewsData> _newsDataDict;
    private readonly List<NewsData> _newsDataList = new List<NewsData>();
    private readonly List<NewsState> _activeNewsList = new List<NewsState>();

    private int _daysSinceLastNews = 0;
    private float _currentNewsChance = 0.0f;

    public event Action OnNewsChanged;

    public NewsDataHandler(DataManager dataManager, List<NewsData> newsDataList, InitialNewsData initialNewsData)
    {
        _dataManager = dataManager;
        _newsDataList = newsDataList;
        _initialNewsData = initialNewsData;
        _newsDataDict = new Dictionary<string, NewsData>();

        if (newsDataList != null)
        {
            foreach (NewsData data in newsDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (_newsDataDict.ContainsKey(data.id)) continue;
                _newsDataDict[data.id] = data;
            }
        }

        ResetNewsChance();
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnNewsChanged = null;
    }

    public void HandleDayChanged()
    {
        _daysSinceLastNews++;
        TryGenerateNews();

    }

    private void TryGenerateNews()
    {
        if (_activeNewsList.Count >= _initialNewsData.maxNewsItems)
        {
            ResetNewsChance();
            return;
        }

        _currentNewsChance += _initialNewsData.newsChanceIncrement;
        _daysSinceLastNews += 1;

        float randomValue = UnityEngine.Random.Range(0f, 1f);

        if (randomValue <= _currentNewsChance)
        {
            GenerateNews();
            ResetNewsChance();
        }
    }

    private void GenerateNews()
    {
        if (_newsDataList == null || _newsDataList.Count == 0) return;

        List<NewsData> availableNews = _newsDataList
            .Where(data => !_activeNewsList.Any(active => active.id == data.id))
            .ToList();

        NewsData selectedData = null;
        if (availableNews.Count > 0)
        {
            selectedData = availableNews[UnityEngine.Random.Range(0, availableNews.Count)];
        }

        NewsState newState = new NewsState(selectedData);
        _activeNewsList.Add(newState);

        foreach (EffectData effect in selectedData.effects)
        {
            string instanceId = GetInstanceIdForEffect(effect);
            float effectValue = GetRandomEffectValue(effect);
            _dataManager.Effect.ApplyEffect(effect, effectValue, instanceId);
        }

        OnNewsChanged?.Invoke();
    }

    private void ResetNewsChance()
    {
        _daysSinceLastNews = 0;
        _currentNewsChance = _initialNewsData.baseNewsChance;
    }

    private string GetInstanceIdForEffect(EffectData effect)
    {
        if (effect.targetType == EffectTargetType.Resource)
        {
            Dictionary<string, ResourceEntry> allResources = _dataManager.Resource.GetAllResources();
            return ProbabilityUtils.GetRandomKey(allResources);
        }

        return null;
    }

    /// <summary>
    /// 이펙트 타입에 따른 랜덤 값을 생성합니다.
    /// Resource_Price는 PercentAdd만 사용합니다 (Flat 없음).
    /// </summary>
    private float GetRandomEffectValue(EffectData effect)
    {
        switch (effect.statType)
        {
            case EffectStatType.Resource_Price:
                // PercentAdd 값 반환 (예: 0.2 = +20%, -0.2 = -20%)
                return UnityEngine.Random.Range(_initialNewsData.minResourcePricePer, _initialNewsData.maxResourcePricePer);
            default:
                return 0;
        }
    }
}
