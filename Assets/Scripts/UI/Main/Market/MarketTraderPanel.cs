using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class MarketTraderPanel : MonoBehaviour
{
    private GameDataManager _dataManager;
    private MarketActorEntry _selectedActor;

    [Header("Details")]
    [SerializeField] private Image _portrait;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _rolesText;
    [SerializeField] private TextMeshProUGUI _archetypeText;
    [SerializeField] private TextMeshProUGUI _tendencyText;
    [SerializeField] private TextMeshProUGUI _budgetText;
    [SerializeField] private TextMeshProUGUI _satisfactionText;
    [SerializeField] private TextMeshProUGUI _activityText;   // active resources / contracts / holdings summary

    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;
        ClearDetails();
    }

    public void HandleTraderButtonClicked(MarketActorEntry actorEntry)
    {
        _selectedActor = actorEntry;
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        if (_selectedActor?.data == null)
        {
            ClearDetails();
            return;
        }

        var data = _selectedActor.data;
        var state = _selectedActor.state;

        // Portrait & Name
        if (_portrait != null)
        {
            _portrait.sprite = data.icon;
            _portrait.enabled = data.icon != null;
        }

        if (_nameText != null)
        {
            _nameText.text = string.IsNullOrEmpty(data.displayName) ? data.id : data.displayName;
        }

        // Roles & Archetype
        if (_rolesText != null)
        {
            _rolesText.text = GetRoleSummary(data.roles);
        }

        if (_archetypeText != null)
        {
            _archetypeText.text = data.archetype.ToString();
        }

        // Tendency (Provider)
        if (_tendencyText != null)
        {
            string tendency = "-";
            Color color = Color.white;
            if (state?.provider != null && data.roles.HasFlag(MarketRoleFlags.Provider))
            {
                float delta = state.provider.priceDelta;
                tendency = $"Tendency {delta:+0.##;-0.##;0}";
                color = GetDeltaColor(delta);
            }
            _tendencyText.text = tendency;
            _tendencyText.color = color;
        }

        // Budget & Satisfaction (Consumer)
        if (_budgetText != null)
        {
            string budgetStr = "-";
            Color color = Color.white;
            if (state?.consumer != null && data.roles.HasFlag(MarketRoleFlags.Consumer))
            {
                float budget = state.consumer.currentBudget;
                budgetStr = $"Budget {budget:N0}";
                color = GetBudgetColor(budget);
            }
            _budgetText.text = budgetStr;
            _budgetText.color = color;
        }

        if (_satisfactionText != null)
        {
            string satStr = "-";
            if (state?.consumer != null && data.roles.HasFlag(MarketRoleFlags.Consumer))
            {
                satStr = $"Satisfaction {(state.consumer.satisfaction * 100f):0.#}%";
            }
            _satisfactionText.text = satStr;
        }

        // Activity Summary (거래 통계 및 경제력)
        if (_activityText != null)
        {
            var summary = new System.Text.StringBuilder();
            
            // 기본 정보
            float wealth = state?.GetWealth() ?? 0f;
            int rank = state?.GetRank() ?? 0;
            float economicPower = state?.CalculateEconomicPower() ?? 0f;
            string healthStatus = state?.GetHealthStatus() ?? "Normal";
            
            summary.AppendLine($"Rank: #{rank} | Economic Power: {(economicPower * 100f):F1}% | Health: {healthStatus}");
            summary.AppendLine($"Total Wealth: {ReplaceUtils.FormatNumber((long)wealth)}");
            
            // Provider 거래 통계
            if (state?.provider != null && data.roles.HasFlag(MarketRoleFlags.Provider))
            {
                var provider = state.provider;
                summary.AppendLine($"Provider - Sales: {ReplaceUtils.FormatNumber((long)provider.dailySalesRevenue)} | " +
                    $"Volume: {ReplaceUtils.FormatNumber((long)provider.dailySalesVolume)} | " +
                    $"Net Profit: {ReplaceUtils.FormatNumber((long)provider.dailyNetProfit)}");
            }
            
            // Consumer 거래 통계
            if (state?.consumer != null && data.roles.HasFlag(MarketRoleFlags.Consumer))
            {
                var consumer = state.consumer;
                summary.AppendLine($"Consumer - Purchase: {ReplaceUtils.FormatNumber((long)consumer.dailyPurchaseExpense)} | " +
                    $"Volume: {ReplaceUtils.FormatNumber((long)consumer.dailyPurchaseVolume)} | " +
                    $"Value: {ReplaceUtils.FormatNumber((long)consumer.dailyConsumptionValue)}");
            }
            
            // 일일 거래액 및 순이익
            float dailyTradeVolume = state?.GetDailyTradeVolume() ?? 0f;
            float dailyNetProfit = state?.GetDailyNetProfit() ?? 0f;
            summary.AppendLine($"Daily Trade Volume: {ReplaceUtils.FormatNumber((long)dailyTradeVolume)}");
            summary.Append($"Daily Net Profit: {ReplaceUtils.FormatNumber((long)dailyNetProfit)}");
            
            _activityText.text = summary.ToString();
        }
    }

    private void ClearDetails()
    {
        if (_portrait != null)
        {
            _portrait.sprite = null;
            _portrait.enabled = false;
        }
        if (_nameText != null) _nameText.text = "-";
        if (_rolesText != null) _rolesText.text = "-";
        if (_archetypeText != null) _archetypeText.text = "-";
        if (_tendencyText != null) { _tendencyText.text = "-"; _tendencyText.color = Color.white; }
        if (_budgetText != null) { _budgetText.text = "-"; _budgetText.color = Color.white; }
        if (_satisfactionText != null) _satisfactionText.text = "-";
        if (_activityText != null) _activityText.text = "-";
    }

    private static string GetRoleSummary(MarketRoleFlags roles)
    {
        bool isProvider = roles.HasFlag(MarketRoleFlags.Provider);
        bool isConsumer = roles.HasFlag(MarketRoleFlags.Consumer);
        if (isProvider && isConsumer) return "Provider · Consumer";
        if (isProvider) return "Provider";
        if (isConsumer) return "Consumer";
        return "None";
    }

    private static Color GetDeltaColor(float delta)
    {
        if (delta > 0f) return Color.green;
        if (delta < 0f) return Color.red;
        return Color.white;
    }

    private static Color GetBudgetColor(float budget)
    {
        if (budget >= 1000f) return Color.green;
        if (budget <= 100f) return Color.red;
        return Color.white;
    }
}
