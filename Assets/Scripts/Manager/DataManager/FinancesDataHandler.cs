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

    private long _placedBuildingsDailyMaintenance;

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
        _credit = initData != null ? initData.initialCredit : 0L;
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
        _dailyMaintenance = _placedBuildingsDailyMaintenance;
        _dailyInterest = CalculateNegativeInterest();

        _dailyTotal = _dailyResource - _dailySalary - _dailyMaintenance - _dailyInterest;
    }

    /// <summary>
    /// 메인 그리드에 건물/도로가 놓일 때 일일 유지비 합계에 더합니다.
    /// </summary>
    public void RegisterPlacedBuildingMaintenance(BuildingData data)
    {
        if (data == null || data.maintenanceCost <= 0) return;
        _placedBuildingsDailyMaintenance += data.maintenanceCost;
    }

    /// <summary>
    /// 건물/도로 제거 시 일일 유지비 합계에서 뺍니다.
    /// </summary>
    public void UnregisterPlacedBuildingMaintenance(BuildingData data)
    {
        if (data == null || data.maintenanceCost <= 0) return;
        _placedBuildingsDailyMaintenance -= data.maintenanceCost;
        if (_placedBuildingsDailyMaintenance < 0)
            _placedBuildingsDailyMaintenance = 0;
    }

    /// <summary>
    /// 그리드 전체 삭제 등 일괄 정리 시 유지비 합계를 초기화합니다.
    /// </summary>
    public void ClearPlacedBuildingMaintenanceTotal()
    {
        _placedBuildingsDailyMaintenance = 0;
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
