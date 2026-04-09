using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 뉴스 항목을 관리하는 핸들러.
/// </summary>
public class NewsDataHandler : IDataHandlerEvents, ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialNewsData _initialNewsData;

    private readonly Dictionary<string, NewsData> _newsDataDict;
    private readonly List<NewsData> _newsDataList = new List<NewsData>();
    private readonly List<NewsState> _activeNewsList = new List<NewsState>();

    private int _daysSinceLastNews = 0;
    private float _currentNewsChance = 0.0f;

    public event Action<NewsState> OnNewsChanged;

    public NewsDataHandler(DataManager dataManager, List<NewsData> newsDataList, InitialNewsData initialNewsData)
    {
        _dataManager = dataManager;
        _newsDataList = newsDataList ?? new List<NewsData>();
        _initialNewsData = initialNewsData;
        _newsDataDict = new Dictionary<string, NewsData>();

        foreach (NewsData data in _newsDataList)
        {
            if (data == null || string.IsNullOrEmpty(data.id)) continue;
            if (_newsDataDict.ContainsKey(data.id)) continue;
            _newsDataDict[data.id] = data;
        }

        ResetNewsChance();
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

        if (randomValue <= _currentNewsChance || _daysSinceLastNews >= _initialNewsData.guaranteedNewsDay)
        {
            GenerateNews();
            ResetNewsChance();
        }
    }

    private void GenerateNews()
    {
        if (_newsDataList.Count == 0) return;

        List<NewsData> availableNews = _newsDataList
            .Where(data => data != null && !_activeNewsList.Any(active => active.id == data.id))
            .ToList();

        if (availableNews.Count == 0)
        {
            ResetNewsChance();
            return;
        }

        NewsData selectedData = availableNews[UnityEngine.Random.Range(0, availableNews.Count)];
        NewsState newState = new NewsState(selectedData);
        _activeNewsList.Add(newState);

        if (selectedData.effects != null)
        {
            foreach (EffectData effect in selectedData.effects)
            {
                if (effect == null) continue;

                float additionalRandom = UnityEngine.Random.Range(-_initialNewsData.maxRandomRange, _initialNewsData.maxRandomRange);
                float finalValue = effect.value + additionalRandom;

                if (effect.value > 0)
                    finalValue = Mathf.Max(finalValue, _initialNewsData.minVariationRate);
                else if (effect.value < 0)
                    finalValue = Mathf.Min(finalValue, -_initialNewsData.minVariationRate);

                finalValue = Mathf.Clamp(finalValue, _initialNewsData.minResourcePricePer, _initialNewsData.maxResourcePricePer);
                _dataManager.Effect.ApplyEffect(effect, finalValue, effect.targetId);
            }
        }

        OnNewsChanged?.Invoke(newState);
    }

    private void ResetNewsChance()
    {
        _currentNewsChance = _initialNewsData.baseNewsChance;
        _daysSinceLastNews = 0;
    }

    public NewsData GetNewsData(string newsId)
    {
        return _newsDataDict.TryGetValue(newsId, out NewsData data) ? data : null;
    }

    public Dictionary<string, NewsData> GetAllNewsData()
    {
        return new Dictionary<string, NewsData>(_newsDataDict);
    }

    public List<NewsState> GetActiveNewsList()
    {
        return new List<NewsState>(_activeNewsList);
    }

    public void ClearAllSubscriptions()
    {
        OnNewsChanged = null;
    }

    public void HandleDayChanged()
    {
        _daysSinceLastNews++;

        for(int i = _activeNewsList.Count - 1; i >= 0; i--)
        {
            NewsState news = _activeNewsList[i];
            news.remainingDays--;
            if (news.remainingDays <= 0)
            {
                NewsData data = GetNewsData(news.id);
                if (data?.effects != null)
                {
                    foreach (EffectData effect in data.effects)
                    {
                        if (effect == null) continue;
                        _dataManager.Effect.RemoveEffect(effect, effect.targetId);
                    }
                }
                _activeNewsList.RemoveAt(i);
            }
        }

        TryGenerateNews();
    }
}
