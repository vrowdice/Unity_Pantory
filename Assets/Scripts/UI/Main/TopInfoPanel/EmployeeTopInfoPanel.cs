using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EmployeeTopInfoPanel : MonoBehaviour
{
    [Header("UI References - Worker")]
    [SerializeField] private TextMeshProUGUI _workerCountText;
    [SerializeField] private TextMeshProUGUI _workerSatisfactionText;

    [Header("UI References - Technician")]
    [SerializeField] private TextMeshProUGUI _technicianCountText;
    [SerializeField] private TextMeshProUGUI _technicianSatisfactionText;

    [Header("UI References - Researcher")]
    [SerializeField] private TextMeshProUGUI _researcherCountText;
    [SerializeField] private TextMeshProUGUI _researcherSatisfactionText;

    [Header("UI References - Manager")]
    [SerializeField] private TextMeshProUGUI _managerCountText;
    [SerializeField] private TextMeshProUGUI _managerSatisfactionText;

    private GameDataManager _dataManager;

    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;

        if (_dataManager == null)
        {
            Debug.LogWarning("[EmployeeTopInfoPanel] DataManager is null.");
            return;
        }

        if (_dataManager.Employee == null)
        {
            Debug.LogWarning("[EmployeeTopInfoPanel] Employee handler is null.");
            return;
        }

        // Subscribe to employee changed event
        _dataManager.Employee.OnEmployeeChanged += UpdateAllEmployeeInfo;

        // Initial update
        UpdateAllEmployeeInfo();
    }

    /// <summary>
    /// 모든 직원 타입의 정보를 업데이트합니다.
    /// </summary>
    private void UpdateAllEmployeeInfo()
    {
        if (_dataManager == null || _dataManager.Employee == null)
        {
            return;
        }

        // Update each employee type display
        UpdateEmployeeTypeDisplay(EmployeeType.Worker, _workerCountText, _workerSatisfactionText);
        UpdateEmployeeTypeDisplay(EmployeeType.Technician, _technicianCountText, _technicianSatisfactionText);
        UpdateEmployeeTypeDisplay(EmployeeType.Researcher, _researcherCountText, _researcherSatisfactionText);
        UpdateEmployeeTypeDisplay(EmployeeType.Manager, _managerCountText, _managerSatisfactionText);
    }

    /// <summary>
    /// 특정 직원 타입의 정보를 업데이트합니다.
    /// </summary>
    private void UpdateEmployeeTypeDisplay(EmployeeType type, TextMeshProUGUI countText, TextMeshProUGUI satisfactionText)
    {
        // Get total count, assigned count, and weighted average satisfaction for this type
        // If there are multiple employee entries with the same role, calculate weighted average
        int totalCount = 0;
        int assignedCount = 0;
        float weightedSatisfactionSum = 0f;
        bool hasEmployees = false;

        if (_dataManager?.Employee != null)
        {
            var allEmployees = _dataManager.Employee.GetAllEmployees();
            if (allEmployees != null)
            {
                foreach (var entry in allEmployees.Values)
                {
                    if (entry?.employeeData != null && entry.employeeData.role == type)
                    {
                        int count = entry.employeeState.count;
                        int assigned = entry.employeeState.assignedCount;
                        totalCount += count;
                        assignedCount += assigned;
                        
                        // Weight satisfaction by count (if there are multiple employee types with same role)
                        if (count > 0)
                        {
                            weightedSatisfactionSum += entry.employeeState.currentSatisfaction * count;
                            hasEmployees = true;
                        }
                    }
                }
            }
        }

        // Update count text: "전체 / 할당" 형식
        if (countText != null)
        {
            countText.text = $"{totalCount} / {assignedCount}";
        }

        // Update satisfaction text (weighted average if multiple types, or direct value if single type)
        if (satisfactionText != null)
        {
            float satisfaction = hasEmployees && totalCount > 0 
                ? weightedSatisfactionSum / totalCount 
                : 0f;
            satisfactionText.text = $"{satisfaction:F1}%";
        }
    }

    /// <summary>
    /// 모든 표시를 초기화합니다.
    /// </summary>
    private void ClearAllDisplays()
    {
        if (_workerCountText != null) _workerCountText.text = "0 / 0";
        if (_workerSatisfactionText != null) _workerSatisfactionText.text = "0%";
        
        if (_technicianCountText != null) _technicianCountText.text = "0 / 0";
        if (_technicianSatisfactionText != null) _technicianSatisfactionText.text = "0%";
        
        if (_researcherCountText != null) _researcherCountText.text = "0 / 0";
        if (_researcherSatisfactionText != null) _researcherSatisfactionText.text = "0%";
        
        if (_managerCountText != null) _managerCountText.text = "0 / 0";
        if (_managerSatisfactionText != null) _managerSatisfactionText.text = "0%";
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (_dataManager != null && _dataManager.Employee != null)
        {
            _dataManager.Employee.OnEmployeeChanged -= UpdateAllEmployeeInfo;
        }
    }
}
