using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 재정(돈)을 관리하는 서비스 클래스
/// </summary>
public class FinancesDataHandler
{
    private readonly DataManager _dataManager;

    private long _credit;
    private long _creditDelta;
    public event Action OnCreditChanged;

    public long Credit => _credit;

    /// <summary>
    /// FinancesService 생성자
    /// </summary>
    public FinancesDataHandler(DataManager gameDataManager, InitialResourceData initData)
    {
        _dataManager = gameDataManager;
        _credit = initData.initialCredit;
    }

    public bool ModifyCredit(long credit)
    {
        _credit += credit;

        OnCreditChanged?.Invoke();
        return true;
    }

    public long CalculateDailyCreditDelta()
    {
        long credit = 0;

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

