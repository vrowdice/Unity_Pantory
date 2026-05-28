using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SaveLoadPopup : PopupBase
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private GameObject _newSavefileBtn;
    [SerializeField] private Transform _saveLoadBtnContentTransform;
    [SerializeField] private GameObject _saveLoadBtnPrefab;

    private bool _isSaveMode = false;
    private string _selectedSaveFileName = string.Empty;
    private readonly List<SaveLoadBtn> _saveLoadBtns = new List<SaveLoadBtn>();
    private Coroutine _refreshCoroutine;

    public void Init(bool isSaveMode)
    {
        base.Init();

        _isSaveMode = isSaveMode;
        _selectedSaveFileName = string.Empty;

        UpdateUI();
        RefreshSaveFileList();

        Show();
    }

    public override void Close()
    {
        StaggeredSpawnUtils.Stop(this, ref _refreshCoroutine);
        base.Close();
    }

    private void UpdateUI()
    {
        if (_isSaveMode)
        {
            _titleText.text = "Save".Localize(LocalizationUtils.TABLE_COMMON);
            _newSavefileBtn.SetActive(true);
        }
        else
        {
            _titleText.text = "Load".Localize(LocalizationUtils.TABLE_COMMON);
            _newSavefileBtn.SetActive(false);
        }
    }

    public void RefreshSaveFileList()
    {
        StaggeredSpawnUtils.Restart(this, ref _refreshCoroutine, RefreshSaveFileListRoutine());
    }

    private IEnumerator RefreshSaveFileListRoutine()
    {
        if (_saveLoadBtnContentTransform == null || _saveLoadBtnPrefab == null)
        {
            Debug.LogError("[SaveLoadPopup] Container or Prefab is null.");
            yield break;
        }

        foreach (SaveLoadBtn btn in _saveLoadBtns)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }

        _saveLoadBtns.Clear();

        List<string> saveFiles = SaveLoadManager.Instance.GetSaveFileList();

        yield return StaggeredSpawnUtils.ForEachFrame(saveFiles.Count, i =>
        {
            string fileName = saveFiles[i];
            GameObject btnObj = Instantiate(_saveLoadBtnPrefab, _saveLoadBtnContentTransform);
            SaveLoadBtn btn = btnObj.GetComponent<SaveLoadBtn>();
            if (btn != null)
            {
                btn.Init(this, _isSaveMode, fileName);
                _saveLoadBtns.Add(btn);
            }
        });
    }

    public void SetSelectedSaveFileName(string fileName)
    {
        _selectedSaveFileName = fileName;
    }

    public void SaveSavefile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.SaveFileNameEmpty);
            return;
        }

        if (SaveLoadManager.Instance.HasSaveFile(fileName))
        {
            UIManager.Instance.ShowConfirmPopup(ConfirmMessage.OverwriteConfirm, () => DoSaveSavefile(fileName));
            return;
        }

        DoSaveSavefile(fileName);
    }

    private void DoSaveSavefile(string fileName)
    {
        bool success = SaveLoadManager.Instance.SaveSavefile(fileName, DataManager.Instance);
        if (success)
        {
            RefreshSaveFileList();
            UIManager.Instance.ShowWarningPopup(WarningMessage.SaveSuccess);
            UIManager.Instance.CloseAllPopups();
        }
        else
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.SaveFailed);
        }
    }

    public void LoadSavefile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.LoadFileNameEmpty);
            return;
        }

        if (!SaveLoadManager.Instance.HasSaveFile(fileName))
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.SaveFileNotFound);
            return;
        }

        UIManager.Instance.ShowConfirmPopup(ConfirmMessage.LoadConfirm, () =>
        {
            bool success = SaveLoadManager.Instance.LoadSaveFile(fileName, DataManager.Instance);
            if (success)
            {
                SceneLoadManager.Instance.LoadScene("Main");
                Destroy(gameObject);
                UIManager.Instance.CloseAllPopups();
            }
            else
            {
                UIManager.Instance.ShowWarningPopup(WarningMessage.LoadFailed);
            }
        });
    }

    public void DeleteSavefile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.DeleteFileNameEmpty);
            return;
        }

        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("[SaveLoadPopup] SaveLoadManager is null.");
            return;
        }

        UIManager.Instance.ShowConfirmPopup(ConfirmMessage.DeleteConfirm, () =>
        {
            bool success = SaveLoadManager.Instance.DeleteSaveFile(fileName);
            if (success)
            {
                RefreshSaveFileList();
                UIManager.Instance.ShowWarningPopup(WarningMessage.DeleteSuccess);
            }
            else
            {
                UIManager.Instance.ShowWarningPopup(WarningMessage.DeleteFailed);
            }
        });
    }

    public void CreateNewSavefile()
    {
        UIManager.Instance.ShowEnterNamePopup((string fileName) =>
        {
            if (string.IsNullOrEmpty(fileName))
            {
                UIManager.Instance.ShowWarningPopup(WarningMessage.SaveFileNameEmpty);
                return;
            }

            SaveSavefile(fileName);
        });
    }

    public void OnClickClose()
    {
        CloseAndDestroy();
    }
}
