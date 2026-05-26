using Evo.UI;
using TMPro;
using UnityEngine;

/// <summary>
/// 파산 게임오버 시 회사 성장 그래프와 최종 실적을 보여주는 팝업.
/// </summary>
public class BankruptcyGameOverPopup : PopupBase
{
    private const int MaxChartDataPoints = 60;

    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _summaryText;
    [SerializeField] private TextMeshProUGUI _finalCreditText;
    [SerializeField] private TextMeshProUGUI _finalWealthText;
    [SerializeField] private TextMeshProUGUI _runDurationText;
    [SerializeField] private TextMeshProUGUI _creditChartLabelText;
    [SerializeField] private TextMeshProUGUI _wealthChartLabelText;
    [SerializeField] private LineChart _creditChart;
    [SerializeField] private LineChart _wealthChart;

    public override void Init()
    {
        base.Init();
        RefreshContent();
        Show();
    }

    public void OnClickReturnToTitle()
    {
        DataManager.Instance?.Finances?.ResetBankruptcyStateForTitleReturn();
        UIManager.Instance?.ClearManagerCanvasPopups();

        Destroy(gameObject);
        SceneLoadManager.Instance?.LoadScene("Title");
    }

    private void RefreshContent()
    {
        FinancesDataHandler finances = _dataManager.Finances;
        TimeDataHandler time = _dataManager.Time;

        if (_titleText != null)
        {
            _titleText.text = GameOverPopupMessage.Title.Localize(LocalizationUtils.TABLE_COMMON);
        }

        if (_summaryText != null)
        {
            _summaryText.text = GameOverPopupMessage.Summary.Localize(LocalizationUtils.TABLE_COMMON);
        }

        if (_finalCreditText != null)
        {
            _finalCreditText.text = GameOverPopupMessage.FinalCredit.LocalizeFormat(
                LocalizationUtils.TABLE_COMMON,
                ReplaceUtils.FormatNumberWithCommas(finances.Credit));
        }

        if (_finalWealthText != null)
        {
            _finalWealthText.text = GameOverPopupMessage.FinalWealth.LocalizeFormat(
                LocalizationUtils.TABLE_COMMON,
                ReplaceUtils.FormatNumberWithCommas(finances.Wealth));
        }

        if (_runDurationText != null && time != null)
        {
            _runDurationText.text = GameOverPopupMessage.RunDuration.LocalizeFormat(
                LocalizationUtils.TABLE_COMMON,
                time.Year,
                time.Month + 1);
        }

        if (_creditChartLabelText != null)
        {
            _creditChartLabelText.text = GameOverPopupMessage.CreditChartLabel.Localize(LocalizationUtils.TABLE_COMMON);
        }

        if (_wealthChartLabelText != null)
        {
            _wealthChartLabelText.text = GameOverPopupMessage.WealthChartLabel.Localize(LocalizationUtils.TABLE_COMMON);
        }

        FinancesChartUtility.PopulateChart(
            _creditChart,
            finances.MonthlyCreditHistory,
            finances.Credit,
            MaxChartDataPoints);
        FinancesChartUtility.PopulateChart(
            _wealthChart,
            finances.MonthlyWealthHistory,
            finances.Wealth,
            MaxChartDataPoints);
    }
}
