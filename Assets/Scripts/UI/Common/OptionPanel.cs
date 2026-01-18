using UnityEngine;
using UnityEngine.UI;

public class OptionPanel : MonoBehaviour
{
    [SerializeField] private Slider _BGMSlider;
    [SerializeField] private Slider _SFXSlider;

    private bool _isInitializing = false;

    public void Init()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[OptionPanel] SoundManager.Instance is null. Volume controls will not work.");
            return;
        }

        _isInitializing = true;

        if (_BGMSlider != null)
        {
            _BGMSlider.value = SoundManager.Instance.GetBGMVolume();
            _BGMSlider.onValueChanged.RemoveAllListeners();
            _BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (_SFXSlider != null)
        {
            _SFXSlider.value = SoundManager.Instance.GetSFXVolume();
            _SFXSlider.onValueChanged.RemoveAllListeners();
            _SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        _isInitializing = false;
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (_isInitializing) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (_isInitializing) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }
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
        GameManager.Instance.ShowWarningPanel("Settings have been applied and saved.");
    }

    public void OnClickExit()
    {
        Destroy(gameObject);
    }
}
