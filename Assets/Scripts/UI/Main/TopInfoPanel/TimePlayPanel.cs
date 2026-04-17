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
            SpeedBtn targetBtn = _lastUsedSpeedBtn ?? (_speedBtnList.Count > 0 ? _speedBtnList[0] : null);
            if (targetBtn != null)
            {
                targetBtn.OnClick();
            }
        }

        for (int i = 0; i < _speedBtnList.Count && i < 9; i++)
        {
            if (!IsNumberKeyDown(i + 1))
            {
                continue;
            }

            _speedBtnList[i].OnClick();
            break;
        }
    }

    private void BuildSpeedButtons()
    {
        for (int i = 0; i < _btnSpeedGenList.Count; i++)
        {
            float speed = _btnSpeedGenList[i];
            GameObject go = GameManager.Instance.PoolingManager.GetPooledObject(_speedBtnPrefab);
            go.transform.SetParent(_speedBtnContentTransform, false);
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
