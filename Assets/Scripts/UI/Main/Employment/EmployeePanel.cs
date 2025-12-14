using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// 직원 관리 패널
/// </summary>
public class EmployeePanel : BasePanel
{
    #region Inspector Variables
    [SerializeField] private Slider _managementSlider;
    [SerializeField] private Image _managementFillImage;
    [SerializeField] private TextMeshProUGUI _managementRatioText;

    [Header("Role Buttons")]
    [SerializeField] private Transform EmployeeActionBtnContent;

    [Header("Basic Information")]
    [SerializeField] private Image _employeeImage;
    [SerializeField] private Image _employeeIconImage;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("Employee Stats")]
    [SerializeField] private TextMeshProUGUI _employeeCountText;
    [SerializeField] private TextMeshProUGUI _totalSalatyText;

    [Header("Efficiency")]
    [SerializeField] private Slider _efficiencySlider;
    [SerializeField] private TextMeshProUGUI _efficiencyValueText;

    [Header("Satisfaction")]
    [SerializeField] private Slider _satisfactionSlider;
    [SerializeField] private TextMeshProUGUI _satisfactionValueText;

    [Header("Salary Level")]
    [SerializeField] private List<Toggle> _salaryLevelToggles;

    [Header("Status Scroll View Contents")]
    [SerializeField] private GameObject _efficiencyStatusTextPairPanelPrefab;
    [SerializeField] private Transform _efficiencyStatusScrollViewContentTransform;
    [SerializeField] private Transform _satisfactionStatusScrollViewContentTransform;
    #endregion

    #region Private Variables

    private EmployeeEntry _selectedEmployeeEntry;
    private EmployeeType _selectedEmployeeType;
    private bool _isSubscribedToDayChange;
    private bool _isSubscribedToEmployeeChanged;
    private bool _isUpdatingToggles; // 토글 업데이트 중 플래그 (무한 루프 방지)

    #endregion

    #region Initialization

    /// <summary>
    /// (BasePanel)
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[EmployeePanel] DataManager is null.");
            return;
        }

        SetupEmployeeRoleButtons();
        SubscribeToDayChange();
        SubscribeToEmployeeChanged();
        SelectFirstEmployee();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 직원 수를 delta 값만큼 변경
    /// 양수면 증가, 음수면 감소
    /// </summary>
    /// <param name="delta">변경할 수량 (양수: 증가, 음수: 감소)</param>
    public void ChangeEmployeeCount(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        if (delta > 0)
        {
            // 직원 고용
            _dataManager.Employee.HireEmployee(_selectedEmployeeType, delta);
        }
        else
        {
            // 직원 해고 (delta가 음수이므로 절댓값 사용)
            int fireCount = Mathf.Abs(delta);
            _dataManager.Employee.TryFireEmployee(_selectedEmployeeType, fireCount);
        }

        // UI는 HandleEmployeeChanged 이벤트를 통해 자동으로 갱신됨
    }

    /// <summary>
    /// 선택된 직원의 급여 레벨을 설정합니다.
    /// </summary>
    /// <param name="salaryLevel">급여 레벨 (0=매우 적음, 1=적음, 2=보통, 3=많음, 4=매우 많음)</param>
    public void SetSelectedEmployeeSalaryLevel(int salaryLevel)
    {
        if (_isUpdatingToggles)
        {
            return;
        }

        _dataManager.Employee.SetEmployeeSalaryLevel(_selectedEmployeeType, salaryLevel);
    }

    /// <summary>
    /// 선택된 직원의 현재 급여 레벨을 반환합니다.
    /// </summary>
    /// <returns>현재 급여 레벨 (0~4), 선택된 직원이 없으면 -1</returns>
    public int GetSelectedEmployeeSalaryLevel()
    {
        return _dataManager.Employee.GetEmployeeSalaryLevel(_selectedEmployeeType);
    }

    /// <summary>
    /// 직원 버튼 클릭 핸들러
    /// </summary>
    public void HandleEmployeeButtonClicked(EmployeeEntry entry)
    {
        if (entry == null || entry.employeeData == null)
        {
            return;
        }

        _selectedEmployeeEntry = entry;
        _selectedEmployeeType = entry.employeeData.type;
        UpdateEmployeeUI();
    }

    #endregion

    #region Role Button Setup

    /// <summary>
    /// 직원 역할별 버튼 생성
    /// </summary>
    private void SetupEmployeeRoleButtons()
    {
        if (_gameManager?.ActionBtnPrefab == null || EmployeeActionBtnContent == null)
        {
            Debug.LogWarning("[EmployeePanel] ActionBtnPrefab or EmployeeActionBtnContent is null.");
            return;
        }

        GameObjectUtils.ClearChildren(EmployeeActionBtnContent);

        // EmployeeType enum의 모든 역할에 대해 버튼 생성
        System.Array employeeTypes = System.Enum.GetValues(typeof(EmployeeType));
        
        foreach (EmployeeType role in employeeTypes)
        {
            var btnObj = Instantiate(_gameManager.ActionBtnPrefab, EmployeeActionBtnContent);
            var btn = btnObj.GetComponent<ActionBtn>();
            if (btn != null)
            {
                string roleName = role.ToString();
                btn.OnInitialize(roleName, () => ShowEmployeeByRole(role));
            }
        }
    }

    /// <summary>
    /// 특정 역할의 첫 번째 직원 표시
    /// </summary>
    private void ShowEmployeeByRole(EmployeeType role)
    {
        if (_dataManager?.Employee == null)
        {
            return;
        }

        var allEmployees = _dataManager.Employee.GetAllEmployees();
        if (allEmployees == null || allEmployees.Count == 0)
        {
            return;
        }

        // 해당 역할의 첫 번째 직원 찾기
        EmployeeEntry foundEntry = null;
        foreach (var entry in allEmployees.Values)
        {
            if (entry?.employeeData != null && entry.employeeData.type == role)
            {
                foundEntry = entry;
                break;
            }
        }

        if (foundEntry != null)
        {
            HandleEmployeeButtonClicked(foundEntry);
        }
        else
        {
            Debug.LogWarning($"[EmployeePanel] No employee found for role: {role}");
        }
    }

    #endregion

    #region Employee Selection

    /// <summary>
    /// 첫 번째 직원 선택
    /// </summary>
    private void SelectFirstEmployee()
    {
        if (_dataManager?.Employee == null)
        {
            return;
        }

        var allEmployees = _dataManager.Employee.GetAllEmployees();
        if (allEmployees == null || allEmployees.Count == 0)
        {
            return;
        }

        // 첫 번째 직원 선택
        foreach (var entry in allEmployees.Values)
        {
            if (entry != null && entry.employeeData != null)
            {
                HandleEmployeeButtonClicked(entry);
                break;
            }
        }
    }

    #endregion

    #region UI Update

    /// <summary>
    /// 직원 UI 정보 업데이트
    /// </summary>
    private void UpdateEmployeeUI()
    {
        if (_selectedEmployeeEntry == null || _selectedEmployeeEntry.employeeData == null)
        {
            ClearEmployeeUI();
            return;
        }

        var data = _selectedEmployeeEntry.employeeData;
        var state = _selectedEmployeeEntry.state;

        // 기본 정보
        if (_employeeImage != null && data.Image != null)
        {
            _employeeImage.sprite = data.Image;
        }

        if (_employeeIconImage != null && data.icon != null)
        {
            _employeeIconImage.sprite = data.icon;
        }

        if (_titleText != null)
        {
            _titleText.text = data.displayName ?? data.id;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = data.description ?? string.Empty;
        }

        // 직원 수 및 급여
        if (_employeeCountText != null)
        {
            _employeeCountText.text = $"Count: {state.count}";
        }

        if (_totalSalatyText != null)
        {
            _totalSalatyText.text = $"Total Salary: {state.totalSalary:N0}";
        }

        // 효율성 슬라이더 (0~200% 범위를 0~1로 정규화: 0.0 = 0%, 1.0 = 100%, 2.0 = 200%)
        if (_efficiencySlider != null)
        {
            // currentEfficiency는 0.0~2.0 범위, 슬라이더는 0.0~1.0 범위
            float normalizedEfficiency = Mathf.Clamp(state.currentEfficiency, 0f, 2f) / 2f;
            _efficiencySlider.value = normalizedEfficiency;
        }

        if (_efficiencyValueText != null)
        {
            _efficiencyValueText.text = $"{(state.currentEfficiency * 100f):F1}%";
        }

        // 만족도 슬라이더 (-100~100 범위를 0~1로 정규화)
        if (_satisfactionSlider != null)
        {
            float normalizedSatisfaction = (state.currentSatisfaction + 100f) / 200f;
            _satisfactionSlider.value = Mathf.Clamp01(normalizedSatisfaction);
        }

        if (_satisfactionValueText != null)
        {
            _satisfactionValueText.text = $"{state.currentSatisfaction:F1}";
        }

        // 급여 레벨 토글 업데이트
        UpdateSalaryLevelToggles(state.salaryLevel);
        
        // 관리 비율 업데이트
        UpdateManagementRatio();
        
        // 이펙트 정보 업데이트
        UpdateEffectStatus();
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
    /// 급여 레벨 토글 상태 업데이트
    /// </summary>
    /// <param name="salaryLevel">현재 급여 레벨 (0~4)</param>
    private void UpdateSalaryLevelToggles(int salaryLevel)
    {
        if (_salaryLevelToggles == null || _salaryLevelToggles.Count == 0)
        {
            return;
        }

        // 급여 레벨 범위 검증
        salaryLevel = Mathf.Clamp(salaryLevel, 0, 4);

        // 토글 업데이트 중 플래그 설정 (무한 루프 방지)
        _isUpdatingToggles = true;

        try
        {
            // 각 토글의 상태 업데이트 (순서대로 0~4에 매핑)
            for (int i = 0; i < _salaryLevelToggles.Count && i <= 4; i++)
            {
                if (_salaryLevelToggles[i] != null)
                {
                    // 현재 상태와 다를 때만 업데이트 (불필요한 이벤트 발생 방지)
                    if (_salaryLevelToggles[i].isOn != (i == salaryLevel))
                    {
                        _salaryLevelToggles[i].isOn = (i == salaryLevel);
                    }
                }
            }
        }
        finally
        {
            // 플래그 해제
            _isUpdatingToggles = false;
        }
    }

    /// <summary>
    /// 직원 UI 초기화
    /// </summary>
    private void ClearEmployeeUI()
    {
        if (_titleText != null) _titleText.text = string.Empty;
        if (_descriptionText != null) _descriptionText.text = string.Empty;
        if (_employeeCountText != null) _employeeCountText.text = "Count: 0";
        if (_totalSalatyText != null) _totalSalatyText.text = "Total Salary: 0";
        if (_efficiencySlider != null) _efficiencySlider.value = 0f;
        if (_efficiencyValueText != null) _efficiencyValueText.text = "0%";
        if (_satisfactionSlider != null) _satisfactionSlider.value = 0f;
        if (_satisfactionValueText != null) _satisfactionValueText.text = "0";
        
        // 급여 레벨 토글 초기화 (기본값: 보통 = 2)
        UpdateSalaryLevelToggles(2);
        
        // 관리 비율 초기화
        if (_managementSlider != null) _managementSlider.value = 0f;
        if (_managementRatioText != null) _managementRatioText.text = "0%";
        
        // 이펙트 상태 초기화
        ClearEffectStatus();
    }

    /// <summary>
    /// 직원 UI 갱신
    /// </summary>
    private void RefreshEmployeeUI()
    {
        var entry = _dataManager.Employee.GetEmployeeEntry(_selectedEmployeeType);
        if (entry != null)
        {
            _selectedEmployeeEntry = entry;
            UpdateEmployeeUI();
        }
    }

    #endregion

    #region Effect Status Display

    /// <summary>
    /// 효율성 및 만족도 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateEffectStatus()
    {
        if (_dataManager?.Effect == null)
        {
            ClearEffectStatus();
            return;
        }

        // 효율성 관련 이펙트 표시 (만족도 이펙트를 표시 - 만족도가 효율성에 영향을 주므로)
        UpdateEfficiencyEffectStatus();
        
        // 만족도 관련 이펙트 표시
        UpdateSatisfactionEffectStatus();
    }

    /// <summary>
    /// 효율성 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    /// <summary>
    /// 효율성 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateEfficiencyEffectStatus()
    {
        if (_efficiencyStatusScrollViewContentTransform == null || 
            _efficiencyStatusTextPairPanelPrefab == null ||
            _dataManager?.Effect == null)
        {
            return;
        }

        // 기존 패널 제거
        GameObjectUtils.ClearChildren(_efficiencyStatusScrollViewContentTransform);

        var combinedEffects = new List<EffectState>();

        // 1. 전역 효율성 이펙트
        var globalEffects = _dataManager.Effect.GetActiveEffects(StatType.EfficiencyBonus);
        if (globalEffects != null) 
        {
            combinedEffects.AddRange(globalEffects);
        }

        // 2. 직원 개별 효율성 이펙트
        if (_selectedEmployeeEntry?.state?.activeEffects != null)
        {
            var localEffects = _selectedEmployeeEntry.state.activeEffects
                .Where(e => e.Data.statType == StatType.EfficiencyBonus);
            combinedEffects.AddRange(localEffects);
        }

        if (combinedEffects.Count > 0)
        {
            // 각 이펙트에 대해 패널 생성
            foreach (var effectState in combinedEffects)
            {
                if (effectState?.Data == null) continue;

                var panelObj = Instantiate(_efficiencyStatusTextPairPanelPrefab, _efficiencyStatusScrollViewContentTransform);
                var panel = panelObj.GetComponent<TextPairPanel>();
                
                if (panel != null)
                {
                    string effectDescription = effectState.Data.displayName ?? effectState.Data.id;
                    string changeValue = FormatEffectValue(effectState.Data.value, effectState.Data.type);
                    
                    panel.OnInitialize(effectDescription, changeValue);
                }
            }
        }
    }
    
    /// <summary>
    /// 만족도 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateSatisfactionEffectStatus()
    {
        if (_satisfactionStatusScrollViewContentTransform == null || 
            _efficiencyStatusTextPairPanelPrefab == null ||
            _dataManager?.Effect == null)
        {
            return;
        }

        // 기존 패널 제거
        GameObjectUtils.ClearChildren(_satisfactionStatusScrollViewContentTransform);

        var combinedEffects = new List<EffectState>();

        // 1. 전역 만족도 이펙트
        var globalEffects = _dataManager.Effect.GetActiveEffects(StatType.SatisfactionChangePerDay);
        if (globalEffects != null)
        {
            combinedEffects.AddRange(globalEffects);
        }

        // 2. 직원 개별 만족도 이펙트
        if (_selectedEmployeeEntry?.state?.activeEffects != null)
        {
            var localEffects = _selectedEmployeeEntry.state.activeEffects
                .Where(e => e.Data.statType == StatType.SatisfactionChangePerDay);
            combinedEffects.AddRange(localEffects);
        }

        if (combinedEffects.Count == 0)
        {
            return;
        }

        // 각 이펙트에 대해 패널 생성
        foreach (var effectState in combinedEffects)
        {
            if (effectState?.Data == null) continue;

            var panelObj = Instantiate(_efficiencyStatusTextPairPanelPrefab, _satisfactionStatusScrollViewContentTransform);
            var panel = panelObj.GetComponent<TextPairPanel>();
            
            if (panel != null)
            {
                string effectDescription = effectState.Data.displayName ?? effectState.Data.id;
                string changeValue = FormatEffectValue(effectState.Data.value, effectState.Data.type);
                
                panel.OnInitialize(effectDescription, changeValue);
            }
        }
    }

    /// <summary>
    /// 이펙트 값을 포맷팅합니다.
    /// </summary>
    /// <param name="value">이펙트 값</param>
    /// <param name="modifierType">연산 방식</param>
    /// <returns>포맷팅된 문자열</returns>
    private string FormatEffectValue(float value, ModifierType modifierType)
    {
        switch (modifierType)
        {
            case ModifierType.Flat:
                // 합연산: +2.0 또는 -1.5 형식
                return value >= 0 ? $"+{value:F1}" : $"{value:F1}";
            
            case ModifierType.PercentAdd:
                // 합연산 퍼센트: +10% 또는 -5% 형식
                float percentAdd = value * 100f;
                return percentAdd >= 0 ? $"+{percentAdd:F1}%" : $"{percentAdd:F1}%";
            
            case ModifierType.PercentMult:
                // 곱연산 퍼센트: x1.5 또는 x0.8 형식
                return $"x{value:F2}";
            
            default:
                return value.ToString("F1");
        }
    }

    /// <summary>
    /// 이펙트 상태 표시를 초기화합니다.
    /// </summary>
    private void ClearEffectStatus()
    {
        if (_efficiencyStatusScrollViewContentTransform != null)
        {
            GameObjectUtils.ClearChildren(_efficiencyStatusScrollViewContentTransform);
        }
        
        if (_satisfactionStatusScrollViewContentTransform != null)
        {
            GameObjectUtils.ClearChildren(_satisfactionStatusScrollViewContentTransform);
        }
    }

    #endregion

    #region Event Subscription

    /// <summary>
    /// 일일 변경 이벤트 구독
    /// </summary>
    private void SubscribeToDayChange()
    {
        if (_isSubscribedToDayChange)
        {
            return;
        }

        if (_dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnDayChanged += HandleDayChanged;
        _isSubscribedToDayChange = true;
    }

    /// <summary>
    /// 일일 변경 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromDayChange()
    {
        if (!_isSubscribedToDayChange)
        {
            return;
        }

        if (_dataManager?.Time == null)
        {
            _isSubscribedToDayChange = false;
            return;
        }

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _isSubscribedToDayChange = false;
    }

    /// <summary>
    /// 직원 변경 이벤트 구독
    /// </summary>
    private void SubscribeToEmployeeChanged()
    {
        if (_isSubscribedToEmployeeChanged)
        {
            return;
        }

        if (_dataManager?.Employee == null)
        {
            return;
        }

        _dataManager.Employee.OnEmployeeChanged += HandleEmployeeChanged;
        _isSubscribedToEmployeeChanged = true;
    }

    /// <summary>
    /// 직원 변경 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromEmployeeChanged()
    {
        if (!_isSubscribedToEmployeeChanged)
        {
            return;
        }

        if (_dataManager?.Employee == null)
        {
            _isSubscribedToEmployeeChanged = false;
            return;
        }

        _dataManager.Employee.OnEmployeeChanged -= HandleEmployeeChanged;
        _isSubscribedToEmployeeChanged = false;
    }


    /// <summary>
    /// 직원 변경 핸들러
    /// </summary>
    private void HandleEmployeeChanged()
    {
        RefreshEmployeeUI();
        // 관리 비율도 업데이트 (직원 변경 시 영향받음)
        UpdateManagementRatio();
    }
    
    /// <summary>
    /// 일일 변경 핸들러
    /// </summary>
    private void HandleDayChanged()
    {
        RefreshEmployeeUI();
        // 관리 비율도 업데이트 (일일 업데이트 시 영향받음)
        UpdateManagementRatio();
    }

    #endregion

    #region Unity Lifecycle

    private void OnDisable()
    {
        if (!gameObject.activeInHierarchy)
        {
            UnsubscribeFromDayChange();
            UnsubscribeFromEmployeeChanged();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromDayChange();
        UnsubscribeFromEmployeeChanged();
    }

    #endregion
}
