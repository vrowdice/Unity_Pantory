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
    [SerializeField] private TextMeshProUGUI _trustText;

    [Header("Resource Lists")]
    [SerializeField] private Transform _provideResourceContentTransform;
    [SerializeField] private Transform _consumeResourceContentTransform;

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
        RefreshResourcesIcon();
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

        string deltaSymbol = state.currentChangeWealth > 0 ? "+" : "";
        _wealthText.text = $"{ReplaceUtils.FormatNumber(state.wealth)} {deltaSymbol}{ReplaceUtils.FormatNumber(state.currentChangeWealth)}";
        _wealthText.color = VisualManager.Instance.GetDeltaColor(state.currentChangeWealth);
        _trustText.text = $"{state.trust}";
    }

    /// <summary>
    /// 생산 자원 아이콘들을 새로고침합니다.
    /// </summary>
    private void RefreshResourcesIcon()
    {
        if (PoolingManager.Instance != null)
        {
            PoolingManager.Instance.ClearChildrenToPool(_consumeResourceContentTransform);
            PoolingManager.Instance.ClearChildrenToPool(_provideResourceContentTransform);
        }
        else
        {
            GameObjectUtils.ClearChildren(_consumeResourceContentTransform);
            GameObjectUtils.ClearChildren(_provideResourceContentTransform);
        }

        if (_selectedActor == null || _selectedActor.data.productionResourceList == null) return;

        GameManager gameManager = GameManager.Instance;

        foreach (ResourceData resourceData in _selectedActor.data.comsumeResourceList)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            int productionCount = (int)_selectedActor.data.baseProductionCount;
            gameManager.CreateProductionIcon(_consumeResourceContentTransform, resourceEntry, productionCount);
        }
        foreach (ResourceData resourceData in _selectedActor.data.productionResourceList)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            int productionCount = (int)_selectedActor.data.baseProductionCount;
            gameManager.CreateProductionIcon(_provideResourceContentTransform, resourceEntry, productionCount);
        }
    }
}
