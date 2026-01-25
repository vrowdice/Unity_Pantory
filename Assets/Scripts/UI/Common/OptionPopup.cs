using UnityEngine;
using Evo.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;

public class OptionPopup : BasePopup
{
    [SerializeField] private Slider _BGMSlider;
    [SerializeField] private Slider _SFXSlider;
    [SerializeField] private Dropdown _localizatioinDropdown;

    private const string PREFS_LOCALE = "SelectedLocale";

    public override void Init()
    {
        base.Init();
        InitSliders();
        InitLocalizationDropdown();
        Show();
    }

    private void InitSliders()
    {
        _BGMSlider.onValueChanged.RemoveAllListeners();
        _BGMSlider.value = SoundManager.Instance.GetBGMVolume();
        _BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        _SFXSlider.onValueChanged.RemoveAllListeners();
        _SFXSlider.value = SoundManager.Instance.GetSFXVolume();
        _SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void InitLocalizationDropdown()
    {
        _localizatioinDropdown.onItemSelected.RemoveAllListeners();
        _localizatioinDropdown.items.Clear();

        string currentLocaleCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? PlayerPrefs.GetString(PREFS_LOCALE, "en");
        int selectedIndex = GetLocaleIndex(currentLocaleCode);

        _localizatioinDropdown.AddItem("English", null, true);
        _localizatioinDropdown.AddItem("日本語", null, true);
        _localizatioinDropdown.AddItem("한국어", null, true);

        _localizatioinDropdown.SelectItem(selectedIndex, false);
        _localizatioinDropdown.onItemSelected.AddListener(OnLocaleChanged);
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

    private void OnLocaleChanged(int index)
    {
        string localeCode = index switch
        {
            0 => "en",
            1 => "ja",
            2 => "ko",
            _ => "en"
        };

        StartCoroutine(ChangeLocale(localeCode));
    }

    private IEnumerator ChangeLocale(string localeCode)
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

        LocalizationSettings.SelectedLocale = targetLocale;
        PlayerPrefs.SetString(PREFS_LOCALE, localeCode);
        PlayerPrefs.Save();

        yield return null;
    }

    private void OnBGMVolumeChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        SoundManager.Instance.SetSFXVolume(value);
    }

    public void OnClickExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnClickApply()
    {
        SoundManager.Instance.SaveSettings();
        OnClickExit();
    }

    public void OnClickExit()
    {
        SceneLoadManager.Instance.ReloadScene();
        Close();
        Destroy(gameObject);
    }
}
