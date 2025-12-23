using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DateTopInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _dateText;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private Slider _dayProgressSlider;

    [Header("Button Sprites")]
    [SerializeField] private Sprite _pauseImage;
    [SerializeField] private Sprite _playImage;

    [Header("Button Images")]
    [SerializeField] private Image _playPauseButtonImage;

    private DataManager _dataManager;
    private bool _isTimePaused = false;

    public void OnInitialize(DataManager dataManager)
    {
        _dataManager = dataManager;

        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null.");
            return;
        }

        // 시간 이벤트 구독
        _dataManager.Time.OnDayChanged += UpdateDateText;
        _dataManager.Time.OnMonthChanged += OnMonthChanged;
        _dataManager.Time.OnYearChanged += OnYearChanged;
        _dataManager.Time.OnHourChanged += OnHourChanged;

        // 초기 UI 업데이트
        UpdateDateText();
        UpdatePlayPauseButtonImage();
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
    /// Play/Pause 버튼 이미지를 업데이트합니다.
    /// </summary>
    private void UpdatePlayPauseButtonImage()
    {
        if (_playPauseButtonImage == null)
        {
            Debug.LogWarning("[InfoDatePanel] Play/Pause button image is null.");
            return;
        }

        // 현재 시간 상태에 따라 이미지 변경
        if (_isTimePaused)
        {
            _playPauseButtonImage.sprite = _playImage;
        }
        else
        {
            _playPauseButtonImage.sprite = _pauseImage;
        }
    }

    /// <summary>
    /// Play/Pause 버튼을 토글합니다. (UI 버튼에서 호출)
    /// </summary>
    public void OnPlayPauseToggleButton()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null. Cannot toggle time.");
            return;
        }

        _isTimePaused = !_isTimePaused;

        if (_isTimePaused)
        {
            _dataManager.Time.PauseTime();
        }
        else
        {
            _dataManager.Time.ResumeTime();
        }

        UpdatePlayPauseButtonImage();
    }

    /// <summary>
    /// 시간을 2배속으로 재생합니다. (UI 버튼에서 호출)
    /// </summary>
    public void OnTimeControlSpeed2xButton()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null. Cannot set time speed.");
            return;
        }

        _isTimePaused = false;
        _dataManager.Time.SetTimeSpeed(2.0f);
        UpdatePlayPauseButtonImage();
    }

    /// <summary>
    /// 시간을 4배속으로 재생합니다. (UI 버튼에서 호출)
    /// </summary>
    public void OnTimeControlSpeed4xButton()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null. Cannot set time speed.");
            return;
        }

        _isTimePaused = false;
        _dataManager.Time.SetTimeSpeed(4.0f);
        UpdatePlayPauseButtonImage();
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
        // 이벤트 구독 해제
        if (_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= UpdateDateText;
            _dataManager.Time.OnMonthChanged -= OnMonthChanged;
            _dataManager.Time.OnYearChanged -= OnYearChanged;
            _dataManager.Time.OnHourChanged -= OnHourChanged;
        }
    }
}
