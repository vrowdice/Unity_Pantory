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
        if (DataManager.Instance.Player.HasCompletedIntroTutorial)
        {
            SaveLoadManager.Instance?.StartNewGame(DataManager.Instance);
            SceneLoadManager.LoadScene("Main");
        }
        else
        {
            // 튜토리얼은 씬 로드 시 GameManager → ResetToTutorialGame()으로 새 게임 적용
            SceneLoadManager.LoadScene("Tutorial");
        }
    }

    public void ShowSaveLoadPopup()
    {
        UIManager.ShowSaveLoadPopup(false);
    }

    public void ShowOptionPopup()
    {
        UIManager.ShowOptionPopup();
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
