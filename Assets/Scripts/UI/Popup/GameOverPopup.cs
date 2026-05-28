using TMPro;
using Evo.UI;
using UnityEngine;

/// <summary>
/// 파산·기한 만료·기업 1등 달성 등 게임 오버 시 회사 성장 그래프와 최종 실적을 보여주는 팝업.
/// </summary>
public class GameOverPopup : PopupBase
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

    [SerializeField] private GameObject _goTitleBtn;
    [SerializeField] private GameObject _continueBtn;

    private GameOverType _gameOverType = GameOverType.Bankruptcy;

    public override void Init()
    {
        Init(GameOverType.Bankruptcy);
    }

    public void Init(GameOverType gameOverType)
    {
        _gameOverType = gameOverType;
        base.Init();
        RefreshContent();
        Show();
    }

    public void OnClickGoTitle()
    {
        DataManager.Instance?.GameOver?.ResetForTitleReturn();
        DataManager.Instance?.Finances?.ResetBankruptcyStateForTitleReturn();
        UIManager.Instance?.ClearManagerCanvasPopups();

        Destroy(gameObject);
        SceneLoadManager.Instance?.LoadScene("Title");
    }

    public void OnClickContinue()
    {
        DataManager.Instance?.GameOver?.ClearGameOverForContinue();
        CloseAndDestroy();
    }

    private void RefreshContent()
    {
        FinancesDataHandler finances = _dataManager.Finances;
        TimeDataHandler time = _dataManager.Time;

        if (_titleText != null)
        {
            _titleText.text = GameOverPopupMessage.GetTitleKey(_gameOverType)
                .Localize(LocalizationUtils.TABLE_COMMON);
        }

        if (_summaryText != null)
        {
            _summaryText.text = GameOverPopupMessage.GetSummaryKey(_gameOverType)
                .Localize(LocalizationUtils.TABLE_COMMON);
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

        if (_goTitleBtn != null)
        {
            _goTitleBtn.SetActive(true);
        }

        if (_continueBtn != null)
        {
            _continueBtn.SetActive(_gameOverType == GameOverType.CompanyRankFirst);
        }
    }
}
