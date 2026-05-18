using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnionPopup : PopupBase
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Transform _effectScrollViewContextTransform;
    [SerializeField] private TextMeshProUGUI _remainDayText;
    [SerializeField] private TextMeshProUGUI _moodText;

    private DataManager _dataManager;

    public override void Init()
    {
        base.Init();
        _dataManager = DataManager.Instance;

        if (_dataManager?.MainEvent?.UnionModule == null
            || _dataManager.MainEvent.CurrentEventType != MainEventType.Union)
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
        if (_iconImage != null && unionData != null && unionData.announcementIcon != null)
        {
            _iconImage.sprite = unionData.announcementIcon;
        }

        if (_remainDayText != null)
        {
            int remaining = module.RemainingDays;
            _remainDayText.text = remaining >= 0 ? remaining.ToString() : "-";
        }

        if (_moodText != null)
        {
            _moodText.text = $"{Mathf.RoundToInt(module.UnionMood)}%";
        }

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
