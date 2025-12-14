using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class EmployeeTopInfoPanel : MonoBehaviour
{
    [SerializeField] private Slider _managementSlider;
    [SerializeField] private Image _managementFillImage;
    [SerializeField] private TextMeshProUGUI _managementRatioText;

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
        
        // Update management ratio
        UpdateManagementRatio();
    }
    
    /// <summary>
    /// 관리 비율 슬라이더와 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateManagementRatio()
    {
        if (_dataManager == null || _dataManager.Employee == null)
        {
            return;
        }

        // GetManagementRatio()는 0.0 ~ 1.0 범위의 값을 반환
        float managementRatio = _dataManager.Employee.GetManagementRatio();

        // 슬라이더 업데이트 (0.0 ~ 1.0 범위)
        if (_managementSlider != null)
        {
            _managementSlider.value = managementRatio;
        }

        // 텍스트 업데이트 (간단하고 직관적인 표시)
        if (_managementRatioText != null)
        {
            _dataManager.Employee.GetManagementInfo(out int currentManagers, out int requiredManagers);
            
            if (requiredManagers <= 0 || currentManagers >= requiredManagers)
            {
                // 충분함: 깔끔하게 100%만 표시
                _managementRatioText.text = "100%";
            }
            else
            {
                // 부족함: 비율과 부족한 인원수만 괄호로 표시
                int shortage = requiredManagers - currentManagers;
                _managementRatioText.text = $"{managementRatio:P0} (-{shortage})"; // 예: "80% (-1)"
            }
        }
        
        // Fill 이미지 색상 업데이트
        if (_managementFillImage != null)
        {
            _dataManager.Employee.GetManagementInfo(out int currentManagers, out int requiredManagers);
            
            if (VisualManager.Instance != null)
            {
                if (requiredManagers <= 0 || currentManagers >= requiredManagers)
                {
                    // 충분함: VisualManager에서 색상 가져오기
                    _managementFillImage.color = VisualManager.Instance.ManagementSufficientColor;
                }
                else
                {
                    // 부족함: VisualManager에서 색상 가져오기
                    _managementFillImage.color = VisualManager.Instance.ManagementInsufficientColor;
                }
            }
            else
            {
                // VisualManager가 없으면 기본 색상 사용
                _managementFillImage.color = requiredManagers <= 0 || currentManagers >= requiredManagers 
                    ? Color.green 
                    : Color.red;
            }
        }
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
                    if (entry?.employeeData != null && entry.employeeData.type == type)
                    {
                        int count = entry.state.count;
                        int assigned = entry.state.assignedCount;
                        totalCount += count;
                        assignedCount += assigned;
                        
                        // Weight satisfaction by count (if there are multiple employee types with same role)
                        if (count > 0)
                        {
                            weightedSatisfactionSum += entry.state.currentSatisfaction * count;
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
        
        // 관리 비율 초기화
        if (_managementSlider != null) _managementSlider.value = 0f;
        if (_managementRatioText != null) _managementRatioText.text = "0%";
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
