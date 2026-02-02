using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;

public class MarketTraderPanel : MonoBehaviour
{
    private DataManager _dataManager;
    private MarketPanel _marketPanel;
    private MarketActorEntry _selectedActor;

    [Header("Details")]
    [SerializeField] private Image _traderImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _wealthText;
    [SerializeField] private TextMeshProUGUI _changeWelthText;

    [Header("Resource Lists")]
    [SerializeField] private Transform _providerResourceContentTransform;

    public void Init(MarketPanel marketPanel)
    {
        _dataManager = DataManager.Instance;
        _marketPanel = marketPanel;

        if (_selectedActor == null)
        {
            ChangeActor(_dataManager.MarketActor.GetMarketActorEntry("artisan_furniture_workshop"));
        }
    }

    public void ChangeActor(MarketActorEntry actorEntry)
    {
        _selectedActor = actorEntry;

        RefreshUI();
    }

    public void HandleDayChanged()
    {
        RefreshUI();
    }

    /// <summary>
    /// 선택된 행위자의 UI를 새로고침합니다.
    /// </summary>
    private void RefreshUI()
    {
        if (_selectedActor == null) return;

        MarketActorData data = _selectedActor.data;
        MarketActorState state = _selectedActor.state;

        _traderImage.sprite = data.icon;
        _nameText.text = data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        _descriptionText.text = data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR_DESCRIPTION);

        _wealthText.text = $"{state.wealth:N0}";
        string deltaSymbol = state.currentChangeWealth > 0 ? "+" : "";
        _changeWelthText.text = $" ({deltaSymbol}{state.currentChangeWealth:F2})";

        RefreshProviderResources();
    }

    /// <summary>
    /// 생산 자원 아이콘들을 새로고침합니다.
    /// </summary>
    private void RefreshProviderResources()
    {
        GameObjectUtils.ClearChildren(_providerResourceContentTransform);

        if (_selectedActor == null || _selectedActor.data.productionResources == null) return;

        GameManager gameManager = GameManager.Instance;

        foreach (ResourceData resourceData in _selectedActor.data.productionResources)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            int productionCount = (int)_selectedActor.data.baseProductionCount;
            gameManager.CreateProductionIcon(_providerResourceContentTransform, resourceEntry, productionCount);
        }
    }
}
