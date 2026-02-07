using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewsPanel : BasePanel
{
    [SerializeField] Transform _newspaperContentTransform;
    [SerializeField] GameObject _newspaperPanelPrefab;

    private Dictionary<string, GameObject> _newsObjectMap = new Dictionary<string, GameObject>();

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        RefreshNewsList();
    }

    private void OnEnable()
    {
        _dataManager.News.OnNewsChanged += RefreshNewsList;
    }

    private void OnDisable()
    {
        _dataManager.News.OnNewsChanged += RefreshNewsList;
    }

    private void RefreshNewsList(NewsState newsState = null)
    {
        List<NewsState> activeNewsList = _dataManager.News.GetActiveNewsList();

        List<string> idsToRemove = _newsObjectMap.Keys
            .Where(id => !activeNewsList.Any(news => news.id == id))
            .ToList();

        foreach (string id in idsToRemove)
        {
            if (_newsObjectMap.TryGetValue(id, out GameObject obj))
            {
                Destroy(obj);
                _newsObjectMap.Remove(id);
            }
        }

        foreach (NewsState item in activeNewsList)
        {
            if (item == null) continue;
            if (_newsObjectMap.ContainsKey(item.id))
            {
                continue;
            }

            GameObject newsObj = Instantiate(_newspaperPanelPrefab, _newspaperContentTransform);
            NewspaperPanel newsPanel = newsObj.GetComponent<NewspaperPanel>();
            if (newsPanel != null)
            {
                newsPanel.Init(item, this);
            }

            _newsObjectMap.Add(item.id, newsObj);
        }
    }
}
