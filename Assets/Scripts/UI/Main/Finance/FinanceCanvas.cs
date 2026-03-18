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

        UpdateDailyUI();
        UpdateMonthlyUI();
    }

    private void OnDisable()
    {
        if (_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= UpdateDailyUI;
            _dataManager.Time.OnMonthChanged -= UpdateMonthlyUI;
        }
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
        _wealthText.text = ReplaceUtils.FormatNumberWithCommas(finances.Wealth);

        long lastMonthCredit = finances.MonthlyCreditHistory.LastOrDefault();
        long currentProfit = finances.Credit - lastMonthCredit;

        _monthlyProfitText.text = ReplaceUtils.FormatNumberWithCommas(currentProfit);
        _monthlyProfitText.color = VisualManager.Instance.GetDeltaColor(currentProfit);

        long lastMonthWealth = finances.MonthlyWealthHistory.LastOrDefault();
        long wealthChange = finances.Wealth - lastMonthWealth;
        
        float growthRate = 0f;
        if (lastMonthWealth != 0)
        {
            growthRate = ((float)wealthChange / lastMonthWealth) * 100f;
        }

        _monthlyGrowthRateText.text = $"{growthRate:F2}%";
        _monthlyGrowthRateText.color = VisualManager.Instance.GetDeltaColor(growthRate);
    }

    private void UpdateCharts()
    {
        FinancesDataHandler finances = _dataManager.Finances;
        UpdateSingleChart(_creditChart, finances.MonthlyCreditHistory, finances.Credit);
        UpdateSingleChart(_welthChart, finances.MonthlyWealthHistory, finances.Wealth);
    }

    private void UpdateSingleChart(LineChart chart, IReadOnlyList<long> history, long currentValue)
    {
        if (chart == null) return;
        chart.dataPoints.Clear();

        int startIndex = Mathf.Max(0, history.Count - _maxDataPoints);
        int totalPoints = (history.Count - startIndex) + 1;

        int labelStep = totalPoints <= 12 ? 1 : Mathf.Max(1, totalPoints / 10);
        for (int i = startIndex; i < history.Count; i++)
        {
            bool showLabel = (i % labelStep == 0);
            string label = showLabel ? $"M{i + 1}" : "";
            chart.dataPoints.Add(new LineChart.DataPoint(label, history[i]));
        }

        chart.dataPoints.Add(new LineChart.DataPoint("Now", currentValue));
        chart.DrawChart();
    }
}
