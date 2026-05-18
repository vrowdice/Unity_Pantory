using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

public class UnionPopup : PopupBase
{
    [SerializeField] private GameObject _employeeInfoContainerPrefab;

    [SerializeField] private Image _iconImage;
    [SerializeField] private Slider _cohesionSlider;
    [SerializeField] private TextMeshProUGUI _cohesionValueText;
    [SerializeField] private Transform _effectScrollViewContextTransform;
    [SerializeField] private TextMeshProUGUI _remainDayText;


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

    public void RefreshUI()
    {
        UnionStateModule module = _dataManager?.MainEvent?.UnionModule;
        if (module == null)
        {
            return;
        }

        InitialUnionMainEventData unionData = _dataManager.InitialUnionMainEventData;
        _iconImage.sprite = unionData.announcementIcon;

        int remaining = module.RemainingDays;
        _remainDayText.text = remaining >= 0 ? remaining.ToString() : "-";
        _cohesionProgressText.text = $"{Mathf.RoundToInt(module.UnionCohesionProgress)}%";

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
