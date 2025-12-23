using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설계 모드 UI를 관리하며, 스레드 저장/로드 및 관련 정보 표시를 담당합니다.
/// </summary>
public partial class DesignUiManager
{
    [Header("Thread References")]
    [SerializeField] private ThreadSaveInfoPanel _threadSaveInformationPanel;

    private string _currentThreadTitle = DefaultThreadTitle;
    private const string DefaultThreadTitle = "Main Line";

    /// <summary>
    /// 저장 버튼 클릭 시 호출됩니다. 현재 배치된 건물들의 생산 체인과 유지비 등을 계산하여 요약 패널을 띄웁니다.
    /// </summary>
    public void OnClickSaveButton()
    {
        string threadIdentifier = _buildingTileManager.CurrentThreadId;
        string threadTitle = DefaultThreadTitle;

        // 스레드 식별자가 없는 경우 제목으로부터 생성 시도
        if (string.IsNullOrEmpty(threadIdentifier))
        {
            threadIdentifier = GetThreadIdFromTitle(threadTitle);
            _buildingTileManager.SetCurrentThread(threadIdentifier);
        }

        List<string> inputResourceIdentifiers;
        Dictionary<string, int> inputResourceCounts;
        List<string> outputResourceIdentifiers;
        Dictionary<string, int> outputResourceCounts;

        // 생산 체인 및 유지비 계산 (BuildingTileManager에 위임)
        _buildingTileManager.CalculateProductionChain(
            threadIdentifier,
            out inputResourceIdentifiers,
            out inputResourceCounts,
            out outputResourceIdentifiers,
            out outputResourceCounts
        );

        int totalMaintenanceCost = _buildingTileManager.CalculateTotalMaintenanceCost(threadIdentifier);

        // 직원 요구사항 계산
        int requiredEmployeeCount;
        CalculateThreadEmployeeRequirements(threadIdentifier, out requiredEmployeeCount);

        // 요약 패널 표시
        _threadSaveInformationPanel.OnInitialize(
            threadTitle,
            inputResourceIdentifiers,
            inputResourceCounts,
            outputResourceIdentifiers,
            outputResourceCounts,
            totalMaintenanceCost,
            this
        );

        Debug.Log("[DesignUiManager] Save information panel shown for Thread: " + threadIdentifier);
        Debug.Log("[DesignUiManager] Required Employees: " + requiredEmployeeCount);
    }

    /// <summary>
    /// 실제 저장 로직을 실행합니다. BuildingTileManager의 최종 저장 기능을 호출합니다.
    /// </summary>
    public void SaveThreadChanges(string threadName, string categoryIdentifier)
    {
        if (_buildingTileManager == null)
        {
            Debug.LogError("[DesignUiManager] Cannot save changes: BuildingTileManager is null.");
            return;
        }

        // 캡처 및 데이터 저장을 포함한 통합 저장 로직 실행
        _buildingTileManager.SaveThreadChanges(threadName, categoryIdentifier);

        _currentThreadTitle = threadName;

        DeselectBuilding();

        if (_gameManager != null)
        {
            _gameManager.ShowWarningPanel("Saved successfully.");
        }
    }

    /// <summary>
    /// 로드 버튼 클릭 시 호출됩니다. 스레드 관리 패널을 엽니다.
    /// </summary>
    public void OnClickLoadButton()
    {
        if (_gameManager == null || _dataManager == null)
        {
            return;
        }

        // 스레드 관리 패널 오픈 및 선택 콜백 등록
        _gameManager.ShowManageThreadPanel((string selectedThreadIdentifier) =>
        {
            LoadThread(selectedThreadIdentifier);
        });
    }

    /// <summary>
    /// 선택된 스레드 식별자를 기반으로 데이터를 불러오고 화면을 갱신합니다.
    /// </summary>
    private void LoadThread(string threadIdentifier)
    {
        if (string.IsNullOrEmpty(threadIdentifier) || _dataManager == null)
        {
            return;
        }

        ThreadState threadState = _dataManager.Thread.GetThread(threadIdentifier);
        if (threadState == null)
        {
            Debug.LogWarning("[DesignUiManager] Thread not found: " + threadIdentifier);
            return;
        }

        string threadTitle = string.IsNullOrEmpty(threadState.threadName) ? DefaultThreadTitle : threadState.threadName;
        _currentThreadTitle = threadTitle;

        // 타일 매니저에 현재 편집 스레드 설정 (화면 갱신 포함)
        if (_buildingTileManager != null)
        {
            _buildingTileManager.SetCurrentThread(threadIdentifier);
        }

        Debug.Log("[DesignUiManager] Thread loaded: " + threadIdentifier + " (" + threadState.threadName + ")");
    }

    /// <summary>
    /// 현재 화면에 배치된 건물들의 총 직원 요구사항을 계산합니다.
    /// </summary>
    private void CalculateThreadEmployeeRequirements(string threadIdentifier, out int requiredEmployeeCount)
    {
        requiredEmployeeCount = 0;

        if (_buildingTileManager == null || _dataManager == null || _dataManager.Building == null)
        {
            Debug.LogWarning("[DesignUiManager] Cannot calculate employee requirements: Required components are null.");
            return;
        }

        // 임시 저장 데이터를 포함한 현재 배치 상태 가져오기
        List<BuildingState> buildingStates = _buildingTileManager.GetCurrentBuildingStates();

        if (buildingStates != null)
        {
            foreach (BuildingState buildingState in buildingStates)
            {
                if (buildingState == null || string.IsNullOrEmpty(buildingState.buildingId))
                {
                    continue;
                }

                BuildingData buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    requiredEmployeeCount += buildingData.requiredEmployees;
                }
            }
        }
    }
}