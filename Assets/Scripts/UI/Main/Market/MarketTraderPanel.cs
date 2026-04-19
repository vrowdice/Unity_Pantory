using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;
using System.ComponentModel;

public class MarketTraderPanel : MonoBehaviour
{
    private DataManager _dataManager;
    private MarketCanvas _marketPanel;
    private MarketActorEntry _selectedActor;

    [Header("Details")]
    [SerializeField] private MarketActorPopupBtn _marketActorPopupBtn;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _wealthText;
    [SerializeField] private TextMeshProUGUI _trustText;

    [Header("Resource Lists")]
    [SerializeField] private Transform _provideResourceContentTransform;
    [SerializeField] private Transform _consumeResourceContentTransform;

    public void Init(MarketCanvas marketPanel)
    {
        _dataManager = marketPanel.Host.DataManager;
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

        _marketActorPopupBtn.Init(_selectedActor);
        _nameText.text = data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        _descriptionText.text = (data.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_MARKET_ACTOR);

        string deltaSymbol = state.currentChangeWealth > 0 ? "+" : "";
        _wealthText.text = $"{ReplaceUtils.FormatNumber(state.wealth)} {deltaSymbol}{ReplaceUtils.FormatNumber(state.currentChangeWealth)}";
        _wealthText.color = _marketPanel.Host.VisualManager.GetDeltaColor(state.currentChangeWealth);
        _trustText.text = $"{state.trust}";
    }

    /// <summary>
    /// 생산 자원 아이콘들을 새로고침합니다.
    /// </summary>
    private void RefreshResourcesIcon()
    {
        PoolingManager pool = _marketPanel.Host.GameManager.PoolingManager;
        if (pool != null)
        {
            pool.ClearChildrenToPool(_consumeResourceContentTransform);
            pool.ClearChildrenToPool(_provideResourceContentTransform);
        }
        else
        {
            GameObjectUtils.ClearChildren(_consumeResourceContentTransform);
            GameObjectUtils.ClearChildren(_provideResourceContentTransform);
        }

        if (_selectedActor == null || _selectedActor.data.productionResourceList == null) return;

        UIManager uiManager = _marketPanel.Host.UIManager;

        foreach (ResourceData resourceData in _selectedActor.data.comsumeResourceList)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            int productionCount = (int)_selectedActor.data.baseProductionCount;
            uiManager.CreateProductionIcon(_consumeResourceContentTransform, resourceEntry, productionCount);
        }
        foreach (ResourceData resourceData in _selectedActor.data.productionResourceList)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            int productionCount = (int)_selectedActor.data.baseProductionCount;
            uiManager.CreateProductionIcon(_provideResourceContentTransform, resourceEntry, productionCount);
        }
    }
}
