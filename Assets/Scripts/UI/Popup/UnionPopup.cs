using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnionPopup : PopupBase
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Transform _effectScrollViewContextTransform;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _moodText;

    private DataManager _dataManager;

    public void Init()
    {
        base.Init();
        _dataManager = DataManager.Instance;
        RefreshUI();
        Show();
    }

    public void RefreshUI()
    {
        InitialUnionMainEventData unionData = _dataManager?.InitialUnionMainEventData;
        if (unionData != null)
        {
            string table = LocalizationUtils.TABLE_MAIN_EVENT;
            string key = unionData.announcementLocalizationKey;
            if (_titleText != null)
            {
                _titleText.text = key.Localize(table);
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = (key + LocalizationUtils.KEY_SUFFIX_DESC).Localize(table);
            }

            if (_iconImage != null && unionData.announcementIcon != null)
            {
                _iconImage.sprite = unionData.announcementIcon;
            }
        }

        if (_moodText != null)
        {
            _moodText.text = $"{GetUnionMoodPercent()}%";
        }

        if (_effectScrollViewContextTransform != null && PoolingManager.Instance != null)
        {
            PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
        }
    }

    public void OnClickCloseBtn()
    {
        Close();
    }

    private int GetUnionMoodPercent()
    {
        if (_dataManager?.Employee == null)
        {
            return 0;
        }

        int totalCount = 0;
        float weightedSatisfactionSum = 0f;
        foreach (EmployeeEntry entry in _dataManager.Employee.GetAllEmployees().Values)
        {
            if (entry?.data == null || entry.state.count <= 0)
            {
                continue;
            }

            totalCount += entry.state.count;
            weightedSatisfactionSum += entry.state.currentSatisfaction * entry.state.count;
        }

        if (totalCount <= 0)
        {
            return 0;
        }

        return Mathf.RoundToInt(Mathf.Clamp(weightedSatisfactionSum / totalCount, 0f, 100f));
    }
}
