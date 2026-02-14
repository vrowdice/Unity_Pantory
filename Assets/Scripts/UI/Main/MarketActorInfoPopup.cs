using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MarketActorInfoPopup : BasePopup
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
    private MainCanvas _mainUiManager;
    private DataManager _dataManager;

    public void Init(MarketActorEntry marketActorEntry, MainCanvas mainUiManager)
    {
        base.Init();

        _currentMarketActorEntry = marketActorEntry;
        _mainUiManager = mainUiManager;
        _dataManager = DataManager.Instance;

        _dataManager.Time.OnDayChanged -= OnDayChanged;
        _dataManager.Time.OnDayChanged += OnDayChanged;

        RefreshAllUI();

        Show();
    }

    public void RefreshAllUI()
    {
        _nameText.text = _currentMarketActorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        _descriptionText.text = _currentMarketActorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR_DESCRIPTION);
        _trustText.text = _currentMarketActorEntry.state.trust.ToString("F1");
        _wealthText.text = _currentMarketActorEntry.state.wealth.ToString("F1");

        _marketActorImage.sprite = _currentMarketActorEntry.data.icon;

        UpdateTypeOfOrder();
    }

    private void UpdateTypeOfOrder()
    {
        if (_typeOfOrderScrollViewContextTransform == null || _typeOfOrderScrollViewItemTextPrefab == null) return;

        for (int i = _typeOfOrderScrollViewContextTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(_typeOfOrderScrollViewContextTransform.GetChild(i).gameObject);
        }

        MarketActorType actorType = _currentMarketActorEntry.data.marketActorType;
        Dictionary<string, OrderData> allOrders = _dataManager.Order.GetAllOrderData();

        foreach (OrderData orderData in allOrders.Values)
        {
            if (orderData == null || orderData.marketActorType != actorType) continue;

            GameObject itemObj = Instantiate(_typeOfOrderScrollViewItemTextPrefab, _typeOfOrderScrollViewContextTransform);
            TextMeshProUGUI textComp = itemObj.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = orderData.id.Localize(LocalizationUtils.TABLE_ORDER);
            }
        }
    }

    public void OnDayChanged()
    {
        if (gameObject.activeSelf) RefreshAllUI();
    }
}
