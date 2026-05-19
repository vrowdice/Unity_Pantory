using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnionPopup : PopupBase
{
    [SerializeField] private GameObject _employeeInfoContainerPrefab;
    [SerializeField] private Transform _employeeInfoContainerContentTransform;

    [SerializeField] private Image _iconImage;
    [SerializeField] private Slider _cohesionSlider;
    [SerializeField] private TextMeshProUGUI _cohesionProgressText;
    [SerializeField] private Transform _effectScrollViewContextTransform;
    [SerializeField] private TextMeshProUGUI _remainDayText;

    private List<UnionPopupEmployeeInfoContainer> _employeeInfoContainerList = new();


    public override void Init()
    {
        base.Init();

        if (_dataManager?.MainEvent?.UnionModule == null || _dataManager.MainEvent.CurrentEventType != MainEventType.Union)
        {
            return;
        }

        RefreshUI();
        Show();
    }

    protected override void HandleDayChanged()
    {
        if (gameObject.activeSelf)
        {
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        UnionStateModule module = _dataManager?.MainEvent?.UnionModule;
        if (module == null) return;

        int remaining = module.RemainingDays;
        if(remaining <= 0)
        {
            Close();
        }

        InitialUnionMainEventData unionData = _dataManager.InitialUnionMainEventData;
        _iconImage.sprite = unionData.announcementIcon;

        _remainDayText.text = remaining >= 0 ? remaining.ToString() : "-";

        float cohesionProgress = Mathf.Clamp(module.UnionCohesionProgress, 0f, 100f);
        if (_cohesionSlider != null)
        {
            _cohesionSlider.minValue = 0f;
            _cohesionSlider.maxValue = 100f;
            _cohesionSlider.value = cohesionProgress;
        }

        _cohesionProgressText.text = $"{Mathf.RoundToInt(cohesionProgress)}%";

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);

        if(_employeeInfoContainerList.Count != 4)
        {
            GameObjectUtils.ClearChildren(_employeeInfoContainerContentTransform);
            _employeeInfoContainerList.Clear();

            Dictionary<EmployeeType, EmployeeEntry> employeeEntries = _dataManager.Employee.GetAllEmployees();
            foreach (EmployeeEntry employeeEntry in employeeEntries.Values)
            {
                GameObject _containerObj = Instantiate(_employeeInfoContainerPrefab, _employeeInfoContainerContentTransform);
                UnionPopupEmployeeInfoContainer _container = _containerObj.GetComponent<UnionPopupEmployeeInfoContainer>();
                _container.Init(employeeEntry);
                _employeeInfoContainerList.Add(_container);
            }
        }
        else
        {
            foreach (UnionPopupEmployeeInfoContainer container in _employeeInfoContainerList)
            {
                container.RefreshUI();
            }
        }
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
