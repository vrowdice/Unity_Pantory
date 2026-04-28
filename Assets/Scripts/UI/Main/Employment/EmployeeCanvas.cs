using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 직원 관리 패널
/// </summary>
public class EmployeeCanvas : MainCanvasPanelBase
{
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
    [SerializeField] private TextMeshProUGUI _whenHireText;
    [SerializeField] private TextMeshProUGUI _whenFireText;
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
    [SerializeField] private Transform _efficiencyStatusScrollViewContentTransform;
    [SerializeField] private Transform _satisfactionStatusScrollViewContentTransform;

    private List<ActionBtn> _roleButtons = new List<ActionBtn>();
    private EmployeeType _selectedEmployeeType = EmployeeType.Worker;
    private bool _isUpdatingToggles;

    /// <summary>
    /// (BasePanel)
    /// </summary>
    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;

        _dataManager.Employee.OnEmployeeChanged -= HandleEmployeeChanged;
        _dataManager.Employee.OnEmployeeChanged += HandleEmployeeChanged;

        SetupEmployeeRoleButtons();
        OnEmployeeTypeClick(EmployeeType.Worker);
    }

    private void OnDisable()
    {
        if (_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= HandleDayChanged;
            _dataManager.Employee.OnEmployeeChanged -= HandleEmployeeChanged;
        }
    }

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
            _dataManager.Employee.HireEmployee(_selectedEmployeeType, delta);
        }
        else
        {
            int fireCount = Mathf.Abs(delta);
            _dataManager.Employee.TryFireEmployee(_selectedEmployeeType, fireCount);
        }
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
    /// 직원 타입 선택 핸들러
    /// </summary>
    public void OnEmployeeTypeClick(EmployeeType employeeType)
    {
        _selectedEmployeeType = employeeType;
        UpdateEmployeeUI();
        UpdateRoleButtonHighlight();
    }

    /// <summary>
    /// 직원 역할별 버튼 생성
    /// </summary>
    private void SetupEmployeeRoleButtons()
    {
        if (_panelUIManager?.ActionBtnPrefab == null || EmployeeActionBtnContent == null)
        {
            return;
        }

        int targetCount = EnumUtils.GetAllEnumValues<EmployeeType>().Count;
        if (EmployeeActionBtnContent.childCount == targetCount)
        {
            _roleButtons.Clear();
            foreach (Transform child in EmployeeActionBtnContent)
            {
                ActionBtn btn = child.GetComponent<ActionBtn>();
                if (btn != null)
                {
                    _roleButtons.Add(btn);
                }
            }
            UpdateRoleButtonHighlight();
            return;
        }

        _gameManager.PoolingManager.ClearChildrenToPool(EmployeeActionBtnContent);
        _roleButtons.Clear();

        List<EmployeeType> roles = EnumUtils.GetAllEnumValues<EmployeeType>();
        foreach (EmployeeType role in roles)
        {
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_panelUIManager.ActionBtnPrefab);
            btnObj.transform.SetParent(EmployeeActionBtnContent, false);
            ActionBtn btn = btnObj.GetComponent<ActionBtn>();
            if (btn != null)
            {
                EmployeeType capturedRole = role;
                string localizedName = capturedRole.Localize(LocalizationUtils.TABLE_EMPLOYEE);
                btn.Init(localizedName, () => {
                    ShowEmployeeByRole(capturedRole);
                    UpdateRoleButtonHighlight();
                });
                _roleButtons.Add(btn);
            }
        }
        
        UpdateRoleButtonHighlight();
    }

    /// <summary>
    /// 역할 버튼 하이라이트 업데이트
    /// </summary>
    private void UpdateRoleButtonHighlight()
    {
        if (_roleButtons.Count == 0) return;

        List<EmployeeType> roles = EnumUtils.GetAllEnumValues<EmployeeType>();
        for (int i = 0; i < roles.Count && i < _roleButtons.Count; i++)
        {
            _roleButtons[i].SetHighlight(_selectedEmployeeType == roles[i]);
        }
    }

    /// <summary>
    /// 특정 역할의 직원 표시
    /// </summary>
    private void ShowEmployeeByRole(EmployeeType role)
    {
        EmployeeEntry entry = _dataManager.Employee.GetEmployeeEntry(role);
        if (entry == null)
        {
            Debug.LogWarning($"[EmployeePanel] No employee found for role: {role}");
            return;
        }

        OnEmployeeTypeClick(role);
    }

    /// <summary>
    /// 직원 UI 정보 업데이트
    /// </summary>
    private void UpdateEmployeeUI()
    {
        EmployeeEntry entry = _dataManager.Employee.GetEmployeeEntry(_selectedEmployeeType);
        if (entry == null) return;

        EmployeeData data = entry.data;
        EmployeeState state = entry.state;

        _employeeImage.sprite = data.Image;
        _employeeIconImage.sprite = data.icon;
        _titleText.text = data.type.Localize(LocalizationUtils.TABLE_EMPLOYEE);
        _descriptionText.text = (data.type.ToString() + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_EMPLOYEE);
        _employeeCountText.text = $"{state.count}";
        _whenHireText.text = $"- {data.hiringCost:N0}";
        _whenFireText.text = $"- {data.firingCost:N0}";
        _totalSalatyText.text = $"{state.totalSalary:N0}";

        float normalizedEfficiency = Mathf.Clamp01(state.currentEfficiency);
        _efficiencySlider.value = normalizedEfficiency;
        _efficiencyValueText.text = $"{(state.currentEfficiency * 100f):F1}%";

        float normalizedSatisfaction = (state.currentSatisfaction + 100f) / 200f;
        _satisfactionSlider.value = Mathf.Clamp01(normalizedSatisfaction);
        _satisfactionValueText.text = $"{state.currentSatisfaction:F1}";

        UpdateSalaryLevelToggles(state.salaryLevel);
        UpdateManagementRatio();
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

        float managementRatio = _dataManager.Employee.GetManagementRatio();
        if (_managementSlider != null)
        {
            _managementSlider.value = managementRatio;
        }

        if (_managementRatioText != null)
        {
            _dataManager.Employee.GetManagementInfo(out int currentManagers, out int requiredManagers);

            if (requiredManagers <= 0 || currentManagers >= requiredManagers)
            {
                _managementRatioText.text = "100%";
            }
            else
            {
                int shortage = requiredManagers - currentManagers;
                _managementRatioText.text = $"{managementRatio:P0} (-{shortage})";
            }
        }

        if (_managementFillImage != null)
        {
            _dataManager.Employee.GetManagementInfo(out int currentManagers, out int requiredManagers);

            if (_visualManager != null)
            {
                if (requiredManagers <= 0 || currentManagers >= requiredManagers)
                {
                    _managementFillImage.color = _visualManager.ManagementSufficientColor;
                }
                else
                {
                    _managementFillImage.color = _visualManager.ManagementInsufficientColor;
                }
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
        salaryLevel = Mathf.Clamp(salaryLevel, 0, 4);
        _isUpdatingToggles = true;

        try
        {
            for (int i = 0; i < _salaryLevelToggles.Count && i <= 4; i++)
            {
                if (_salaryLevelToggles[i] != null)
                {
                    if (_salaryLevelToggles[i].isOn != (i == salaryLevel))
                    {
                        _salaryLevelToggles[i].isOn = (i == salaryLevel);
                    }
                }
            }
        }
        finally
        {
            _isUpdatingToggles = false;
        }
    }

    /// <summary>
    /// 직원 UI 갱신
    /// </summary>
    private void RefreshEmployeeUI()
    {
        UpdateEmployeeUI();
    }

    /// <summary>
    /// 효율성 및 만족도 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateEffectStatus()
    {
        ClearEffectStatus();
        UpdateEfficiencyEffectStatus();
        UpdateSatisfactionEffectStatus();
    }

    /// <summary>
    /// 효율성 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateEfficiencyEffectStatus()
    {
        _gameManager.PoolingManager.ClearChildrenToPool(_efficiencyStatusScrollViewContentTransform);

        List<EffectState> combinedEffects = new List<EffectState>();

        string instanceId = _selectedEmployeeType.ToString();
        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Efficiency, instanceId));

        if (combinedEffects.Count > 0)
        {
            foreach (EffectState effectState in combinedEffects)
            {
                if (effectState == null) continue;

                _panelUIManager.CreateEffectTextPairPanel(_efficiencyStatusScrollViewContentTransform, effectState);
            }
        }
    }

    /// <summary>
    /// 만족도 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateSatisfactionEffectStatus()
    {
        _gameManager.PoolingManager.ClearChildrenToPool(_satisfactionStatusScrollViewContentTransform);

        List<EffectState> combinedEffects = new List<EffectState>();
        string instanceId = _selectedEmployeeType.ToString();
        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Satisfaction, instanceId));

        foreach (EffectState effectState in combinedEffects)
        {
            if (effectState == null) continue;

            _panelUIManager.CreateEffectTextPairPanel(_satisfactionStatusScrollViewContentTransform, effectState);
        }
    }

    /// <summary>
    /// 이펙트 상태 표시를 초기화합니다.
    /// </summary>
    private void ClearEffectStatus()
    {
        if (_efficiencyStatusScrollViewContentTransform != null)
        {
            _gameManager.PoolingManager.ClearChildrenToPool(_efficiencyStatusScrollViewContentTransform);
        }

        if (_satisfactionStatusScrollViewContentTransform != null)
        {
            _gameManager.PoolingManager.ClearChildrenToPool(_satisfactionStatusScrollViewContentTransform);
        }
    }

    /// <summary>
    /// 직원 변경 핸들러
    /// </summary>
    private void HandleEmployeeChanged()
    {
        RefreshEmployeeUI();
        UpdateManagementRatio();
    }

    /// <summary>
    /// 일일 변경 핸들러
    /// </summary>
    private void HandleDayChanged()
    {
        RefreshEmployeeUI();
        UpdateManagementRatio();
    }
}