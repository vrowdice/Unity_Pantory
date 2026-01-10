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
    /// 그래프와 텍스트 정보를 포함한 모든 UI 요소를 최신화합니다.
    /// </summary>
    private void RefreshUI()
    {
        if (_selectedActor == null) return;

        MarketActorData data = _selectedActor.data;
        MarketActorState state = _selectedActor.state;

        _traderImage.sprite = data.icon;
        _nameText.text = data.displayName;
        _descriptionText.text = data.description;

        _wealthText.text = $"{state.wealth:N0}";
        string deltaSymbol = state.currentChangeWealth > 0 ? "+" : "";
        _changeWelthText.text = $" ({deltaSymbol}{state.currentChangeWealth:F2})";
    }
}
