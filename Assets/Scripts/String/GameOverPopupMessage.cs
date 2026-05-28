/// <summary>
/// 게임 오버 팝업 로컬라이즈 키 (Common 테이블).
/// </summary>
public static class GameOverPopupMessage
{
    public const string TitleBankruptcy = "BankruptcyGameOverPopupTitle";
    public const string SummaryBankruptcy = "BankruptcyGameOverPopupSummary";
    public const string TitleGovernmentDissolution = "GameOverPopupTitle_GovernmentDissolution";
    public const string SummaryGovernmentDissolution = "GameOverPopupSummary_GovernmentDissolution";
    public const string TitleCompanyRankFirst = "GameOverPopupTitle_CompanyRankFirst";
    public const string SummaryCompanyRankFirst = "GameOverPopupSummary_CompanyRankFirst";
    public const string FinalCredit = "BankruptcyGameOverPopupFinalCredit";
    public const string FinalWealth = "BankruptcyGameOverPopupFinalWealth";
    public const string RunDuration = "BankruptcyGameOverPopupRunDuration";
    public const string CreditChartLabel = "BankruptcyGameOverPopupCreditChart";
    public const string WealthChartLabel = "BankruptcyGameOverPopupWealthChart";
    public const string Continue = "GameOverContinue";

    public static string GetTitleKey(GameOverType type)
    {
        switch (type)
        {
            case GameOverType.GovernmentDissolution:
                return TitleGovernmentDissolution;
            case GameOverType.CompanyRankFirst:
                return TitleCompanyRankFirst;
            default:
                return TitleBankruptcy;
        }
    }

    public static string GetSummaryKey(GameOverType type)
    {
        switch (type)
        {
            case GameOverType.GovernmentDissolution:
                return SummaryGovernmentDissolution;
            case GameOverType.CompanyRankFirst:
                return SummaryCompanyRankFirst;
            default:
                return SummaryBankruptcy;
        }
    }
}
