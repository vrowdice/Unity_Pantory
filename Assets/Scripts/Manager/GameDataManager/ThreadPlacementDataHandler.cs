using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Thread 설치 위치 데이터를 관리하고 실제 배치된 스레드 인스턴스의 상태 및 계산을 담당합니다.
/// </summary>
public class ThreadPlacementDataHandler
{
    private readonly DataManager _dataManager;
    private readonly Dictionary<Vector2Int, ThreadPlacementState> _placedThreads = new Dictionary<Vector2Int, ThreadPlacementState>();

    public event Action OnPlacementChanged;

    /// <summary>
    /// ThreadPlacementDataHandler를 초기화합니다.
    /// </summary>
    /// <param name="gameDataManager">DataManager 인스턴스</param>
    public ThreadPlacementDataHandler(DataManager gameDataManager)
    {
        _dataManager = gameDataManager ?? throw new ArgumentNullException(nameof(gameDataManager));
        RefreshAllThreadCalculations();
    }

    /// <summary>
    /// 배치된 모든 스레드의 딕셔너리를 반환합니다.
    /// </summary>
    /// <returns>배치된 스레드의 읽기 전용 딕셔너리</returns>
    public Dictionary<Vector2Int, ThreadPlacementState> GetAllPlacedThreads()
    {
        return _placedThreads;
    }

    /// <summary>
    /// 템플릿 스레드를 복사하여 새로운 인스턴스를 배치합니다.
    /// </summary>
    /// <param name="gridPosition">배치할 그리드 위치</param>
    /// <param name="templateId">템플릿 스레드 ID</param>
    /// <returns>생성된 ThreadState 인스턴스, 실패 시 null</returns>
    public ThreadState PlaceThread(Vector2Int gridPosition, string templateId)
    {
        ThreadState template = _dataManager.Thread.GetThread(templateId);
        if (template == null)
        {
            Debug.LogError($"[PlaceThread] Template not found: {templateId}");
            return null;
        }

        ThreadState newState = CloneThreadState(template);

        newState.threadId = GenerateUniqueId(templateId, gridPosition);
        newState.threadName = $"{template.threadName} ({gridPosition.x}, {gridPosition.y})";

        RecalculateThreadStats(newState);

        ThreadPlacementState placement = new ThreadPlacementState(gridPosition, templateId, newState);
        _placedThreads[gridPosition] = placement;

        RaisePlacementChanged();

        return newState;
    }

    /// <summary>
    /// 특정 위치의 스레드 런타임 상태를 가져옵니다.
    /// </summary>
    /// <param name="gridPosition">조회할 그리드 위치</param>
    /// <returns>해당 위치의 ThreadState, 없으면 null</returns>
    public ThreadState GetThreadStateAt(Vector2Int gridPosition)
    {
        return _placedThreads.TryGetValue(gridPosition, out ThreadPlacementState placement) ? placement.RuntimeState : null;
    }

    /// <summary>
    /// ThreadId로 배치를 설정합니다.
    /// </summary>
    /// <param name="gridPosition">배치할 그리드 위치</param>
    /// <param name="threadId">스레드 ID</param>
    public void SetPlacedThread(Vector2Int gridPosition, string threadId)
    {
        if (_placedThreads.ContainsKey(gridPosition)) return;

        ThreadState threadState = _dataManager.Thread.GetThread(threadId);
        if (threadState != null)
        {
            ThreadPlacementState placement = new ThreadPlacementState(gridPosition, threadId, threadState);
            _placedThreads[gridPosition] = placement;
            RaisePlacementChanged();
        }
    }

    /// <summary>
    /// 특정 위치의 스레드를 제거합니다.
    /// </summary>
    /// <param name="gridPosition">제거할 그리드 위치</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemovePlacedThread(Vector2Int gridPosition)
    {
        if (_placedThreads.Remove(gridPosition))
        {
            RaisePlacementChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 모든 배치된 스레드를 제거합니다.
    /// </summary>
    public void ClearAll()
    {
        _placedThreads.Clear();
        RaisePlacementChanged();
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnPlacementChanged = null;
    }

    /// <summary>
    /// 스레드의 총 유지비를 계산합니다.
    /// </summary>
    /// <param name="threadIdentifier">스레드 식별자</param>
    /// <param name="buildingStates">건물 상태 리스트</param>
    /// <returns>총 유지비</returns>
    public int CalculateTotalMaintenanceCost(string threadIdentifier, List<BuildingState> buildingStates)
    {
        if (buildingStates == null || buildingStates.Count == 0)
        {
            return 0;
        }

        CalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(_dataManager, buildingStates);
        return stats.TotalMaintenanceCost;
    }

    /// <summary>
    /// 건물 상태 리스트로부터 총 유지비를 계산합니다.
    /// </summary>
    /// <param name="buildingStates">건물 상태 리스트</param>
    /// <returns>총 유지비</returns>
    public int CalculateTotalMaintenanceCost(List<BuildingState> buildingStates)
    {
        if (buildingStates == null || buildingStates.Count == 0) return 0;

        CalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(_dataManager, buildingStates);
        return stats.TotalMaintenanceCost;
    }

    /// <summary>
    /// 현재 배치된 모든 스레드의 총 유지비를 계산합니다.
    /// </summary>
    /// <returns>총 유지비</returns>
    public int CalculateTotalMaintenanceCostOfAllPlaced()
    {
        return _placedThreads.Values
            .Sum(placement => placement.RuntimeState?.totalMaintenanceCost ?? 0);
    }

    /// <summary>
    /// 스레드의 직원 요구사항을 계산합니다.
    /// </summary>
    /// <param name="threadIdentifier">스레드 식별자</param>
    /// <param name="buildingStates">건물 상태 리스트</param>
    /// <returns>필요한 직원 수</returns>
    public int CalculateRequiredEmployees(string threadIdentifier, List<BuildingState> buildingStates)
    {
        if (buildingStates == null || buildingStates.Count == 0)
        {
            return 0;
        }

        CalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(_dataManager, buildingStates);
        return stats.TotalRequiredEmployees;
    }

    /// <summary>
    /// 스레드의 생산 체인을 계산하여 자원을 집계합니다.
    /// </summary>
    /// <param name="threadIdentifier">스레드 식별자</param>
    /// <param name="buildingStates">건물 상태 리스트</param>
    /// <param name="inputResourceIdentifiers">입력 자원 식별자 리스트</param>
    /// <param name="inputResourceCounts">입력 자원 수량 딕셔너리</param>
    /// <param name="outputResourceIdentifiers">출력 자원 식별자 리스트</param>
    /// <param name="outputResourceCounts">출력 자원 수량 딕셔너리</param>
    public void CalculateProductionChain(
        string threadIdentifier,
        List<BuildingState> buildingStates,
        out List<string> inputResourceIdentifiers,
        out Dictionary<string, int> inputResourceCounts,
        out List<string> outputResourceIdentifiers,
        out Dictionary<string, int> outputResourceCounts)
    {
        if (string.IsNullOrEmpty(threadIdentifier) || buildingStates == null || buildingStates.Count == 0)
        {
            inputResourceIdentifiers = new List<string>();
            inputResourceCounts = new Dictionary<string, int>();
            outputResourceIdentifiers = new List<string>();
            outputResourceCounts = new Dictionary<string, int>();
            return;
        }

        if (_dataManager == null || _dataManager.Building == null)
        {
            inputResourceIdentifiers = new List<string>();
            inputResourceCounts = new Dictionary<string, int>();
            outputResourceIdentifiers = new List<string>();
            outputResourceCounts = new Dictionary<string, int>();
            return;
        }

        CalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(_dataManager, buildingStates);
        inputResourceCounts = stats.InputResourceCounts;
        outputResourceCounts = stats.OutputResourceCounts;

        inputResourceIdentifiers = new List<string>(inputResourceCounts.Keys);
        outputResourceIdentifiers = new List<string>(outputResourceCounts.Keys);
    }

    /// <summary>
    /// 생산 체인을 계산합니다.
    /// </summary>
    /// <param name="buildingStates">건물 상태 리스트</param>
    /// <param name="inputIds">입력 자원 식별자 리스트</param>
    /// <param name="inputCounts">입력 자원 수량 딕셔너리</param>
    /// <param name="outputIds">출력 자원 식별자 리스트</param>
    /// <param name="outputCounts">출력 자원 수량 딕셔너리</param>
    public void CalculateProductionChain(
        List<BuildingState> buildingStates,
        out List<string> inputIds, out Dictionary<string, int> inputCounts,
        out List<string> outputIds, out Dictionary<string, int> outputCounts)
    {
        inputIds = new List<string>();
        outputIds = new List<string>();
        inputCounts = new Dictionary<string, int>();
        outputCounts = new Dictionary<string, int>();

        if (buildingStates == null || buildingStates.Count == 0) return;

        CalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(_dataManager, buildingStates);
        inputCounts = stats.InputResourceCounts;
        outputCounts = stats.OutputResourceCounts;

        inputIds.AddRange(inputCounts.Keys);
        outputIds.AddRange(outputCounts.Keys);
    }

    /// <summary>
    /// 저장된 모든 템플릿 스레드의 스탯을 갱신합니다.
    /// </summary>
    public void RefreshAllThreadCalculations()
    {
        Dictionary<string, ThreadState> allThreads = _dataManager.Thread.GetAllThreads();
        foreach (ThreadState thread in allThreads.Values)
        {
            RecalculateThreadStats(thread);
        }
    }

    private void RaisePlacementChanged() => OnPlacementChanged?.Invoke();

    private ThreadState CloneThreadState(ThreadState source)
    {
        string json = JsonUtility.ToJson(source);
        ThreadState clone = JsonUtility.FromJson<ThreadState>(json);
        return clone;
    }

    private string GenerateUniqueId(string templateId, Vector2Int pos)
    {
        return $"{templateId}_{pos.x}_{pos.y}_{Guid.NewGuid().ToString().Substring(0, 8)}";
    }

    private void RecalculateThreadStats(ThreadState state)
    {
        if (state == null || state.buildingStateList == null) return;

        List<BuildingState> buildings = state.buildingStateList;

        CalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(_dataManager, buildings);

        state.totalMaintenanceCost = stats.TotalMaintenanceCost;
        state.requiredEmployees = stats.TotalRequiredEmployees;
    }

    /// <summary>
    /// 일 경과 시 배치된 스레드의 생산 진행도를 업데이트하고 자원 델타를 적용합니다.
    /// </summary>
    public void HandleDayChanged()
    {
        UpdateResourceDeltasFromPlacedThreads();
    }

    /// <summary>
    /// 배치된 스레드의 생산/소비를 계산합니다
    /// </summary>
    private void UpdateResourceDeltasFromPlacedThreads()
    {
        if (_placedThreads == null) return;

        foreach (ThreadPlacementState placement in _placedThreads.Values)
        {
            if (placement == null || placement.RuntimeState == null) continue;

            ThreadState threadState = placement.RuntimeState;
            UpdateThreadProductionProgress(threadState);

            if (threadState.currentProductionProgress >= 1.0f)
            {
                int productionCount = Mathf.FloorToInt(threadState.currentProductionProgress);

                CalculationResult stats = BuildingCalculationUtility.CalculateProductionStats(_dataManager, threadState.buildingStateList);
                Dictionary<string, int> cons = stats.InputResourceCounts;
                Dictionary<string, int> prod = stats.OutputResourceCounts;

                if (cons.Count > 0 || prod.Count > 0)
                {
                    ModifyPlayerProduction(prod, productionCount);
                    ModifyPlayerProduction(cons, -productionCount);
                }

                threadState.currentProductionProgress -= productionCount;
            }
        }
    }

    /// <summary>
    /// 스레드의 생산 진행도와 효율을 업데이트합니다
    /// </summary>
    private void UpdateThreadProductionProgress(ThreadState threadState)
    {
        if (threadState == null) return;

        float quantityEfficiency = 0f;
        float qualityEfficiency = 1.0f;

        if (threadState.requiredEmployees > 0)
        {
            int currentEmployees = threadState.currentWorkers + threadState.currentTechnicians;
            quantityEfficiency = Mathf.Clamp01((float)currentEmployees / threadState.requiredEmployees);

            if (_dataManager.Employee != null && currentEmployees > 0)
            {
                float totalEfficiencySum = 0f;
                if (threadState.currentWorkers > 0)
                {
                    EmployeeEntry workerEntry = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Worker);
                    float workerEff = workerEntry.state.currentEfficiency;
                    totalEfficiencySum += threadState.currentWorkers * workerEff;
                }

                if (threadState.currentTechnicians > 0)
                {
                    EmployeeEntry techEntry = _dataManager.Employee.GetEmployeeEntry(EmployeeType.Technician);
                    float techEff = techEntry.state.currentEfficiency;
                    totalEfficiencySum += threadState.currentTechnicians * techEff;
                }

                qualityEfficiency = totalEfficiencySum / currentEmployees;
            }
        }
        else
        {
            quantityEfficiency = 0f;
        }

        threadState.currentProductionEfficiency = quantityEfficiency * qualityEfficiency;
        threadState.currentProductionProgress += threadState.currentProductionEfficiency;
    }

    /// <summary>
    /// 스레드의 생산품 가감을 적용합니다
    /// </summary>
    private void ModifyPlayerProduction(Dictionary<string, int> resourceDic, int multiplier)
    {
        foreach (KeyValuePair<string, int> kvp in resourceDic)
        {
            string resourceId = kvp.Key;
            int requiredAmount = kvp.Value * multiplier;

            _dataManager.Resource.ModifyThreadDelta(resourceId, requiredAmount);
        }
    }
}
