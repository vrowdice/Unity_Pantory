using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class SaveLoadBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _saveDataNameText;
    [SerializeField] private GameObject _loadBtn;

    private SaveLoadPopup _saveLoadPopup;
    private bool _isSaveMode = false;

    public void Init(SaveLoadPopup saveLoadPopup, bool isSaveMode)
    {
        _isSaveMode = isSaveMode;

        if (isSaveMode)
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
            _saveLoadPopup.SaveSavefile();
        }
        else
        {
            _saveLoadPopup.LoadSavefile();
        }
    }

    public void OnClickLoadBtn()
    {
        _saveLoadPopup.LoadSavefile();
    }

    public void OnClickDeleteBtn()
    {
        _saveLoadPopup.DeleteSavefile();
    }
}
