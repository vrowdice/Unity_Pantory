using TMPro;
using UnityEngine;

public class SaveLoadBtn : BtnBase
{
    [SerializeField] private TextMeshProUGUI _saveDataNameText;
    [SerializeField] private TextMeshProUGUI _saveDataDateText;
    [SerializeField] private GameObject _loadBtn;
    [SerializeField] private Evo.UI.Button _loadButton;
    [SerializeField] private Evo.UI.Button _deleteButton;

    private SaveLoadPopup _saveLoadPopup;
    private bool _isSaveMode = false;
    private string _saveFileName = string.Empty;

    public void Init(SaveLoadPopup saveLoadPopup, bool isSaveMode, string saveFileName, string saveDateText = "")
    {
        _saveLoadPopup = saveLoadPopup;
        _isSaveMode = isSaveMode;
        _saveFileName = saveFileName;

        if (_saveDataNameText != null)
        {
            _saveDataNameText.text = saveFileName;
        }

        if (_saveDataDateText != null)
        {
            _saveDataDateText.text = saveDateText ?? string.Empty;
        }

        if (_loadButton == null && _loadBtn != null)
        {
            _loadButton = _loadBtn.GetComponent<Evo.UI.Button>();
        }

        _loadBtn.SetActive(false);

        BindClick(_loadButton, OnClickLoad);
        BindClick(_deleteButton, OnClickDelete);
    }

    protected override void HandleClick()
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

    private void OnClickLoad()
    {
        _saveLoadPopup.LoadSavefile(_saveFileName);
    }

    private void OnClickDelete()
    {
        _saveLoadPopup.DeleteSavefile(_saveFileName);
    }
}
