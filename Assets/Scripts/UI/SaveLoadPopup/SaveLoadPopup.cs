using UnityEngine;
using TMPro;

public class SaveLoadPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private GameObject _newSavefileBtn;

    [SerializeField] private GameObject _saveLoadBtnPrefab;

    private bool _isSaveMode = false;

    public void Init(bool isSaveMode)
    {
        _isSaveMode = isSaveMode;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_isSaveMode)
        {
            _titleText.text = LocalizationUtils.Localize("Save");
            _newSavefileBtn.SetActive(true);
        }
        else
        {
            _titleText.text = LocalizationUtils.Localize("Load");
            _newSavefileBtn.SetActive(false);
        }
    }
    
    public void SaveSavefile()
    {

    }

    public void LoadSavefile()
    {

    }

    public void DeleteSavefile()
    {

    }

    public void CreateNewSavefile()
    {

    }
}
