using TMPro;
using UnityEngine;

public class SaveLoadBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _saveDataNameText;
    [SerializeField] private GameObject _loadBtn;

    private SaveLoadPopup _saveLoadPopup;
    private bool _isSaveMode = false;
    private string _saveFileName = string.Empty;

    public void Init(SaveLoadPopup saveLoadPopup, bool isSaveMode, string saveFileName)
    {
        _saveLoadPopup = saveLoadPopup;
        _isSaveMode = isSaveMode;
        _saveFileName = saveFileName;

        if (_saveDataNameText != null)
        {
            _saveDataNameText.text = saveFileName;
        }

        if (_isSaveMode)
        {
            _loadBtn.SetActive(true);
        }
        else
        {
            _loadBtn.SetActive(false);
        }
    }

    public void OnClick()
    {
        if (_isSaveMode)
        {
            _saveLoadPopup.SaveSavefile(_saveFileName);
        }
        else
        {
            _saveLoadPopup.LoadSavefile(_saveFileName);
        }
    }

    public void OnClickLoadBtn()
    {
        _saveLoadPopup.LoadSavefile(_saveFileName);
    }

    public void OnClickDeleteBtn()
    {
        _saveLoadPopup.DeleteSavefile(_saveFileName);
    }
}
