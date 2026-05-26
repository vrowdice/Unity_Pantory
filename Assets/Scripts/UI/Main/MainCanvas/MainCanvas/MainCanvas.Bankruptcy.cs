using UnityEngine;

public partial class MainCanvas
{
    [Header("Bankruptcy")]
    [SerializeField] private GameOverTimer _gameOverTimer;

    private void InitBankruptcyUi()
    {
        DataManager.Finances.OnBankruptcyCountdownChanged -= HandleBankruptcyCountdownChanged;
        DataManager.Finances.OnBankruptcyCountdownChanged += HandleBankruptcyCountdownChanged;
        DataManager.Finances.OnBankruptcyTriggered -= HandleBankruptcyTriggered;
        DataManager.Finances.OnBankruptcyTriggered += HandleBankruptcyTriggered;

        RefreshBankruptcyUi(playShowAnimation: false);
    }

    private void CleanupBankruptcyUi()
    {
        if (DataManager?.Finances == null)
        {
            return;
        }

        DataManager.Finances.OnBankruptcyCountdownChanged -= HandleBankruptcyCountdownChanged;
        DataManager.Finances.OnBankruptcyTriggered -= HandleBankruptcyTriggered;
    }

    private void HandleBankruptcyCountdownChanged(int monthsRemaining)
    {
        RefreshBankruptcyUi(playShowAnimation: true);
    }

    private void HandleBankruptcyTriggered()
    {
        RefreshBankruptcyUi(playShowAnimation: false);
    }

    private void RefreshBankruptcyUi(bool playShowAnimation)
    {
        if (_gameOverTimer == null)
        {
            return;
        }

        FinancesDataHandler finances = DataManager.Finances;

        if (finances.IsBankruptcyCountdownActive)
        {
            _gameOverTimer.ApplyCountdown(
                finances.BankruptcyMonthsRemaining,
                finances.BankruptcyGraceMonths,
                playShowAnimation);
            return;
        }

        _gameOverTimer.Hide();
    }
}
