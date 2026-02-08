using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설계 모드 UI를 관리하며, 스레드 저장/로드 및 관련 정보 표시를 담당합니다.
/// </summary>
public partial class DesignCanvas
{
    [Header("Thread References")]
    [SerializeField] private ThreadSaveInfoPopup _threadSaveInformationPanel;

    /// <summary>
    /// 저장 버튼 클릭 시 호출됩니다. 현재 배치된 건물들의 생산 체인과 유지비 등을 계산하여 요약 패널을 띄웁니다.
    /// </summary>
    /// <summary>
    /// 저장 버튼 클릭 시 호출됩니다. 현재 배치된 건물들의 생산 체인과 유지비 등을 계산하여 요약 패널을 띄웁니다.
    /// </summary>
    public void OnClickSaveButton()
    {
        string threadName = _designRunner.CurrentThreadId;

        List<string> inputResourceIdentifiers;
        Dictionary<string, int> inputResourceCounts;
        List<string> outputResourceIdentifiers;
        Dictionary<string, int> outputResourceCounts;

        _designRunner.CalculateProductionChain(
            threadName,
            out inputResourceIdentifiers,
            out inputResourceCounts,
            out outputResourceIdentifiers,
            out outputResourceCounts
        );

        int totalMaintenanceCost = _designRunner.CalculateTotalMaintenanceCost(threadName);

        _threadSaveInformationPanel.Init(
            inputResourceIdentifiers,
            inputResourceCounts,
            outputResourceIdentifiers,
            outputResourceCounts,
            totalMaintenanceCost,
            this
        );
    }

    /// <summary>
    /// 실제 저장 로직을 실행합니다. BuildingTileManager의 최종 저장 기능을 호출합니다.
    /// </summary>
    public void SaveThreadChanges(string threadName, string categoryIdentifier)
    {
        _designRunner.SaveThread(threadName, categoryIdentifier);
        GameManager.ShowWarningPanel(WarningMessage.SavedSuccessfully.Localize(LocalizationUtils.TABLE_WARNING_MESSAGE));

        DeselectBuilding();
    }

    /// <summary>
    /// 로드 버튼 클릭 시 호출됩니다. 스레드 관리 패널을 엽니다.
    /// </summary>
    public void OnClickLoadButton()
    {
        GameManager.ShowManageThreadPanel((string selectedThreadName) =>
        {
            LoadThread(selectedThreadName);
        });
    }

    /// <summary>
    /// 선택된 스레드 식별자를 기반으로 데이터를 불러오고 화면을 갱신합니다.
    /// </summary>
    private void LoadThread(string threadName)
    {
        if (string.IsNullOrEmpty(threadName))
        {
            return;
        }

        ThreadState threadState = DataManager.Thread.GetThread(threadName);
        if (threadState == null)
        {
            Debug.LogWarning("[DesignUiManager] Thread not found: " + threadName);
            return;
        }

        if (_designRunner != null)
        {
            _designRunner.LoadThread(threadName);
        }

        Debug.Log($"[DesignUiManager] Thread loaded: {threadName}");
    }
}