using System;
using UnityEngine;

/// <summary>
/// 게임 내 재정(돈)을 관리하는 서비스 클래스
/// </summary>
public class FinancesDataHandler
{
    // 현재 보유 금액
    private long _credit;

    // 금액 변경 이벤트
    public event Action OnCreditChanged;

    /// <summary>
    /// FinancesService 생성자
    /// </summary>
    public FinancesDataHandler(GameDataManager gameDataManager)
    {
        _credit = 0;
    }

    // ----------------- Public Getters (읽기 전용) -----------------

    /// <summary>
    /// 현재 보유 금액을 반환합니다.
    /// </summary>
    /// <returns>보유 금액</returns>
    public long GetCredit()
    {
        return _credit;
    }

    // ----------------- Public Methods (재정 관리) -----------------

    /// <summary>
    /// 금액을 추가합니다.
    /// </summary>
    /// <param name="amount">추가할 금액</param>
    public void AddCredit(long amount)  
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[FinancesService] Amount to add must be greater than 0. (input: {amount})");
            return;
        }

        _credit += amount;
        Debug.Log($"[FinancesService] Credit +{amount} (total: {_credit})");
        
        OnCreditChanged?.Invoke();
    }

    /// <summary>
    /// 금액을 차감합니다. 금액이 부족하면 실패합니다.
    /// </summary>
    /// <param name="amount">차감할 금액</param>
    /// <returns>성공 시 true, 금액 부족 시 false</returns>
    public bool TryRemoveCredit(long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[FinancesService] Amount to remove must be greater than 0. (input: {amount})");
            return true;
        }

        if (_credit >= amount)
        {
            _credit -= amount;
            Debug.Log($"[FinancesService] Credit -{amount} (total: {_credit})");
            
            OnCreditChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning($"[FinancesService] Not enough credit! (required: {amount}, available: {_credit})");
            return false;
        }
    }

    /// <summary>
    /// 금액을 직접 설정합니다.
    /// </summary>
    /// <param name="amount">설정할 금액</param>
    public void SetCredit(long amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[FinancesService] Credit cannot be negative. (input: {amount})");
            return;
        }

        _credit = amount;
        Debug.Log($"[FinancesService] Credit = {amount}");
        
        OnCreditChanged?.Invoke();
    }

    /// <summary>
    /// 금액이 충분한지 확인합니다.
    /// </summary>
    /// <param name="amount">필요한 금액</param>
    /// <returns>충분하면 true, 부족하면 false</returns>
    public bool HasEnoughCredit(long amount)
    {
        return _credit >= amount;
    }

    /// <summary>
    /// 금액을 0으로 초기화합니다.
    /// </summary>
    public void ResetCredit()
    {
        _credit = 0;
        Debug.Log("[FinancesService] Credit has been reset.");
        
        OnCreditChanged?.Invoke();
    }
}

