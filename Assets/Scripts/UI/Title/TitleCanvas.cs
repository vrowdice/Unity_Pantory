using UnityEngine;

public class TitleCanvas : CanvasBase
{
    public void Init(TitleRunner titleRunner)
    {
        base.Init();

        // 언어·볼륨 전체: TitleRunner.Init 에서 TryLoadUserSettingsAndApply. 이후 씬 전환 시 볼륨만 SoundManager 가 갱신합니다.
        UpdateAllMainText();
    }

    public void GoToMainScene()
    {
        SceneLoadManager.Instance.LoadScene("Main");
    }

    public void ShowSaveLoadPopup()
    {
        UIManager.Instance.ShowSaveLoadPopup(false);
    }

    public void ShowOptionPopup()
    {
        UIManager.Instance.ShowOptionPopup();
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
