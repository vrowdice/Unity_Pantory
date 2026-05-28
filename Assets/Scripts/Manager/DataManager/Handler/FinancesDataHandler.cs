using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 내 재정(돈)을 관리하는 서비스 클래스
/// </summary>
public class FinancesDataHandler : IDataHandlerEvents, ITimeChangeHandler, IMonthChangeHandler, IGameSaveHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialFinancesData _initialFinancesData;

    private long _credit;
    private long _wealth;

    private long _dailySalary;
    private long _dailyResource;
    private long _dailyMaintenance;
    private long _dailyPolicyCost;
    private long _dailyInterest;
    private long _dailyTotal;

    private long _placedBuildingsDailyMaintenance;
    private long _placedBuildingsAssetValue;

    private int _bankruptcyMonthsRemaining;
    private bool _skipNextBankruptcyDecrement;
    private int _lastBankruptcyMonthsRemaining = -1;

    private List<long> _monthlyCreditHistory = new List<long>();
    private List<long> _monthlyWealthHistory = new List<long>();

    public IReadOnlyList<long> MonthlyCreditHistory => _monthlyCreditHistory;
    public IReadOnlyList<long> MonthlyWealthHistory => _monthlyWealthHistory;

    public event Action OnCreditChanged;
    public event Action<int> OnBankruptcyCountdownChanged;
    public event Action OnBankruptcyTriggered;

    public long Credit => _credit;
    public long Wealth => _wealth;
    public int BankruptcyMonthsRemaining => _bankruptcyMonthsRemaining;
    public int BankruptcyGraceMonths => GetBankruptcyGraceMonths();
    public bool IsBankruptcyCountdownActive => _bankruptcyMonthsRemaining > 0 && !IsBankruptcyGameOver;
    public bool IsBankruptcyGameOver =>
        _dataManager?.GameOver != null
        && _dataManager.GameOver.IsGameOver
        && _dataManager.GameOver.CurrentGameOverType == GameOverType.Bankruptcy;

    public long DailySalary => _dailySalary;
    public long DailyResource => _dailyResource;
    public long DailyMaintenance => _dailyMaintenance;
    public long DailyInterest => _dailyInterest;
    public long DailyPolicyCost => _dailyPolicyCost;
    public long DailyTotal => _dailyTotal;

    /// <summary>
    /// FinancesService 생성자
    /// </summary>
    public FinancesDataHandler(DataManager dataManager, InitialFinancesData initData)
    {
        _dataManager = dataManager;
        _initialFinancesData = initData;
        _credit = initData != null ? initData.initialCredit : 0L;
    }

    public bool ModifyCredit(long credit)
    {
        _credit += credit;
        RefreshWealth();

        OnCreditChanged?.Invoke();
        return true;
    }

    public void HandleDayChanged()
    {
        CalculateDailyCreditDelta();
        ModifyCredit(_dailyTotal);
    }

    public void HandleMonthChanged()
    {
        _monthlyCreditHistory.Add(_credit);
        _monthlyWealthHistory.Add(_wealth);
        AdvanceBankruptcyCountdown();
    }

    public void CalculateDailyCreditDelta()
    {
        _dailySalary = _dataManager.Employee.CalculateTotalSalary();
        _dailyResource = _dataManager.Resource.TotalCreditChange;
        _dailyMaintenance = _placedBuildingsDailyMaintenance;
        _dailyPolicyCost = _dataManager.Policy != null ? _dataManager.Policy.CalculateDailyPolicyCost() : 0L;
        _dailyInterest = CalculateNegativeInterest();

        _dailyTotal = _dailyResource - _dailySalary - _dailyMaintenance - _dailyPolicyCost - _dailyInterest;
    }

    /// <summary>
    /// 메인 그리드에 건물/도로가 놓일 때 일일 유지비 합계에 더합니다.
    /// </summary>
    public void RegisterPlacedBuildingMaintenance(BuildingData data)
    {
        if (data == null) return;

        if (data.maintenanceCost > 0)
            _placedBuildingsDailyMaintenance += data.maintenanceCost;

        if (data.buildCost > 0)
            _placedBuildingsAssetValue += data.buildCost;

        RefreshWealth();
    }

    /// <summary>
    /// 건물/도로 제거 시 일일 유지비 합계에서 뺍니다.
    /// </summary>
    public void UnregisterPlacedBuildingMaintenance(BuildingData data)
    {
        if (data == null) return;

        if (data.maintenanceCost > 0)
        {
            _placedBuildingsDailyMaintenance -= data.maintenanceCost;
            if (_placedBuildingsDailyMaintenance < 0)
                _placedBuildingsDailyMaintenance = 0;
        }

        if (data.buildCost > 0)
        {
            _placedBuildingsAssetValue -= data.buildCost;
            if (_placedBuildingsAssetValue < 0)
                _placedBuildingsAssetValue = 0;
        }

        RefreshWealth();
    }

    /// <summary>
    /// 그리드 전체 삭제 등 일괄 정리 시 유지비 합계를 초기화합니다.
    /// </summary>
    public void ClearPlacedBuildingMaintenanceTotal()
    {
        _placedBuildingsDailyMaintenance = 0;
        _placedBuildingsAssetValue = 0;
        RefreshWealth();
    }

    public long CalculateNegativeInterest()
    {
        if (_credit >= 0 || _initialFinancesData == null) return 0;
        return (long)(Mathf.Abs(_credit) * _initialFinancesData.negativeInterestRate);
    }

    public long CalculateCurrentTotalWealth()
    {
        long currentCredit = _credit;
        long inventoryValue = _dataManager.Resource.GetAllResources().Values
            .Sum(entry => entry.state.count * entry.state.currentValue);

        return currentCredit + inventoryValue + _placedBuildingsAssetValue;
    }

    private void RefreshWealth()
    {
        _wealth = CalculateCurrentTotalWealth();
        UpdateBankruptcyStateOnWealthChanged();
    }

    private void UpdateBankruptcyStateOnWealthChanged()
    {
        if (IsBankruptcyGameOver || (_dataManager?.GameOver != null && _dataManager.GameOver.IsGameOver))
        {
            return;
        }

        if (_wealth >= 0)
        {
            if (_bankruptcyMonthsRemaining > 0)
            {
                _bankruptcyMonthsRemaining = 0;
                _skipNextBankruptcyDecrement = false;
                NotifyBankruptcyCountdownChanged(0);
            }
            return;
        }

        if (_bankruptcyMonthsRemaining > 0)
        {
            return;
        }

        _bankruptcyMonthsRemaining = GetBankruptcyGraceMonths();
        _skipNextBankruptcyDecrement = true;
        NotifyBankruptcyCountdownChanged(_bankruptcyMonthsRemaining);
    }

    private void AdvanceBankruptcyCountdown()
    {
        if (IsBankruptcyGameOver || (_dataManager?.GameOver != null && _dataManager.GameOver.IsGameOver))
        {
            return;
        }

        RefreshWealth();

        if (_wealth >= 0 || _bankruptcyMonthsRemaining <= 0)
        {
            return;
        }

        if (_skipNextBankruptcyDecrement)
        {
            _skipNextBankruptcyDecrement = false;
            return;
        }

        _bankruptcyMonthsRemaining--;
        NotifyBankruptcyCountdownChanged(_bankruptcyMonthsRemaining);

        if (_bankruptcyMonthsRemaining <= 0)
        {
            TriggerBankruptcyGameOver();
        }
    }

    private void NotifyBankruptcyCountdownChanged(int monthsRemaining, bool showWarning = true)
    {
        if (monthsRemaining > 0)
        {
            if (showWarning)
            {
                string messageKey = _lastBankruptcyMonthsRemaining <= 0
                    ? WarningMessage.BankruptcyCountdownStarted
                    : WarningMessage.BankruptcyCountdownTick;

                UIManager.Instance?.ShowWarningPopup(messageKey, monthsRemaining);
            }

            _lastBankruptcyMonthsRemaining = monthsRemaining;
        }
        else
        {
            _lastBankruptcyMonthsRemaining = 0;
        }

        OnBankruptcyCountdownChanged?.Invoke(monthsRemaining);
    }

    /// <summary>
    /// 파산 게임오버 후 타이틀 복귀 시 파산 카운트다운 상태를 초기화합니다.
    /// </summary>
    public void ResetBankruptcyStateForTitleReturn()
    {
        _bankruptcyMonthsRemaining = 0;
        _skipNextBankruptcyDecrement = false;
        _lastBankruptcyMonthsRemaining = 0;
    }

    /// <summary>
    /// 세이브 로드 후 플레이 씬 진입 시 파산 카운트다운 UI를 복원합니다.
    /// </summary>
    public void NotifyBankruptcyUiRestored()
    {
        if (IsBankruptcyGameOver)
        {
            OnBankruptcyTriggered?.Invoke();
            return;
        }

        if (_bankruptcyMonthsRemaining > 0)
        {
            OnBankruptcyCountdownChanged?.Invoke(_bankruptcyMonthsRemaining);
        }
    }

    private void TriggerBankruptcyGameOver()
    {
        if (IsBankruptcyGameOver)
        {
            return;
        }

        _dataManager.GameOver?.TriggerGameOver(GameOverType.Bankruptcy);
        OnBankruptcyTriggered?.Invoke();
    }

    private int GetBankruptcyGraceMonths()
    {
        if (_initialFinancesData == null || _initialFinancesData.bankruptcyGraceMonths < 1)
        {
            return 3;
        }

        return _initialFinancesData.bankruptcyGraceMonths;
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnCreditChanged = null;
        OnBankruptcyCountdownChanged = null;
        OnBankruptcyTriggered = null;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.credit = _credit;
        saveData.wealth = _wealth;
        saveData.monthlyCreditHistory = new List<long>(_monthlyCreditHistory);
        saveData.monthlyWealthHistory = new List<long>(_monthlyWealthHistory);
        saveData.bankruptcyMonthsRemaining = _bankruptcyMonthsRemaining;
        saveData.skipNextBankruptcyDecrement = _skipNextBankruptcyDecrement;
        saveData.isBankruptcyGameOver = IsBankruptcyGameOver;
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        _credit = saveData.credit;
        _monthlyCreditHistory = new List<long>(saveData.monthlyCreditHistory ?? new List<long>());
        _monthlyWealthHistory = new List<long>(saveData.monthlyWealthHistory ?? new List<long>());
        _bankruptcyMonthsRemaining = saveData.bankruptcyMonthsRemaining;
        _skipNextBankruptcyDecrement = saveData.skipNextBankruptcyDecrement;
        _lastBankruptcyMonthsRemaining = _bankruptcyMonthsRemaining;

        _wealth = CalculateCurrentTotalWealth();
        ReconcileBankruptcyStateAfterLoad();
    }

    private void ReconcileBankruptcyStateAfterLoad()
    {
        if (IsBankruptcyGameOver)
        {
            return;
        }

        if (_wealth >= 0)
        {
            if (_bankruptcyMonthsRemaining > 0)
            {
                _bankruptcyMonthsRemaining = 0;
                _skipNextBankruptcyDecrement = false;
            }

            _lastBankruptcyMonthsRemaining = 0;
            return;
        }

        if (_bankruptcyMonthsRemaining <= 0)
        {
            _bankruptcyMonthsRemaining = GetBankruptcyGraceMonths();
            _skipNextBankruptcyDecrement = true;
        }

        _lastBankruptcyMonthsRemaining = _bankruptcyMonthsRemaining;
    }
}
