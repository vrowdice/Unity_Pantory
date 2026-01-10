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
    [SerializeField] private Transform _efficiencyStatusScrollViewContentTransform;
    [SerializeField] private Transform _satisfactionStatusScrollViewContentTransform;

    private EmployeeEntry _selectedEmployeeEntry;
    private EmployeeType _selectedEmployeeType;
    private bool _isSubscribedToDayChange;
    private bool _isSubscribedToEmployeeChanged;
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
        SelectFirstEmployee();
    }

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
        if (entry == null || entry.data == null)
        {
            return;
        }

        _selectedEmployeeEntry = entry;
        _selectedEmployeeType = entry.data.type;
        UpdateEmployeeUI();
    }

    #endregion

    #region Role Button Setup

    /// <summary>
    /// 직원 역할별 버튼 생성
    /// </summary>
    private void SetupEmployeeRoleButtons()
    {
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
                btn.Init(roleName, () => ShowEmployeeByRole(role));
            }
        }
    }

    /// <summary>
    /// 특정 역할의 첫 번째 직원 표시
    /// </summary>
    private void ShowEmployeeByRole(EmployeeType role)
    {
        var allEmployees = _dataManager.Employee.GetAllEmployees();
        if (allEmployees == null || allEmployees.Count == 0)
        {
            return;
        }

        // 해당 역할의 첫 번째 직원 찾기
        EmployeeEntry foundEntry = null;
        foreach (EmployeeEntry entry in allEmployees.Values)
        {
            if (entry?.data != null && entry.data.type == role)
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

        foreach (var entry in allEmployees.Values)
        {
            if (entry != null && entry.data != null)
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
        var data = _selectedEmployeeEntry.data;
        var state = _selectedEmployeeEntry.state;

        _employeeImage.sprite = data.Image;
        _employeeIconImage.sprite = data.icon;
        _titleText.text = data.displayName ?? data.id;
        _descriptionText.text = data.description ?? string.Empty;
        _employeeCountText.text = $"Count: {state.count}";
        _totalSalatyText.text = $"Total Salary: {state.totalSalary:N0}";

        float normalizedEfficiency = Mathf.Clamp(state.currentEfficiency, 0f, 2f) / 2f;
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

        // GetManagementRatio()는 0.0 ~ 1.0 범위의 값을 반환
        float managementRatio = _dataManager.Employee.GetManagementRatio();
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
                _managementRatioText.text = "100%";
            }
            else
            {
                int shortage = requiredManagers - currentManagers;
                _managementRatioText.text = $"{managementRatio:P0} (-{shortage})";
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
                    _managementFillImage.color = VisualManager.Instance.ManagementSufficientColor;
                }
                else
                {
                    _managementFillImage.color = VisualManager.Instance.ManagementInsufficientColor;
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
        ClearEffectStatus();
        UpdateEfficiencyEffectStatus();
        UpdateSatisfactionEffectStatus();
    }

    /// <summary>
    /// 효율성 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateEfficiencyEffectStatus()
    {
        GameObjectUtils.ClearChildren(_efficiencyStatusScrollViewContentTransform);

        List<EffectState> combinedEffects = new List<EffectState>();

        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Efficiency_Flat));
        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Efficiency_Mult));

        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(_selectedEmployeeEntry.data.type, EffectStatType.Employee_Efficiency_Flat));
        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(_selectedEmployeeEntry.data.type, EffectStatType.Employee_Efficiency_Mult));

        if (combinedEffects.Count > 0)
        {
            foreach (EffectState effectState in combinedEffects)
            {
                if (effectState == null) continue;

                GameObject panelObj = Instantiate(_gameManager.EffectTextPairPanelPrefab, _efficiencyStatusScrollViewContentTransform);
                TextPairPanel panel = panelObj.GetComponent<TextPairPanel>();

                if (panel != null)
                {
                    string effectDescription = effectState.displayName ?? effectState.id;
                    string changeValue = _dataManager.Effect.FormatEffectValue(effectState.value, effectState.type);
                    panel.Init(effectDescription, changeValue, effectState.value);
                }
            }
        }
    }

    /// <summary>
    /// 만족도 관련 이펙트 상태를 업데이트합니다.
    /// </summary>
    private void UpdateSatisfactionEffectStatus()
    {
        GameObjectUtils.ClearChildren(_satisfactionStatusScrollViewContentTransform);

        List<EffectState> combinedEffects = new List<EffectState>();
        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(EffectTargetType.Employee, EffectStatType.Employee_Satisfaction_Per));
        combinedEffects.AddRange(_dataManager.Effect.GetEffectStatEffects(_selectedEmployeeEntry.data.type, EffectStatType.Employee_Satisfaction_Per));

        foreach (EffectState effectState in combinedEffects)
        {
            if (effectState == null) continue;

            GameObject panelObj = Instantiate(_gameManager.EffectTextPairPanelPrefab, _satisfactionStatusScrollViewContentTransform);
            TextPairPanel panel = panelObj.GetComponent<TextPairPanel>();

            if (panel != null)
            {
                string effectDescription = effectState.displayName ?? effectState.id;
                string changeValue = _dataManager.Effect.FormatEffectValue(effectState.value, effectState.type);
                panel.Init(effectDescription, changeValue, effectState.value);
            }
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