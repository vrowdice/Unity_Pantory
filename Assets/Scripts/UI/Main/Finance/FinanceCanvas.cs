using UnityEngine;
using Evo.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class FinanceCanvas : MainCanvasPanelBase
{
    [SerializeField] private LineChart _creditChart;
    [SerializeField] private LineChart _welthChart;

    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private TextMeshProUGUI _wealthText;

    [SerializeField] private TextMeshProUGUI _monthlyGrowthRateText;
    [SerializeField] private TextMeshProUGUI _monthlyProfitText;

    [SerializeField] private int _maxDataPoints = 60;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Time.OnDayChanged -= UpdateDailyUI;
        _dataManager.Time.OnDayChanged += UpdateDailyUI;
        _dataManager.Time.OnMonthChanged -= UpdateMonthlyUI;
        _dataManager.Time.OnMonthChanged += UpdateMonthlyUI;
        _dataManager.Finances.OnBankruptcyCountdownChanged -= HandleBankruptcyCountdownChanged;
        _dataManager.Finances.OnBankruptcyCountdownChanged += HandleBankruptcyCountdownChanged;

        UpdateDailyUI();
        UpdateMonthlyUI();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= UpdateDailyUI;
            _dataManager.Time.OnMonthChanged -= UpdateMonthlyUI;
            if (_dataManager.Finances != null)
            {
                _dataManager.Finances.OnBankruptcyCountdownChanged -= HandleBankruptcyCountdownChanged;
            }
        }
    }

    private void HandleBankruptcyCountdownChanged(int monthsRemaining)
    {
        UpdateTexts();
    }

    private void UpdateDailyUI()
    {
        UpdateTexts();
    }

    private void UpdateMonthlyUI()
    {
        UpdateCharts();
    }

    private void UpdateTexts()
    {
        FinancesDataHandler finances = _dataManager.Finances;

        _creditText.text = ReplaceUtils.FormatNumberWithCommas(finances.Credit);
        _wealthText.text = BuildWealthDisplayText(finances);

        long lastMonthCredit = finances.MonthlyCreditHistory.LastOrDefault();
        long currentProfit = finances.Credit - lastMonthCredit;

        _monthlyProfitText.text = ReplaceUtils.FormatNumberWithCommas(currentProfit);
        _monthlyProfitText.color = _visualManager.GetDeltaColor(currentProfit);

        long lastMonthWealth = finances.MonthlyWealthHistory.LastOrDefault();
        long wealthChange = finances.Wealth - lastMonthWealth;
        
        float growthRate = 0f;
        if (lastMonthWealth != 0)
        {
            growthRate = ((float)wealthChange / lastMonthWealth) * 100f;
        }

        _monthlyGrowthRateText.text = $"{growthRate:F2}%";
        _monthlyGrowthRateText.color = _visualManager.GetDeltaColor(growthRate);
    }

    private static string BuildWealthDisplayText(FinancesDataHandler finances)
    {
        string wealthText = ReplaceUtils.FormatNumberWithCommas(finances.Wealth);
        if (!finances.IsBankruptcyCountdownActive)
        {
            return wealthText;
        }

        return $"{wealthText}\n<color=#FF6B6B>{WarningMessage.BankruptcyCountdownTick.LocalizeFormat(LocalizationUtils.TABLE_COMMON, finances.BankruptcyMonthsRemaining)}</color>";
    }

    private void UpdateCharts()
    {
        FinancesDataHandler finances = _dataManager.Finances;
        UpdateSingleChart(_creditChart, finances.MonthlyCreditHistory, finances.Credit);
        UpdateSingleChart(_welthChart, finances.MonthlyWealthHistory, finances.Wealth);
    }

    private void UpdateSingleChart(LineChart chart, IReadOnlyList<long> history, long currentValue)
    {
        FinancesChartUtility.PopulateChart(chart, history, currentValue, _maxDataPoints);
    }
}
