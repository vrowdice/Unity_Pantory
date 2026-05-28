using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimePlayPanel : MonoBehaviour
{
    public event Action OnTimePlayStarted;

    public bool IsTimePaused => _isTimePaused;
    public PlayPauseBtn PlayPauseButton => _playPauseBtn;
    [SerializeField] private GameObject _playPauseBtnPrefab;
    [SerializeField] private List<float> _btnSpeedGenList;
    [SerializeField] private Transform _speedBtnContentTransform;
    [SerializeField] private GameObject _speedBtnPrefab;

    private DataManager _dataManager;
    private GameManager _gameManager;
    private bool _isTimePaused = true;
    private PlayPauseBtn _playPauseBtn;
    private List<SpeedBtn> _speedBtnList = new List<SpeedBtn>();
    private SpeedBtn _lastUsedSpeedBtn;
    private Coroutine _speedButtonCoroutine;

    private void OnDisable()
    {
        StaggeredSpawnUtils.Stop(this, ref _speedButtonCoroutine);
    }

    public void Init(DataManager dataManager, GameManager gameManager)
    {
        _dataManager = dataManager;
        _gameManager = gameManager;
        BuildPlayPauseButton();
        StaggeredSpawnUtils.Restart(this, ref _speedButtonCoroutine, BuildSpeedButtonsRoutine());
        RefreshUiState();
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsTypingInTextInput())
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }

        for (int i = 0; i < _speedBtnList.Count && i < 9; i++)
        {
            if (!IsNumberKeyDown(i + 1))
            {
                continue;
            }

            OnSpeedBtnClicked(_speedBtnList[i]);
            break;
        }
    }

    private void BuildPlayPauseButton()
    {
        if (_gameManager == null || _playPauseBtnPrefab == null || _speedBtnContentTransform == null)
        {
            return;
        }

        GameObject go = _gameManager.PoolingManager.GetPooledObject(_playPauseBtnPrefab);
        go.transform.SetParent(_speedBtnContentTransform, false);
        go.transform.SetAsFirstSibling();
        _playPauseBtn = go.GetComponent<PlayPauseBtn>();
        _playPauseBtn.Init(TogglePlayPause);
    }

    private IEnumerator BuildSpeedButtonsRoutine()
    {
        if (_gameManager == null || _speedBtnPrefab == null || _speedBtnContentTransform == null)
            yield break;

        _speedBtnList.Clear();

        yield return StaggeredSpawnUtils.ForEachFrame(_btnSpeedGenList.Count, i =>
        {
            float speed = _btnSpeedGenList[i];
            GameObject go = _gameManager.PoolingManager.GetPooledObject(_speedBtnPrefab);
            go.transform.SetParent(_speedBtnContentTransform, false);
            SpeedBtn btn = go.GetComponent<SpeedBtn>();
            int index = i;
            btn.Init(speed, index, OnSpeedBtnClicked);
            _speedBtnList.Add(btn);
        });
    }

    private void TogglePlayPause()
    {
        if (_isTimePaused)
        {
            if (!TutorialInputGate.CanUseTimePlay())
                return;

            Play();
        }
        else
        {
            Pause();
        }
    }

    private void Play()
    {
        SpeedBtn targetBtn = _lastUsedSpeedBtn ?? (_speedBtnList.Count > 0 ? _speedBtnList[0] : null);
        if (targetBtn == null || _dataManager == null)
        {
            return;
        }

        bool wasPaused = _isTimePaused;
        _isTimePaused = false;
        _lastUsedSpeedBtn = targetBtn;
        _dataManager.Time.SetTimeSpeed(targetBtn.Speed);
        RefreshUiState();

        if (wasPaused)
            OnTimePlayStarted?.Invoke();
    }

    private void Pause()
    {
        if (_dataManager == null)
        {
            return;
        }

        _isTimePaused = true;
        _dataManager.Time.SetTimeSpeed(0);
        RefreshUiState();
    }

    private void OnSpeedBtnClicked(SpeedBtn btn)
    {
        if (!TutorialInputGate.CanUseTimePlay())
            return;

        bool wasPaused = _isTimePaused;
        _lastUsedSpeedBtn = btn;
        _isTimePaused = false;
        _dataManager.Time.SetTimeSpeed(btn.Speed);
        RefreshUiState();

        if (wasPaused)
            OnTimePlayStarted?.Invoke();
    }

    private void RefreshUiState()
    {
        if (_playPauseBtn != null)
        {
            _playPauseBtn.SetPausedVisual(_isTimePaused);
        }

        foreach (SpeedBtn speedBtn in _speedBtnList)
        {
            speedBtn.SetSigPanelVisible(!_isTimePaused && speedBtn == _lastUsedSpeedBtn);
        }
    }

    private bool IsNumberKeyDown(int number)
    {
        switch (number)
        {
            case 1:
                return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            case 2:
                return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            case 3:
                return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
            case 4:
                return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            case 5:
                return Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
            case 6:
                return Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6);
            case 7:
                return Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7);
            case 8:
                return Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8);
            case 9:
                return Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9);
            default:
                return false;
        }
    }
}
