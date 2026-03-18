using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimePlayPanel : MonoBehaviour
{
    [SerializeField] private List<float> _btnSpeedGenList;
    [SerializeField] private Transform _speedBtnContentTransform;
    [SerializeField] private GameObject _speedBtnPrefab;

    private DataManager _dataManager;
    private bool _isTimePaused = true;
    private List<SpeedBtn> _speedBtnList = new List<SpeedBtn>();
    private SpeedBtn _lastUsedSpeedBtn;

    private void Start()
    {
        _dataManager = DataManager.Instance;

        BuildSpeedButtons();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(_lastUsedSpeedBtn != null) _lastUsedSpeedBtn.OnClick();
        }
    }

    private void BuildSpeedButtons()
    {
        for (int i = 0; i < _btnSpeedGenList.Count; i++)
        {
            float speed = _btnSpeedGenList[i];
            GameObject go = Instantiate(_speedBtnPrefab, _speedBtnContentTransform);
            SpeedBtn btn = go.GetComponent<SpeedBtn>();
            int index = i;
            btn.Init(speed, index, OnSpeedBtnClicked);
            _speedBtnList.Add(btn);
        }
    }

    private void OnSpeedBtnClicked(SpeedBtn btn)
    {
        foreach (SpeedBtn speedBtn in _speedBtnList)
            speedBtn.SetSigPanelVisible(false);

        if(_lastUsedSpeedBtn == btn && !_isTimePaused)
        {
            _isTimePaused = true;
            _dataManager.Time.SetTimeSpeed(0);
            _lastUsedSpeedBtn = btn;
            return;
        }

        _isTimePaused = false;
        _dataManager.Time.SetTimeSpeed(btn.Speed);
        btn.SetSigPanelVisible(true);

        _lastUsedSpeedBtn = btn;
    }
}
