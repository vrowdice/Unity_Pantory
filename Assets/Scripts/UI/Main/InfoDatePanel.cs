using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoDatePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _dateText;

    [Header("Button Sprites")]
    [SerializeField] private Sprite _pauseImage;
    [SerializeField] private Sprite _playImage;

    [Header("Button Images")]
    [SerializeField] private Image _playPauseButtonImage;

    private GameDataManager _dataManager;
    private bool _isTimePaused = false;

    public void OnInitialize(GameDataManager argDataManager)
    {
        _dataManager = argDataManager;

        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null.");
            return;
        }

        // 시간 이벤트 구독
        _dataManager.Time.OnDayChanged += UpdateDateText;
        _dataManager.Time.OnMonthChanged += OnMonthChanged;
        _dataManager.Time.OnYearChanged += OnYearChanged;

        // 초기 UI 업데이트
        UpdateDateText();
        UpdatePlayPauseButtonImage();
    }

    // ----------------- UI 업데이트 -----------------

    /// <summary>
    /// 날짜 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateDateText()
    {
        if (_dateText == null)
        {
            Debug.LogWarning("[InfoDatePanel] Date text component is null.");
            return;
        }

        if (_dataManager == null)
        {
            Debug.LogWarning("[InfoDatePanel] DataManager is null. Cannot update date text.");
            return;
        }

        // TimeService에서 날짜 정보 가져오기
        int year = _dataManager.Time.Year;
        int month = _dataManager.Time.Month;
        int day = _dataManager.Time.Day;

        // 날짜 텍스트 업데이트
        _dateText.text = $"Y{year} M{month} D{day}";
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
            // 일시정지 상태면 재생(▶) 아이콘 표시
            _playPauseButtonImage.sprite = _playImage;
        }
        else
        {
            // 재생 상태면 일시정지(⏸) 아이콘 표시
            _playPauseButtonImage.sprite = _pauseImage;
        }
    }

    // ----------------- 시간 제어 함수 (버튼용) -----------------

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

        // 현재 상태를 토글
        _isTimePaused = !_isTimePaused;

        if (_isTimePaused)
        {
            // 일시정지
            _dataManager.PauseTime();
            Debug.Log("[InfoDatePanel] Time paused by button.");
        }
        else
        {
            // 재생 (1배속)
            _dataManager.ResumeTime();
            Debug.Log("[InfoDatePanel] Time resumed at 1x speed by button.");
        }

        // 버튼 이미지 업데이트
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

        // 2배속으로 설정하면 자동으로 일시정지 해제
        _isTimePaused = false;
        _dataManager.SetTimeSpeed(2.0f);
        
        // 버튼 이미지 업데이트
        UpdatePlayPauseButtonImage();
        
        Debug.Log("[InfoDatePanel] Time speed set to 2x by button.");
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

        // 4배속으로 설정하면 자동으로 일시정지 해제
        _isTimePaused = false;
        _dataManager.SetTimeSpeed(4.0f);
        
        // 버튼 이미지 업데이트
        UpdatePlayPauseButtonImage();
        
        Debug.Log("[InfoDatePanel] Time speed set to 4x by button.");
    }

    // ----------------- 이벤트 핸들러 -----------------

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

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= UpdateDateText;
            _dataManager.Time.OnMonthChanged -= OnMonthChanged;
            _dataManager.Time.OnYearChanged -= OnYearChanged;
        }
    }
}
