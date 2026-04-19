using UnityEngine;
using Evo.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;

public class OptionPopup : PopupBase
{
    [SerializeField] private Slider _BGMSlider;
    [SerializeField] private Slider _SFXSlider;
    [SerializeField] private Dropdown _localizatioinDropdown;
    [SerializeField] private GameObject _saveBtn;

    private const string PREFS_LOCALE = "SelectedLocale";

    private string _pendingLocaleCode;
    private float _pendingBGMVolume;
    private float _pendingSFXVolume;

    public override void Init()
    {
        base.Init();
        InitSliders();
        InitLocalizationDropdown();

        if(SceneLoadManager.Instance.CurrentSceneName == "Main")
        {
            _saveBtn.SetActive(true);
        }
        else
        {
            _saveBtn.SetActive(false);
        }

        Show();
    }

    private void InitSliders()
    {
        _pendingBGMVolume = SoundManager.Instance.GetBGMVolume();
        _pendingSFXVolume = SoundManager.Instance.GetSFXVolume();

        _BGMSlider.onValueChanged.RemoveAllListeners();
        _BGMSlider.value = _pendingBGMVolume;
        _BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        _SFXSlider.onValueChanged.RemoveAllListeners();
        _SFXSlider.value = _pendingSFXVolume;
        _SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void InitLocalizationDropdown()
    {
        _localizatioinDropdown.onItemSelected.RemoveAllListeners();
        _localizatioinDropdown.items.Clear();

        _pendingLocaleCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? PlayerPrefs.GetString(PREFS_LOCALE, "en");
        int selectedIndex = GetLocaleIndex(_pendingLocaleCode);

        _localizatioinDropdown.AddItem("English", null, true);
        _localizatioinDropdown.AddItem("日本語", null, true);
        _localizatioinDropdown.AddItem("한국어", null, true);

        _localizatioinDropdown.SelectItem(selectedIndex, false);
        _localizatioinDropdown.onItemSelected.AddListener(OnLocaleSelectionChanged);
    }

    private int GetLocaleIndex(string localeCode)
    {
        switch (localeCode.ToLower())
        {
            case "en":
                return 0;
            case "ja":
                return 1;
            case "ko":
                return 2;
            default:
                return 0;
        }
    }

    private void OnLocaleSelectionChanged(int index)
    {
        _pendingLocaleCode = index switch
        {
            0 => "en",
            1 => "ja",
            2 => "ko",
            _ => "en"
        };
    }

    private void ApplyLocale(string localeCode)
    {
        IList<Locale> availableLocales = LocalizationSettings.AvailableLocales.Locales;
        Locale targetLocale = null;

        foreach (Locale locale in availableLocales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                targetLocale = locale;
                break;
            }
        }

        if (targetLocale != null)
        {
            LocalizationSettings.SelectedLocale = targetLocale;
            PlayerPrefs.SetString(PREFS_LOCALE, localeCode);
            PlayerPrefs.Save();
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        _pendingBGMVolume = value;
    }

    private void OnSFXVolumeChanged(float value)
    {
        _pendingSFXVolume = value;
    }

    public void OnClickGoTitle()
    {
        UIManager.Instance.ShowConfirmPopup(ConfirmMessage.UnsavedProgressConfirm, () =>
        {
            SceneLoadManager.Instance.LoadScene("Title");
            OnClickExit();
        });
    }

    public void OnClickExitGame()
    {
        UIManager.Instance.ShowConfirmPopup(ConfirmMessage.UnsavedProgressConfirm, () =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            OnClickExit();
        });
    }

    public void OnClickSave()
    {
        UIManager.Instance.ShowSaveLoadPopup(true);
    }

    public void OnClickLoad()
    {
        UIManager.Instance.ShowSaveLoadPopup(false);
    }

    public void OnClickApply()
    {
        ApplyLocale(_pendingLocaleCode);
        SoundManager.Instance.SetBGMVolume(_pendingBGMVolume);
        SoundManager.Instance.SetSFXVolume(_pendingSFXVolume);
        SoundManager.Instance.SaveSettings();

        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.CurrentSceneName == "Main")
        {
            MainRunner mainRunner = UnityEngine.Object.FindAnyObjectByType<MainRunner>();
            mainRunner?.FlushPlacedLayoutToDataManager();
        }

        SceneLoadManager.Instance.ReloadScene();
        OnClickExit();
    }

    public void OnClickExit()
    {
        CloseAndDestroy();
    }
}
