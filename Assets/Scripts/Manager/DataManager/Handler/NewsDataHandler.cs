using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 뉴스 항목을 관리하는 핸들러.
/// </summary>
public class NewsDataHandler : IDataHandlerEvents, ITimeChangeHandler, IGameSaveHandler
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
            for (int ei = 0; ei < selectedData.effects.Count; ei++)
            {
                EffectData effect = selectedData.effects[ei];
                if (effect == null) continue;

                float additionalRandom = UnityEngine.Random.Range(-_initialNewsData.maxRandomRange, _initialNewsData.maxRandomRange);
                float finalValue = effect.value + additionalRandom;

                if (effect.value > 0)
                    finalValue = Mathf.Max(finalValue, _initialNewsData.minVariationRate);
                else if (effect.value < 0)
                    finalValue = Mathf.Min(finalValue, -_initialNewsData.minVariationRate);

                finalValue = Mathf.Clamp(finalValue, _initialNewsData.minResourcePricePer, _initialNewsData.maxResourcePricePer);
                string instanceId = ResolveNewsEffectInstanceId(effect);
                string runtimeEffectId = BuildNewsEffectRuntimeId(newState.id, effect.id, ei);
                _dataManager.Effect.ApplyEffect(effect, finalValue, instanceId, runtimeEffectId);
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

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        foreach (NewsState state in _activeNewsList)
        {
            saveData.activeNews.Add(CloneState(state));
        }
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        _activeNewsList.Clear();
        foreach (NewsState state in saveData.activeNews)
        {
            _activeNewsList.Add(CloneState(state));
        }
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
                    for (int ei = 0; ei < data.effects.Count; ei++)
                    {
                        EffectData effect = data.effects[ei];
                        if (effect == null) continue;
                        string instanceId = ResolveNewsEffectInstanceId(effect);
                        string runtimeEffectId = BuildNewsEffectRuntimeId(news.id, effect.id, ei);
                        _dataManager.Effect.RemoveEffect(effect, instanceId, runtimeEffectId);
                    }
                }
                _activeNewsList.RemoveAt(i);
            }
        }

        TryGenerateNews();
    }

    private static NewsState CloneState(NewsState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<NewsState>(json);
    }

    /// <summary>
    /// 뉴스 이펙트 적용/해제 시 EffectDataHandler와 동일한 인스턴스 키를 씁니다 (전역 이펙트는 null).
    /// </summary>
    private static string ResolveNewsEffectInstanceId(EffectData effect)
    {
        if (effect == null)
        {
            return null;
        }

        if (effect.isGlobalEffect)
        {
            return null;
        }

        return string.IsNullOrEmpty(effect.targetId) ? null : effect.targetId;
    }

    /// <summary>
    /// 서로 다른 뉴스가 같은 EffectData 에셋(동일 template id)을 쓸 때 슬롯이 덮이거나, 한쪽만 끝나도 전부 지워지지 않도록 런타임 id를 분리합니다.
    /// </summary>
    private static string BuildNewsEffectRuntimeId(string activeNewsId, string templateEffectId, int effectIndexInNews)
    {
        return $"News::{activeNewsId}::{templateEffectId}::{effectIndexInNews}";
    }
}
