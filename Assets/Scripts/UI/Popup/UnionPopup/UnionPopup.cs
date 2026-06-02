using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnionPopup : PopupBase
{
    [SerializeField] private GameObject _employeeInfoContainerPrefab;
    [SerializeField] private Transform _employeeInfoContainerContentTransform;

    [SerializeField] private Image _iconImage;

    [SerializeField] private Slider _dailyCohesionChangeDecreseSlider;
    [SerializeField] private Slider _dailyCohesionChangeIncreaseSlider;
    [SerializeField] private TextMeshProUGUI _dailyCohesionChangeText;

    [SerializeField] private Slider _unionCohesionSlider;
    [SerializeField] private TextMeshProUGUI _unionCohesionText;

    [SerializeField] private Transform _requestScrollViewContentransform;
    [SerializeField] private GameObject _requestBtnPrefab;
    [SerializeField] private TextMeshProUGUI _remainDayText;

    [Header("Complete Feedback")]
    [SerializeField] private AudioClip _requirementCompleteSfx;

    private List<UnionPopupEmployeeInfoContainer> _employeeInfoContainerList = new();
    private List<UnionPopupRequestBtn> _requestBtnList = new();
    private bool _isEmployeeEventSubscribed;
    private bool _isUnionRequestEventSubscribed;
    private bool _isResourceEventSubscribed;
    private bool _isCreditEventSubscribed;
    private bool _isPolicyEventSubscribed;
    private Coroutine _refreshCoroutine;

    public override void Init()
    {
        base.Init();

        if (_dataManager?.MainEvent?.UnionModule == null || _dataManager.MainEvent.CurrentEventType != MainEventType.Union)
        {
            return;
        }

        SubscribeUnionPopupEvents();
        RefreshUI();
        Show();
    }

    public override void Close()
    {
        StaggeredSpawnUtils.Stop(this, ref _refreshCoroutine);
        UnsubscribeUnionPopupEvents();
        base.Close();
    }

    protected override void HandleDayChanged()
    {
        if (gameObject.activeSelf)
        {
            RefreshUI();
        }
    }

    private void SubscribeUnionPopupEvents()
    {
        if (_dataManager?.Employee != null && !_isEmployeeEventSubscribed)
        {
            _dataManager.Employee.OnEmployeeChanged -= HandleUnionPopupDataChanged;
            _dataManager.Employee.OnEmployeeChanged += HandleUnionPopupDataChanged;
            _isEmployeeEventSubscribed = true;
        }

        if (_dataManager?.UnionRequest != null && !_isUnionRequestEventSubscribed)
        {
            _dataManager.UnionRequest.OnUnionRequestChanged -= HandleUnionPopupDataChanged;
            _dataManager.UnionRequest.OnUnionRequestChanged += HandleUnionPopupDataChanged;
            _isUnionRequestEventSubscribed = true;
        }

        if (_dataManager?.Resource != null && !_isResourceEventSubscribed)
        {
            _dataManager.Resource.OnResourceChanged -= HandleUnionPopupDataChanged;
            _dataManager.Resource.OnResourceChanged += HandleUnionPopupDataChanged;
            _isResourceEventSubscribed = true;
        }

        if (_dataManager?.Finances != null && !_isCreditEventSubscribed)
        {
            _dataManager.Finances.OnCreditChanged -= HandleUnionPopupDataChanged;
            _dataManager.Finances.OnCreditChanged += HandleUnionPopupDataChanged;
            _isCreditEventSubscribed = true;
        }

        if (_dataManager?.Policy != null && !_isPolicyEventSubscribed)
        {
            _dataManager.Policy.OnPolicyChanged -= HandleUnionPopupDataChanged;
            _dataManager.Policy.OnPolicyChanged += HandleUnionPopupDataChanged;
            _isPolicyEventSubscribed = true;
        }
    }

    private void UnsubscribeUnionPopupEvents()
    {
        if (_isEmployeeEventSubscribed && _dataManager?.Employee != null)
        {
            _dataManager.Employee.OnEmployeeChanged -= HandleUnionPopupDataChanged;
            _isEmployeeEventSubscribed = false;
        }

        if (_isUnionRequestEventSubscribed && _dataManager?.UnionRequest != null)
        {
            _dataManager.UnionRequest.OnUnionRequestChanged -= HandleUnionPopupDataChanged;
            _isUnionRequestEventSubscribed = false;
        }

        if (_isResourceEventSubscribed && _dataManager?.Resource != null)
        {
            _dataManager.Resource.OnResourceChanged -= HandleUnionPopupDataChanged;
            _isResourceEventSubscribed = false;
        }

        if (_isCreditEventSubscribed && _dataManager?.Finances != null)
        {
            _dataManager.Finances.OnCreditChanged -= HandleUnionPopupDataChanged;
            _isCreditEventSubscribed = false;
        }

        if (_isPolicyEventSubscribed && _dataManager?.Policy != null)
        {
            _dataManager.Policy.OnPolicyChanged -= HandleUnionPopupDataChanged;
            _isPolicyEventSubscribed = false;
        }
    }

    private void HandleUnionPopupDataChanged()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        RefreshUI();
    }

    private void HandleUnionPopupDataChanged(UnionRequestState request)
    {
        HandleUnionPopupDataChanged();
    }

    public void RefreshUI()
    {
        StaggeredSpawnUtils.Restart(this, ref _refreshCoroutine, RefreshUIRoutine());
    }

    private IEnumerator RefreshUIRoutine()
    {
        UnionStateModule module = _dataManager?.MainEvent?.UnionModule;
        if (module == null)
            yield break;

        int remaining = module.RemainingDays;
        if (remaining <= 0)
        {
            Close();
            yield break;
        }

        InitialUnionMainEventData unionData = _dataManager.InitialUnionMainEventData;
        _iconImage.sprite = unionData.announcementIcon;

        _remainDayText.text = remaining >= 0 ? remaining.ToString() : "-";

        RefreshDailyCohesionChangeUI(module);
        RefreshUnionCohesionUI(module);

        if (_employeeInfoContainerList.Count != 4)
        {
            GameObjectUtils.ClearChildren(_employeeInfoContainerContentTransform);
            _employeeInfoContainerList.Clear();

            List<EmployeeEntry> employeeEntries = new List<EmployeeEntry>(_dataManager.Employee.GetAllEmployees().Values);

            yield return StaggeredSpawnUtils.ForEachFrame(employeeEntries.Count, i =>
            {
                EmployeeEntry employeeEntry = employeeEntries[i];
                GameObject containerObj = Instantiate(_employeeInfoContainerPrefab, _employeeInfoContainerContentTransform);
                UnionPopupEmployeeInfoContainer container = containerObj.GetComponent<UnionPopupEmployeeInfoContainer>();
                container.Init(employeeEntry);
                _employeeInfoContainerList.Add(container);
            });
        }
        else
        {
            foreach (UnionPopupEmployeeInfoContainer container in _employeeInfoContainerList)
                container.RefreshUI();
        }

        RefreshUnionRequestButtons(module);
    }

    private void RefreshDailyCohesionChangeUI(UnionStateModule module)
    {
        float dailyGain = module.GetDailyCohesionGainFromWorkforceSatisfaction();
        float dailyLoss = module.GetDailyCohesionLossFromWorkforceSatisfaction();
        float maxDailyGain = module.GetMaxDailyCohesionGainFromWorkforceSatisfaction();
        float maxDailyLoss = module.GetMaxDailyCohesionLossFromWorkforceSatisfaction();

        float increaseValue = dailyGain > 0f && maxDailyGain > 0f
            ? Mathf.Clamp01(dailyGain / maxDailyGain)
            : 0f;
        float decreaseValue = dailyLoss > 0f && maxDailyLoss > 0f
            ? Mathf.Clamp01(dailyLoss / maxDailyLoss)
            : 0f;

        if (_dailyCohesionChangeIncreaseSlider != null)
        {
            _dailyCohesionChangeIncreaseSlider.minValue = 0f;
            _dailyCohesionChangeIncreaseSlider.maxValue = 1f;
            _dailyCohesionChangeIncreaseSlider.value = increaseValue;
        }

        if (_dailyCohesionChangeDecreseSlider != null)
        {
            _dailyCohesionChangeDecreseSlider.minValue = 0f;
            _dailyCohesionChangeDecreseSlider.maxValue = 1f;
            _dailyCohesionChangeDecreseSlider.value = decreaseValue;
        }

        if (_dailyCohesionChangeText != null)
        {
            if (dailyGain > 0f)
            {
                _dailyCohesionChangeText.text = $"+{dailyGain:F1}%/day";
            }
            else if (dailyLoss > 0f)
            {
                _dailyCohesionChangeText.text = $"-{dailyLoss:F1}%/day";
            }
            else
            {
                _dailyCohesionChangeText.text = "0%/day";
            }
        }
    }

    private void RefreshUnionRequestButtons(UnionStateModule module)
    {
        if (_requestBtnPrefab == null || _requestScrollViewContentransform == null)
            return;

        List<UnionRequestState> activeRequests = module.GetActiveUnionRequests();

        while (_requestBtnList.Count > activeRequests.Count)
        {
            int lastIndex = _requestBtnList.Count - 1;
            UnionPopupRequestBtn removedBtn = _requestBtnList[lastIndex];
            _requestBtnList.RemoveAt(lastIndex);
            if (removedBtn != null && removedBtn.gameObject != _requestBtnPrefab)
                Destroy(removedBtn.gameObject);
        }

        for (int i = 0; i < activeRequests.Count; i++)
        {
            UnionPopupRequestBtn requestBtn = GetOrCreateUnionRequestBtn(i);
            if (requestBtn == null)
                continue;

            requestBtn.gameObject.SetActive(true);
            requestBtn.Init(activeRequests[i], _dataManager, this, _requirementCompleteSfx);
        }

        for (int i = activeRequests.Count; i < _requestBtnList.Count; i++)
            _requestBtnList[i].gameObject.SetActive(false);
    }

    private UnionPopupRequestBtn GetOrCreateUnionRequestBtn(int index)
    {
        if (index < _requestBtnList.Count)
            return _requestBtnList[index];

        UnionPopupRequestBtn requestBtn;
        if (_requestBtnList.Count == 0)
        {
            requestBtn = _requestBtnPrefab.GetComponent<UnionPopupRequestBtn>();
        }
        else
        {
            GameObject btnObj = Instantiate(_requestBtnPrefab, _requestScrollViewContentransform);
            requestBtn = btnObj.GetComponent<UnionPopupRequestBtn>();
        }

        _requestBtnList.Add(requestBtn);
        return requestBtn;
    }

    private void RefreshUnionCohesionUI(UnionStateModule module)
    {
        float cohesionProgress = Mathf.Clamp(module.UnionCohesionProgress, 0f, 100f);

        if (_unionCohesionSlider != null)
        {
            _unionCohesionSlider.minValue = 0f;
            _unionCohesionSlider.maxValue = 100f;
            _unionCohesionSlider.value = cohesionProgress;
        }

        if (_unionCohesionText != null)
        {
            _unionCohesionText.text = $"{Mathf.RoundToInt(cohesionProgress)}%";
        }
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
