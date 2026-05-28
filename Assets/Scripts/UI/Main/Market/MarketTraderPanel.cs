using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketTraderPanel : MonoBehaviour
{
    private DataManager _dataManager;
    private MarketCanvas _marketPanel;
    private MarketActorEntry _selectedActor;
    private Coroutine _resourceIconCoroutine;

    [Header("Details")]
    [SerializeField] private MarketActorPopupBtn _marketActorPopupBtn;
    [SerializeField] private TMPro.TextMeshProUGUI _nameText;
    [SerializeField] private TMPro.TextMeshProUGUI _descriptionText;
    [SerializeField] private TMPro.TextMeshProUGUI _wealthText;
    [SerializeField] private TMPro.TextMeshProUGUI _trustText;

    [Header("Resource Lists")]
    [SerializeField] private Transform _provideResourceContentTransform;
    [SerializeField] private Transform _consumeResourceContentTransform;

    private void OnDisable()
    {
        StaggeredSpawnUtils.Stop(this, ref _resourceIconCoroutine);
    }

    public void Init(MarketCanvas marketPanel)
    {
        if (marketPanel == null)
            return;

        _dataManager = marketPanel.DataManager;
        _marketPanel = marketPanel;

        if (_selectedActor == null && _dataManager != null)
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

    private void RefreshUI()
    {
        if (_selectedActor == null || _marketPanel == null)
            return;

        MarketActorData data = _selectedActor.data;
        MarketActorState state = _selectedActor.state;

        _marketActorPopupBtn.Init(_selectedActor);
        _nameText.text = data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        _descriptionText.text = (data.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_MARKET_ACTOR);

        string deltaSymbol = state.currentChangeWealth > 0 ? "+" : "";
        _wealthText.text = $"{ReplaceUtils.FormatNumber(state.wealth)} {deltaSymbol}{ReplaceUtils.FormatNumber(state.currentChangeWealth)}";
        _wealthText.color = _marketPanel.VisualManager.GetDeltaColor(state.currentChangeWealth);
        _trustText.text = $"{state.trust}";
    }

    private void RefreshResourcesIcon()
    {
        StaggeredSpawnUtils.Restart(this, ref _resourceIconCoroutine, RefreshResourcesIconRoutine());
    }

    private IEnumerator RefreshResourcesIconRoutine()
    {
        if (_marketPanel == null || _dataManager == null)
            yield break;

        PoolingManager pool = _marketPanel.GameManager?.PoolingManager;
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

        if (_selectedActor == null || _selectedActor.data.productionResourceList == null)
            yield break;

        UIManager uiManager = _marketPanel.UIManager;
        if (uiManager == null)
            yield break;

        List<(Transform parent, ResourceEntry entry, int count)> iconDefs = new List<(Transform, ResourceEntry, int)>();

        foreach (ResourceData resourceData in _selectedActor.data.comsumeResourceList)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            int productionCount = (int)_selectedActor.data.baseProductionCount;
            iconDefs.Add((_consumeResourceContentTransform, resourceEntry, productionCount));
        }

        foreach (ResourceData resourceData in _selectedActor.data.productionResourceList)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            int productionCount = (int)_selectedActor.data.baseProductionCount;
            iconDefs.Add((_provideResourceContentTransform, resourceEntry, productionCount));
        }

        yield return StaggeredSpawnUtils.ForEachFrame(iconDefs.Count, i =>
        {
            (Transform parent, ResourceEntry entry, int count) def = iconDefs[i];
            uiManager.CreateProductionIcon(def.parent, def.entry, def.count);
        });
    }
}
