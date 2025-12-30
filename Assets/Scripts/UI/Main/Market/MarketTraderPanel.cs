using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;

public class MarketTraderPanel : MonoBehaviour
{
    private DataManager _dataManager;
    private GameManager _gameManager;
    private MarketActorEntry _selectedActor;
    private List<GameObject> _providerResourceIcons = new List<GameObject>();
    private List<GameObject> _consumerResourceIcons = new List<GameObject>();

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

    public void OnInitialize(GameManager gameManager, DataManager dataManager)
    {
        _gameManager = gameManager;
        _dataManager = dataManager;

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;

        HandleTraderButtonClicked(_dataManager.Market.GetActor("iron_mining_consortium"));
    }

    public void HandleTraderButtonClicked(MarketActorEntry actorEntry)
    {
        _selectedActor = actorEntry;
        UpdateDetails();
    }

    public string GetSelectedActorId()
    {
        return _selectedActor?.data?.id ?? string.Empty;
    }

    private void UpdateDetails()
    {
        MarketActorData data = _selectedActor.data;
        MarketActorState state = _selectedActor.state;

        _portrait.sprite = data.icon;
        _portrait.enabled = data.icon != null;
        _nameText.text = string.IsNullOrEmpty(data.displayName) ? data.id : data.displayName;
        _descriptionText.text = data.description;

        // Tendency (Provider)
        if (_tendencyText != null)
        {
            string tendency = "-";
            Color color = Color.white;

            float delta = state.provider.priceDelta;
            tendency = $"Tendency {delta:+0.##;-0.##;0}";
            color = VisualManager.Instance.GetDeltaColor(delta);

            _tendencyText.text = tendency;
            _tendencyText.color = color;
        }

        // Budget & Satisfaction (Consumer)
        // currentBudget: 하루에 사용할 수 있는 예산 (자산과는 다름)
        // wealth: 액터의 총 자산
        if (_budgetText != null)
        {
            string budgetStr = "-";
            Color color = Color.white;
            if (state?.consumer != null)
            {
                float budget = state.consumer.currentBudget;
                float wealth = state.GetWealth();
                
                // 예산이 0이면 자산 기반으로 계산된 예산 표시
                if (budget <= 0f && wealth > 0f)
                {
                    var consumerProfile = _selectedActor.GetConsumerProfile();
                    if (consumerProfile != null)
                    {
                        float budgetRatio = _selectedActor.data?.scale switch
                        {
                            MarketActorScale.Small => 0.5f,
                            MarketActorScale.Large => 0.6f,
                            _ => 0.55f
                        };
                        float calculatedBudget = wealth * budgetRatio;
                        budgetStr = $"Daily Budget: {calculatedBudget:N0} (from {ReplaceUtils.FormatNumber((long)wealth)} wealth)";
                    }
                    else
                    {
                        budgetStr = $"Daily Budget: 0 (Wealth: {ReplaceUtils.FormatNumber((long)wealth)})";
                    }
                }
                else if (budget > 0f)
                {
                    budgetStr = $"Daily Budget: {budget:N0}";
                }
                else
                {
                    budgetStr = $"Daily Budget: 0 (No Wealth)";
                }
                color = VisualManager.Instance != null 
                    ? VisualManager.Instance.GetBudgetColor(budget > 0f ? budget : wealth)
                    : Color.white;
            }
            _budgetText.text = budgetStr;
            _budgetText.color = color;
        }

        // 자산 변화량
        if (_assetChangeText != null)
        {
            if (state != null && VisualManager.Instance != null)
            {
                float wealthChange = state.wealth - state.previousWealth;
                string changeStr = wealthChange >= 0
                    ? $"+{ReplaceUtils.FormatNumber((long)wealthChange)}"
                    : ReplaceUtils.FormatNumber((long)wealthChange);
                _assetChangeText.text = $"Asset Change: {changeStr}";
                _assetChangeText.color = VisualManager.Instance.GetDeltaColor(wealthChange);
            }
            else
            {
                _assetChangeText.text = "Asset Change: -";
                _assetChangeText.color = Color.white;
            }
        }

        // Activity Summary (거래 통계 및 경제력) - 3줄로 축약
        if (_activityText != null)
        {
            var summary = new System.Text.StringBuilder();

            // 기본 정보
            float wealth = state?.GetWealth() ?? 0f;
            int rank = state?.GetRank() ?? 0;
            float economicPower = state?.CalculateEconomicPower() ?? 0f;
            string healthStatus = state?.GetHealthStatus() ?? "Normal";

            // 1줄: 기본 정보 통합
            summary.AppendLine($"Rank: #{rank} | Power: {(economicPower * 100f):F1}% | Health: {healthStatus} | Wealth: {ReplaceUtils.FormatNumber((long)wealth)}");

            // 2줄: 거래 정보 통합
            float salesRevenue = state?.provider?.dailySalesRevenue ?? 0f;
            float purchaseExpense = state?.consumer?.dailyPurchaseExpense ?? 0f;
            float dailyTradeVolume = state?.GetDailyTradeVolume() ?? 0f;
            summary.AppendLine($"Sales: {ReplaceUtils.FormatNumber((long)salesRevenue)} | Purchase: {ReplaceUtils.FormatNumber((long)purchaseExpense)} | Volume: {ReplaceUtils.FormatNumber((long)dailyTradeVolume)}");

            // 3줄: 순이익
            float dailyNetProfit = state?.GetDailyNetProfit() ?? 0f;
            summary.Append($"Net Profit: {ReplaceUtils.FormatNumber((long)dailyNetProfit)}");

            _activityText.text = summary.ToString();
        }

        // 자원 아이콘 업데이트
        UpdateResourceIcons();
    }

    private void UpdateResourceIcons()
    {
        // 기존 아이콘 정리
        ClearResourceIcons();

        if (_selectedActor?.data == null || _dataManager == null || _gameManager == null)
        {
            return;
        }

        // Provider 자원 (판매하는 자원) 표시
        if (_providerResourceContentTransform != null)
        {
            var providerProfile = _selectedActor.GetProviderProfile();
            if (providerProfile?.outputs != null && providerProfile.outputs.Count > 0)
            {
                foreach (var output in providerProfile.outputs)
                {
                    if (output?.resource == null || string.IsNullOrEmpty(output.resource.id))
                    {
                        continue;
                    }

                    var resourceEntry = _dataManager.Resource.GetResourceEntry(output.resource.id);
                    if (resourceEntry != null && _gameManager.ProductionInfoImage != null)
                    {
                        var iconObj = Instantiate(_gameManager.ProductionInfoImage, _providerResourceContentTransform);
                        var iconComponent = iconObj.GetComponent<ProductionInfoImage>();
                        if (iconComponent != null)
                        {
                            // 수량 범위 표시 (desiredMin-desiredMax)
                            int avgAmount = (int)((output.desiredMin + output.desiredMax) / 2);
                            iconComponent.OnInitialize(resourceEntry, avgAmount);
                        }
                        _providerResourceIcons.Add(iconObj);
                    }
                }
            }
        }

        // Consumer 자원 (구매하는 자원) 표시
        if (_consumerResourceContentTransform != null)
        {
            var consumerProfile = _selectedActor.GetConsumerProfile();
            if (consumerProfile?.desiredResources != null && consumerProfile.desiredResources.Count > 0)
            {
                foreach (var resource in consumerProfile.desiredResources)
                {
                    if (resource?.resource == null || string.IsNullOrEmpty(resource.resource.id))
                    {
                        continue;
                    }

                    var resourceEntry = _dataManager.Resource.GetResourceEntry(resource.resource.id);
                    if (resourceEntry != null && _gameManager.ProductionInfoImage != null)
                    {
                        var iconObj = Instantiate(_gameManager.ProductionInfoImage, _consumerResourceContentTransform);
                        var iconComponent = iconObj.GetComponent<ProductionInfoImage>();
                        if (iconComponent != null)
                        {
                            // 수량 범위 표시 (desiredMin-desiredMax)
                            int avgAmount = (int)((resource.desiredMin + resource.desiredMax) / 2);
                            iconComponent.OnInitialize(resourceEntry, avgAmount);
                        }
                        _consumerResourceIcons.Add(iconObj);
                    }
                }
            }
        }
    }

    private void ClearResourceIcons()
    {
        // Provider 아이콘 정리
        foreach (var icon in _providerResourceIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        _providerResourceIcons.Clear();

        // Consumer 아이콘 정리
        foreach (var icon in _consumerResourceIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        _consumerResourceIcons.Clear();
    }

    private void HandleDayChanged()
    {
        if (_selectedActor != null)
        {
            UpdateDetails();
        }
    }
}
