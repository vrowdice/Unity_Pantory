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
    [SerializeField] private Image _portrait;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _activityText;
    [SerializeField] private TextMeshProUGUI _tendencyText;
    [SerializeField] private TextMeshProUGUI _budgetText;
    [SerializeField] private TextMeshProUGUI _assetChangeText;

    [Header("Resource Lists")]
    [SerializeField] private Transform _providerResourceContentTransform;
    [SerializeField] private Transform _consumerResourceContentTransform;

    public void Init(DataManager dataManager, MarketPanel marketPanel)
    {
        _dataManager = dataManager;
        _marketPanel = marketPanel;

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;
    }

    public void HandleTraderButtonClicked(MarketActorEntry actorEntry)
    {
        _selectedActor = actorEntry;
    }

    private void HandleDayChanged()
    {

    }
}
