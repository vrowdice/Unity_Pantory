using UnityEngine;

public class CreditTopInfoPopup : PopupBase
{
    [SerializeField] private GameObject _titleDeltaTextPanelPrefab;
    [SerializeField] private PanelDoAni _panelDoAni;
    [SerializeField] private RectTransform _popupArea;

    private DataManager _dataManager;
    private System.Action _cachedHideAction;
    private bool _isEscRegistered;
    private bool _isCreditInfoVisible;
    private RectTransform _toggleButtonRect;

    public override void Init()
    {
        base.Init();
        _dataManager = DataManager.Instance;
        _cachedHideAction ??= HideCreditInfo;

        _isCreditInfoVisible = false;
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

    public void SetToggleButtonRect(RectTransform toggleButtonRect)
    {
        _toggleButtonRect = toggleButtonRect;
    }

    public void ToggleCreditInfo()
    {
        if (_isCreditInfoVisible)
        {
            HideCreditInfo();
        }
        else
        {
            ShowCreditInfo();
        }
    }

    public void ShowCreditInfo()
    {
        _isCreditInfoVisible = true;
        RegisterEscClose();
        UpdateCreditInfo();
        gameObject.SetActive(true);
        _panelDoAni?.OpenPanel();
    }

    public void HideCreditInfo()
    {
        if (!_isCreditInfoVisible && !gameObject.activeSelf)
        {
            return;
        }

        _isCreditInfoVisible = false;
        UnregisterEscClose();

        if (_panelDoAni != null)
        {
            _panelDoAni.ClosePanel(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void RegisterEscClose()
    {
        if (_isEscRegistered || UIManager.Instance == null || _cachedHideAction == null) return;
        UIManager.Instance.PushCloseable(_cachedHideAction);
        _isEscRegistered = true;
    }

    private void UnregisterEscClose()
    {
        if (!_isEscRegistered || UIManager.Instance == null || _cachedHideAction == null) return;
        UIManager.Instance.RemoveCloseable(_cachedHideAction);
        _isEscRegistered = false;
    }

    private void UpdateCreditInfo()
    {
        GameObjectUtils.ClearChildren(transform);

        CreatePanelIfValid(LocalizationUtils.Localize("Employee Salary", "Expenses"), -_dataManager.Finances.DailySalary);
        CreatePanelIfValid(LocalizationUtils.Localize("Resource Changes", "Expenses"), _dataManager.Finances.DailyResource);
        CreatePanelIfValid(LocalizationUtils.Localize("Thread Maintenance Cost", "Expenses"), -_dataManager.Finances.DailyMaintenance);
        CreatePanelIfValid(LocalizationUtils.Localize("Policy Cost", "Expenses"), -_dataManager.Finances.DailyPolicyCost);
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
        if (_isCreditInfoVisible && gameObject.activeInHierarchy)
        {
            UpdateCreditInfo();
        }
    }

    private void Update()
    {
        if (!_isCreditInfoVisible || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (IsPointerOverToggleButton())
        {
            return;
        }

        RectTransform rect = _popupArea != null ? _popupArea : transform as RectTransform;
        if (rect != null && !RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, GetUiCamera(rect)))
        {
            HideCreditInfo();
        }
    }

    private bool IsPointerOverToggleButton()
    {
        if (_toggleButtonRect == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            _toggleButtonRect,
            Input.mousePosition,
            GetUiCamera(_toggleButtonRect));
    }

    private static Camera GetUiCamera(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return null;
        }

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void OnDestroy()
    {
        UnregisterEscClose();

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
