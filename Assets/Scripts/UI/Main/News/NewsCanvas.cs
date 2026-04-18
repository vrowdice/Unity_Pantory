using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewsCanvas : MainCanvasPanelBase
{
    [SerializeField] Transform _newspaperContentTransform;
    [SerializeField] GameObject _newspaperPanelPrefab;

    private Dictionary<string, NewspaperPanel> _newsPanelMap = new Dictionary<string, NewspaperPanel>();

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.News.OnNewsChanged -= RefreshNewsList;
        _dataManager.News.OnNewsChanged += RefreshNewsList;

        RefreshNewsList();
    }

    private void OnDisable()
    {
        if(_dataManager != null)
        {
            _dataManager.News.OnNewsChanged -= RefreshNewsList;
        }
    }

    private void RefreshNewsList(NewsState newsState = null)
    {
        List<NewsState> activeNewsList = _dataManager.News.GetActiveNewsList();

        List<string> idsToRemove = _newsPanelMap.Keys
            .Where(id => !activeNewsList.Any(news => news.id == id))
            .ToList();

        foreach (string id in idsToRemove)
        {
            if (_newsPanelMap.TryGetValue(id, out NewspaperPanel panel))
            {
                _gameManager.PoolingManager.ReturnToPool(panel.gameObject);
                _newsPanelMap.Remove(id);
            }
        }

        foreach (NewsState item in activeNewsList)
        {
            if (item == null) continue;
            if (_newsPanelMap.ContainsKey(item.id)) continue;

            GameObject newsObj = _gameManager.PoolingManager.GetPooledObject(_newspaperPanelPrefab);
            newsObj.transform.SetParent(_newspaperContentTransform, false);
            NewspaperPanel newsPanel = newsObj.GetComponent<NewspaperPanel>();
            if (newsPanel != null)
            {
                newsPanel.Init(item, this);
                _newsPanelMap.Add(item.id, newsPanel);
            }
        }
    }
}
