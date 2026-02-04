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

    public event Action<NewsState> OnNewsChanged;

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
        if (_newsDataList == null || _newsDataList.Count == 0) return;

        List<NewsData> availableNews = _newsDataList
            .Where(data => !_activeNewsList.Any(active => active.id == data.id))
            .ToList();

        NewsData selectedData = null;
        if (availableNews.Count > 0)
        {
            selectedData = availableNews[UnityEngine.Random.Range(0, availableNews.Count)];
        }
        else
        {
            ResetNewsChance();
            return;
        }

            NewsState newState = new NewsState(selectedData);
        _activeNewsList.Add(newState);

        foreach (EffectData effect in selectedData.effects)
        {
            _dataManager.Effect.ApplyEffect(effect, effect.value, effect.targetId);
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
        if (_newsDataDict.TryGetValue(newsId, out var data))
        {
            return data;
        }

        return null;
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
                _activeNewsList.RemoveAt(i);
            }
        }

        TryGenerateNews();
    }
}
