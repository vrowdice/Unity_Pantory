using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일일 비용 예약 구조체
/// </summary>
[Serializable]
public struct DailyExpenseReservation
{
    public long maintenanceCost;      // 유지비
    public long salaryCost;           // 직원 급여
    public long resourceShortageCost; // 자원 부족 비용
    public long playerTradeCost;      // 플레이어 거래 비용
    public long playerTradeRevenue;   // 플레이어 거래 수익
    
    public long TotalExpenses => maintenanceCost + salaryCost + resourceShortageCost + playerTradeCost;
    public long NetDelta => playerTradeRevenue - TotalExpenses;
}

/// <summary>
/// 게임 내 재정(돈)을 관리하는 서비스 클래스
/// </summary>
public class FinancesDataHandler
{
    // 현재 보유 금액
    private long _credit;

    // 금액 변경 이벤트
    public event Action OnCreditChanged;

    // 일일 비용 예약
    private DailyExpenseReservation _reservedDailyExpenses;
    public DailyExpenseReservation ReservedDailyExpenses => _reservedDailyExpenses;
    public bool IsProcessingReservedExpenses { get; private set; } = false;

    // 크레딧 소모 원인 추적 딕셔너리 (키: ID, 값: (가격, 설명))
    private Dictionary<string, (long cost, string reason)> _threadMaintenanceCosts = new Dictionary<string, (long, string)>();
    private Dictionary<string, (long cost, string reason)> _resourceShortageCosts = new Dictionary<string, (long, string)>();
    
    /// <summary>
    /// 스레드별 유지비 딕셔너리 (스레드 ID -> (가격, 설명))
    /// </summary>
    public Dictionary<string, (long cost, string reason)> ThreadMaintenanceCosts => new Dictionary<string, (long, string)>(_threadMaintenanceCosts);
    
    /// <summary>
    /// 자원별 부족 비용 딕셔너리 (자원 ID -> (가격, 설명))
    /// </summary>
    public Dictionary<string, (long cost, string reason)> ResourceShortageCosts => new Dictionary<string, (long, string)>(_resourceShortageCosts);

    // GameDataManager 참조 (비용 계산에 필요)
    private readonly GameDataManager _gameDataManager;

    /// <summary>
    /// FinancesService 생성자
    /// </summary>
    public FinancesDataHandler(GameDataManager gameDataManager)
    {
        _gameDataManager = gameDataManager;
        _credit = 0;
        _reservedDailyExpenses = new DailyExpenseReservation();
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
        OnCreditChanged?.Invoke();
    }

    // ----------------- 일일 비용 예약 시스템 -----------------

    /// <summary>
    /// 일일 예상 크레딧 변화량을 반환합니다 (예약된 비용 기준)
    /// </summary>
    public long CalculateDailyCreditDelta()
    {
        return _reservedDailyExpenses.NetDelta;
    }

    /// <summary>
    /// 일일 비용을 예약합니다 (다음 날 차감될 비용 계산)
    /// </summary>
    public void ReserveDailyExpenses()
    {
        if (_gameDataManager == null)
        {
            Debug.LogWarning("[FinancesDataHandler] GameDataManager is null. Cannot reserve daily expenses.");
            return;
        }

        _reservedDailyExpenses = new DailyExpenseReservation();

        // 1. 스레드 집계 (유지비)
        CalculateThreadAggregates(out long maintenanceCost, out var threadMaintenanceCosts);

        // 2. 자원 부족 비용 계산 (내부에서 생산/소비 계산)
        long shortageCost = CalculateResourceShortageCost(out var resourceShortageCosts, out var totalProduction, out var totalConsumption);

        // 3. 플레이어 자동 거래 비용/수익 계산
        CalculatePlayerTradeEconomics(totalProduction, totalConsumption, out long tradeCost, out long tradeRevenue);

        // 4. 직원 급여
        long salaryCost = _gameDataManager.Employee != null ? _gameDataManager.Employee.GetTotalSalary() : 0;
        
        _reservedDailyExpenses.maintenanceCost = maintenanceCost;
        _reservedDailyExpenses.salaryCost = salaryCost;
        _reservedDailyExpenses.resourceShortageCost = shortageCost;
        _reservedDailyExpenses.playerTradeCost = tradeCost;
        _reservedDailyExpenses.playerTradeRevenue = tradeRevenue;
        
        // 크레딧 소모 원인 정보 저장 (디버깅/UI 표시용)
        _threadMaintenanceCosts = threadMaintenanceCosts;
        _resourceShortageCosts = resourceShortageCosts;
    }

    /// <summary>
    /// 예약된 일일 비용을 적용합니다 (실제 돈 차감/지급)
    /// </summary>
    public void ApplyReservedDailyExpenses()
    {
        IsProcessingReservedExpenses = true;

        long expenses = _reservedDailyExpenses.TotalExpenses;
        long revenue = _reservedDailyExpenses.playerTradeRevenue;

        if (expenses > 0)
        {
            TryRemoveCredit(expenses);
        }
        if (revenue > 0)
        {
            AddCredit(revenue);
        }

        _reservedDailyExpenses = new DailyExpenseReservation(); // 초기화
        _threadMaintenanceCosts.Clear(); // 딕셔너리 초기화
        _resourceShortageCosts.Clear(); // 딕셔너리 초기화
        IsProcessingReservedExpenses = false;
    }

    /// <summary>
    /// 현재 배치된 스레드들의 유지비를 계산합니다.
    /// </summary>
    private void CalculateThreadAggregates(out long totalMaintenance, out Dictionary<string, (long cost, string reason)> threadMaintenanceCosts)
    {
        totalMaintenance = 0;
        threadMaintenanceCosts = new Dictionary<string, (long, string)>();

        if (_gameDataManager?.ThreadPlacement == null || _gameDataManager.Thread == null)
        {
            return;
        }

        var placedThreads = _gameDataManager.ThreadPlacement.GetAllPlacedThreads();
        if (placedThreads == null) return;

        foreach (var placement in placedThreads.Values)
        {
            if (placement == null || placement.RuntimeState == null) continue;

            // 각 배치된 인스턴스의 독립적인 상태를 가져옴
            ThreadState threadState = placement.RuntimeState;

            // 유지비 합산 및 스레드별 유지비 추적
            long maintenanceCost = threadState.totalMaintenanceCost;
            totalMaintenance += maintenanceCost;
            if (maintenanceCost > 0)
            {
                string reason = $"Thread '{threadState.threadId}' Maintenance";
                threadMaintenanceCosts[threadState.threadId] = (maintenanceCost, reason);
            }
        }
    }

    /// <summary>
    /// 자원 부족 비용을 계산합니다. 내부에서 생산/소비를 계산합니다.
    /// </summary>
    private long CalculateResourceShortageCost(out Dictionary<string, (long cost, string reason)> resourceShortageCosts, out Dictionary<string, long> totalProduction, out Dictionary<string, long> totalConsumption)
    {
        resourceShortageCosts = new Dictionary<string, (long, string)>();
        totalProduction = new Dictionary<string, long>();
        totalConsumption = new Dictionary<string, long>();
        
        if (_gameDataManager?.Resource == null || _gameDataManager?.ThreadPlacement == null || _gameDataManager.Thread == null)
        {
            return 0;
        }

        // 스레드에서 생산/소비 집계 (직원이 할당된 스레드만)
        var placedThreads = _gameDataManager.ThreadPlacement.GetAllPlacedThreads();
        if (placedThreads != null)
        {
            foreach (var placement in placedThreads.Values)
            {
                if (placement == null || placement.RuntimeState == null) continue;

                // 각 배치된 인스턴스의 독립적인 상태를 가져옴
                ThreadState threadState = placement.RuntimeState;
                if (threadState == null) continue;

                // [수정] 직원이 할당된 스레드만 생산/소비 집계에 포함
                // 생산 효율이 0이면 생산 진행도가 증가하지 않아 생산/소비가 실행되지 않음
                // 따라서 생산 효율이 0보다 큰 스레드만 집계
                bool hasEmployees = threadState.currentWorkers + threadState.currentTechnicians > 0;
                bool hasProductionEfficiency = threadState.currentProductionEfficiency > 0f;
                
                if (!hasEmployees && !hasProductionEfficiency)
                {
                    // 직원이 없고 생산 효율도 0이면 생산/소비하지 않음
                    continue;
                }

                // 자원 합산
                if (threadState.TryGetAggregatedResourceCounts(out var cons, out var prod))
                {
                    // 생산 효율에 비례하여 실제 생산/소비량 조정
                    float efficiencyMultiplier = hasProductionEfficiency 
                        ? threadState.currentProductionEfficiency 
                        : 0f;
                    
                    // 생산 효율이 있으면 그 비율만큼만 집계
                    if (efficiencyMultiplier > 0f)
                    {
                        AddToDictWithMultiplier(totalProduction, prod, efficiencyMultiplier);
                        AddToDictWithMultiplier(totalConsumption, cons, efficiencyMultiplier);
                    }
                }
            }
        }

        // 로컬 헬퍼
/*        void AddToDict(Dictionary<string, long> target, Dictionary<string, int> source)
        {
            if (source == null) return;
            foreach (var kvp in source)
            {
                if (target.ContainsKey(kvp.Key)) target[kvp.Key] += kvp.Value;
                else target[kvp.Key] = kvp.Value;
            }
        }*/

        // 생산 효율 배율을 적용한 헬퍼
        void AddToDictWithMultiplier(Dictionary<string, long> target, Dictionary<string, int> source, float multiplier)
        {
            if (source == null || multiplier <= 0f) return;
            foreach (var kvp in source)
            {
                long adjustedValue = (long)Mathf.Ceil(kvp.Value * multiplier);
                if (target.ContainsKey(kvp.Key)) target[kvp.Key] += adjustedValue;
                else target[kvp.Key] = adjustedValue;
            }
        }

        // 자원 부족 비용 계산
        long totalCost = 0;
        var allResources = _gameDataManager.Resource.GetAllResources();

        foreach (var kvp in totalConsumption)
        {
            string id = kvp.Key;
            long consumeAmount = kvp.Value;
            long produceAmount = totalProduction.ContainsKey(id) ? totalProduction[id] : 0;
            long currentAmount = _gameDataManager.Resource.GetResourceQuantity(id);

            // 예상 보유량 = 현재 + 생산 - 소비
            long expectedAmount = currentAmount + produceAmount - consumeAmount;

            if (expectedAmount < 0)
            {
                long shortage = -expectedAmount;
                if (allResources.TryGetValue(id, out var entry))
                {
                    float price = entry.resourceState?.currentValue ?? 0f;
                    long cost = (long)Math.Ceiling(price * shortage);
                    totalCost += cost;
                    string reason = $"Resource '{id}' Shortage (Shortage: {shortage}, Unit Price: {price:F2})";
                    resourceShortageCosts[id] = (cost, reason);
                }
            }
        }
        return totalCost;
    }

    /// <summary>
    /// 플레이어 자동 거래 비용/수익을 계산합니다.
    /// </summary>
    private void CalculatePlayerTradeEconomics(Dictionary<string, long> production, Dictionary<string, long> consumption, out long cost, out long revenue)
    {
        cost = 0;
        revenue = 0;

        if (_gameDataManager?.Resource == null || _gameDataManager.Market == null)
        {
            return;
        }

        var allResources = _gameDataManager.Resource.GetAllResources();
        float feeRate = _gameDataManager.Market.GetMarketFeeRate();

        foreach (var kvp in allResources)
        {
            string id = kvp.Key;
            var state = kvp.Value.resourceState;
            if (state == null || state.playerTransactionDelta == 0) continue;

            long delta = state.playerTransactionDelta;
            float price = state.currentValue;

            if (delta > 0) // 매수
            {
                long baseCost = (long)Mathf.Ceil(price * delta);
                cost += baseCost + (long)Mathf.Ceil(baseCost * feeRate);
            }
            else // 매도
            {
                long sellRequest = -delta;
                // 플레이어 재고 확인 (시장 재고가 아님!)
                long currentPlayerInventory = _gameDataManager.Resource.GetPlayerResourceQuantity(id);
                long prod = production.ContainsKey(id) ? production[id] : 0;
                long cons = consumption.ContainsKey(id) ? consumption[id] : 0;

                // 플레이어 재고 + 생산 - 소비 = 예상 플레이어 보유량
                long expectedPlayerAmount = currentPlayerInventory + prod - cons;
                // 실제 판매 가능 수량 = 요청량과 예상 보유량 중 작은 값 (0 이상)
                long actualSell = Math.Max(0, Math.Min(sellRequest, expectedPlayerAmount));

                if (actualSell > 0)
                {
                    long baseRevenue = (long)Mathf.Floor(price * actualSell);
                    revenue += baseRevenue - (long)Mathf.Floor(baseRevenue * feeRate);
                }
            }
        }
    }
}

