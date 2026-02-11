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
        bool isOpen = _panelDoAni != null ? _panelDoAni.IsOpen : gameObject.activeSelf;

        if (isOpen) HideCreditInfo();
        else ShowCreditInfo();
    }

    public void ShowCreditInfo()
    {
        UpdateCreditInfo();
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

        CreatePanelIfValid(LocalizationUtils.Localize("Employee Salary", "Expenses"), -_dataManager.Finances.DailySalary);
        CreatePanelIfValid(LocalizationUtils.Localize("Resource Changes", "Expenses"), -_dataManager.Finances.DailyResource);
        CreatePanelIfValid(LocalizationUtils.Localize("Thread Maintenance Cost", "Expenses"), -_dataManager.Finances.DailyMaintenance);
        CreatePanelIfValid(LocalizationUtils.Localize("Negative Interest", "Expenses"), -_dataManager.Finances.DailyInterest);
        
        CreatePanelIfValid(LocalizationUtils.Localize("Daily Change", "Expenses"), _dataManager.Finances.DailyTotal, true);
    }

    private void CreatePanelIfValid(string title, long value, bool forceShow = false)
    {
        if (value == 0 && !forceShow) return;
        if (_titleDeltaTextPanelPrefab == null) return;

        GameObject panelObj = Instantiate(_titleDeltaTextPanelPrefab, transform);
        panelObj.GetComponent<TitleDeltaTextPanel>().Init(title, value);
    }

    private void HandleDayChanged()
    {
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