using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 재정(돈)을 관리하는 서비스 클래스
/// </summary>
public class FinancesDataHandler
{
    private DataManager _dataManager;
    private InitialFinancesData _initialFinancesData;

    private long _credit;
    private long _creditDelta;

    public event Action OnCreditChanged;

    public long Credit => _credit;

    /// <summary>
    /// FinancesService 생성자
    /// </summary>
    public FinancesDataHandler(DataManager gameDataManager, InitialFinancesData initData)
    {
        _dataManager = gameDataManager;
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
        ModifyCredit(CalculateDailyCreditDelta());
    }

    public long CalculateDailyCreditDelta()
    {
        long credit = 0;

        credit -= _dataManager.Employee.CalculateTotalSalary();
        credit -= _dataManager.Resource.CalculateResourceDeltaChangeCredit();
        credit -= _dataManager.ThreadPlacement.CalculateTotalMaintenanceCostOfAllPlaced();

        return credit;
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnCreditChanged = null;
    }
}

