using System.Collections.Generic;
using Evo.UI;
using UnityEngine;

public static class FinancesChartUtility
{
    public static void PopulateChart(
        LineChart chart,
        IReadOnlyList<long> history,
        long currentValue,
        int maxDataPoints = 60)
    {
        if (chart == null)
        {
            return;
        }

        chart.dataPoints.Clear();

        int startIndex = Mathf.Max(0, history.Count - maxDataPoints);
        int totalPoints = (history.Count - startIndex) + 1;
        int labelStep = totalPoints <= 12 ? 1 : Mathf.Max(1, totalPoints / 10);

        for (int i = startIndex; i < history.Count; i++)
        {
            bool showLabel = i % labelStep == 0;
            string label = showLabel ? $"M{i + 1}" : string.Empty;
            chart.dataPoints.Add(new LineChart.DataPoint(label, history[i]));
        }

        chart.dataPoints.Add(new LineChart.DataPoint("Now", currentValue));
        chart.DrawChart();
    }
}
