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
    public event Action OnCreditChanged;

    public long Credit => _credit;

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnCreditChanged = null;
    }

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
        if(_credit == 0)
        {
            return false;
        }

        if(credit < 0)
        {
            if (_credit < credit)
            {
                return false;
            }
        }

        _credit += credit;
        return true;
    }

    public long CalculateDailyCreditDelta()
    {
        long credit = 0;

        return credit;
    }
}

