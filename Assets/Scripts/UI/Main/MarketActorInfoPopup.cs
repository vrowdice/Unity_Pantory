using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    }

    public void UpdateTypeOfOrder()
    {
        GameObject typeOfOrderItemPrefab = Instantiate(_typeOfOrderScrollViewItemTextPrefab, _typeOfOrderScrollViewContextTransform);
    }

    public void OnDayChanged()
    {
        if (gameObject.activeSelf) RefreshAllUI();
    }
}
