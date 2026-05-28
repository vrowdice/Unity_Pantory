using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MarketActorInfoPopup : PopupBase
{
    [Header("Basic Info")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _trustText;
    [SerializeField] private TextMeshProUGUI _wealthText;
    [SerializeField] private Image _marketActorImage;

    [SerializeField] private Transform _typeOfOrderScrollViewContextTransform;
    [SerializeField] private GameObject _typeOfOrderScrollViewItemTextPrefab;

    private MarketActorEntry _currentMarketActorEntry;
    private Coroutine _orderListCoroutine;

    public void Init(MarketActorEntry marketActorEntry)
    {
        base.Init();

        _currentMarketActorEntry = marketActorEntry;

        RefreshAllUI();

        Show();
    }

    public override void Close()
    {
        StaggeredSpawnUtils.Stop(this, ref _orderListCoroutine);
        base.Close();
    }

    public void RefreshAllUI()
    {
        _nameText.text = _currentMarketActorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        _descriptionText.text = (_currentMarketActorEntry.data.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        _trustText.text = _currentMarketActorEntry.state.trust.ToString("F1");
        _wealthText.text = _currentMarketActorEntry.state.wealth.ToString("F1");

        _marketActorImage.sprite = _currentMarketActorEntry.data.icon;

        StaggeredSpawnUtils.Restart(this, ref _orderListCoroutine, UpdateTypeOfOrderRoutine());
    }

    private IEnumerator UpdateTypeOfOrderRoutine()
    {
        if (_typeOfOrderScrollViewContextTransform == null || _typeOfOrderScrollViewItemTextPrefab == null)
            yield break;

        for (int i = _typeOfOrderScrollViewContextTransform.childCount - 1; i >= 0; i--)
            Destroy(_typeOfOrderScrollViewContextTransform.GetChild(i).gameObject);

        MarketActorType actorType = _currentMarketActorEntry.data.marketActorType;
        Dictionary<string, OrderData> allOrders = _dataManager.Order.GetAllOrderData();
        List<OrderData> matchedOrders = new List<OrderData>();

        foreach (OrderData orderData in allOrders.Values)
        {
            if (orderData == null || orderData.marketActorType != actorType)
                continue;

            matchedOrders.Add(orderData);
        }

        yield return StaggeredSpawnUtils.ForEachFrame(matchedOrders.Count, i =>
        {
            OrderData orderData = matchedOrders[i];
            GameObject itemObj = Instantiate(_typeOfOrderScrollViewItemTextPrefab, _typeOfOrderScrollViewContextTransform);
            TextMeshProUGUI textComp = itemObj.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
                textComp.text = orderData.id.Localize(LocalizationUtils.TABLE_ORDER);
        });
    }

    protected override void HandleDayChanged()
    {
        if (gameObject.activeSelf)
            RefreshAllUI();
    }

    public void OnClickClose()
    {
        Close();
    }
}
