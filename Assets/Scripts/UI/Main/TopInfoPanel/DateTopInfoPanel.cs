using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DateTopInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _dateText;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private Slider _dayProgressSlider;

    private DataManager _dataManager;

    public void Init(DataManager dataManager)
    {
        _dataManager = dataManager;

        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null.");
            return;
        }

        _dataManager.Time.OnDayChanged += UpdateDateText;
        _dataManager.Time.OnMonthChanged += OnMonthChanged;
        _dataManager.Time.OnYearChanged += OnYearChanged;
        _dataManager.Time.OnHourChanged += OnHourChanged;

        UpdateDateText();
        UpdateDayProgressSlider();
    }

    /// <summary>
    /// 날짜/시간 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateDateText()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null. Cannot update date text.");
            return;
        }

        int year = _dataManager.Time.Year;
        int month = _dataManager.Time.Month;
        int day = _dataManager.Time.Day;

        _dateText.text = $"Y{year} M{month} D{day}";
        _timeText.text = _dataManager.Time.CurrentHour.ToString("D2") + ":00";

        UpdateDayProgressSlider();
    }

    /// <summary>
    /// 한 달이 지났을 때 호출됩니다.
    /// </summary>
    private void OnMonthChanged()
    {
        Debug.Log("[InfoDatePanel] Month changed event received.");
        UpdateDateText();
    }

    /// <summary>
    /// 한 해가 지났을 때 호출됩니다.
    /// </summary>
    private void OnYearChanged()
    {
        Debug.Log("[InfoDatePanel] Year changed event received.");
        UpdateDateText();
    }

    private void OnHourChanged()
    {
        UpdateDateText();
    }

    /// <summary>
    /// 하루 24시간 진행도를 슬라이더로 표현 (0.0 ~ 1.0)
    /// </summary>
    private void UpdateDayProgressSlider()
    {
        if (_dayProgressSlider == null)
        {
            return;
        }

        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null. Cannot update day progress slider.");
            return;
        }

        float progress = _dataManager.Time.DayProgress;
        _dayProgressSlider.normalizedValue = Mathf.Clamp01(progress);
    }

    void OnDestroy()
    {
        if (_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= UpdateDateText;
            _dataManager.Time.OnMonthChanged -= OnMonthChanged;
            _dataManager.Time.OnYearChanged -= OnYearChanged;
            _dataManager.Time.OnHourChanged -= OnHourChanged;
        }
    }
}
