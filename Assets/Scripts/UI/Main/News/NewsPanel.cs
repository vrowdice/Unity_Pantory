using System.Collections.Generic;
using UnityEngine;

public class NewsPanel : BasePanel
{
    [SerializeField] Transform _newspaperContentTransform;
    [SerializeField] GameObject _newspaperPanelPrefab;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        RefreshNewsList();
    }

    private void OnEnable()
    {
        _dataManager.Time.OnDayChanged += RefreshNewsList;
    }

    private void OnDisable()
    {
        _dataManager.Time.OnDayChanged += RefreshNewsList;
    }

    private void RefreshNewsList()
    {
         GameObjectUtils.ClearChildren(_newspaperContentTransform);
         
         List<NewsState> activeNewsList = _dataManager.News.GetActiveNewsList();
         foreach (NewsState item in activeNewsList)
         {
             if (item == null) continue;
 
             GameObject newsObj = Instantiate(_newspaperPanelPrefab, _newspaperContentTransform);
             NewspaperPanel newsPanel = newsObj.GetComponent<NewspaperPanel>();
             if (newsPanel != null)
             {
                 newsPanel.Init(item, this);
             }
         }
    }
}
