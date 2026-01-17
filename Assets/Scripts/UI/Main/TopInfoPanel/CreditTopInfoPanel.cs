using UnityEngine;

public class CreditTopInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _titleDeltaTextPanelPrefab;
    [SerializeField] private PanelDoAni _panelDoAni;

    private DataManager _dataManager;

    public void Init(DataManager dataManager)
    {
        _dataManager = dataManager;

        _panelDoAni?.SnapToClosedPosition();
        gameObject.SetActive(false);

        if (_dataManager?.Time != null)
        {
            _dataManager.Time.OnDayChanged -= HandleDayChanged;
            _dataManager.Time.OnDayChanged += HandleDayChanged;

            _dataManager.Finances.OnCreditChanged -= HandleDayChanged;
            _dataManager.Finances.OnCreditChanged += HandleDayChanged;
        }
    }

    public void ToggleCreditInfo()
    {
        // 애니메이션 컴포넌트 유무에 따라 현재 상태 판단
        bool isOpen = _panelDoAni != null ? _panelDoAni.IsOpen : gameObject.activeSelf;

        if (isOpen) HideCreditInfo();
        else ShowCreditInfo();
    }

    public void ShowCreditInfo()
    {
        if (_dataManager == null) return;

        UpdateCreditInfo(); // 데이터 갱신
        gameObject.SetActive(true);
        _panelDoAni?.OpenPanel();
    }

    public void HideCreditInfo()
    {
        if (_panelDoAni != null)
        {
            _panelDoAni.ClosePanel(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdateCreditInfo()
    {
        GameObjectUtils.ClearChildren(transform);

        long salary = -_dataManager.Employee.CalculateTotalSalary();
        long resDelta = -_dataManager.Resource.CalculateResourceDeltaChangeCredit();
        long maint = -_dataManager.ThreadPlacement.CalculateTotalMaintenanceCostOfAllPlaced();
        long interest = -_dataManager.Finances.CalculateNegativeInterest();
        long dailyNet = _dataManager.Finances.CalculateDailyCreditDelta();

        CreatePanelIfValid("Employee Salary", salary);
        CreatePanelIfValid("Resource Delta", resDelta);
        CreatePanelIfValid("Maintenance Cost", maint);
        CreatePanelIfValid("Negative Interest", interest);

        CreatePanelIfValid("Daily Net Change", dailyNet, true);
    }

    private void CreatePanelIfValid(string title, long value, bool forceShow = false)
    {
        if (value == 0 && !forceShow) return;
        if (_titleDeltaTextPanelPrefab == null) return;

        var panelObj = Instantiate(_titleDeltaTextPanelPrefab, transform);
        if (panelObj.TryGetComponent(out TitleDeltaTextPanel panel))
        {
            panel.Init(title, value);
        }
    }

    private void HandleDayChanged()
    {
        // 패널이 활성화된 상태라면 정보 갱신
        if (gameObject.activeInHierarchy)
        {
            UpdateCreditInfo();
        }
    }

    private void OnDestroy()
    {
        if (_dataManager?.Time != null)
        {
            _dataManager.Time.OnDayChanged -= HandleDayChanged;
        }
        if (_dataManager?.Finances != null)
        {
            _dataManager.Finances.OnCreditChanged -= HandleDayChanged;
        }
    }
}