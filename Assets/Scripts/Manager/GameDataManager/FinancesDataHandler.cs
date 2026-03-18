using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 내 재정(돈)을 관리하는 서비스 클래스
/// </summary>
public class FinancesDataHandler : IDataHandlerEvents, ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialFinancesData _initialFinancesData;

    private long _credit;
    private long _wealth;

    private long _dailySalary;
    private long _dailyResource;
    private long _dailyMaintenance;
    private long _dailyInterest;
    private long _dailyTotal;

    private List<long> _monthlyCreditHistory = new List<long>();
    private List<long> _monthlyWealthHistory = new List<long>();

    public IReadOnlyList<long> MonthlyCreditHistory => _monthlyCreditHistory;
    public IReadOnlyList<long> MonthlyWealthHistory => _monthlyWealthHistory;

    public event Action OnCreditChanged;

    public long Credit => _credit;
    public long Wealth => _wealth;

    public long DailySalary => _dailySalary;
    public long DailyResource => _dailyResource;
    public long DailyMaintenance => _dailyMaintenance;
    public long DailyInterest => _dailyInterest;
    public long DailyTotal => _dailyTotal;

    /// <summary>
    /// FinancesService 생성자
    /// </summary>
    public FinancesDataHandler(DataManager dataManager, InitialFinancesData initData)
    {
        _dataManager = dataManager;
        _initialFinancesData = initData;
        _credit = _initialFinancesData.initialCredit;
    }

    public bool ModifyCredit(long credit)
    {
        _credit += credit;

        OnCreditChanged?.Invoke();
        return true;
    }

    public void HandleDayChanged()
    {
        CalculateDailyCreditDelta();

        ModifyCredit(_dailyTotal);
        _wealth = CalculateCurrentTotalWealth();
    }

    public void HandleMonthChanged()
    {
        _monthlyCreditHistory.Add(_credit);
        _monthlyWealthHistory.Add(_wealth);
    }

    public void CalculateDailyCreditDelta()
    {
        _dailySalary = _dataManager.Employee.CalculateTotalSalary();
        _dailyResource = _dataManager.Resource.TotalCreditChange;
        // Thread/ThreadPlacement 시스템 제거: 건물 유지비는 추후 '메인 건물 설치 데이터' 기반으로 재구현
        _dailyMaintenance = 0;
        _dailyInterest = CalculateNegativeInterest();

        _dailyTotal = _dailyResource - _dailySalary - _dailyMaintenance - _dailyInterest;
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
        // Thread/ThreadPlacement 시스템 제거: 자산가치는 추후 '메인 건물 설치 데이터' 기반으로 재구현
        long assetValue = 0;

        return currentCredit + inventoryValue + assetValue;
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnCreditChanged = null;
    }
}
