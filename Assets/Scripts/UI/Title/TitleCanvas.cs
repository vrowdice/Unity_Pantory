using UnityEngine;

public class TitleCanvas : CanvasBase
{
    public void Init(TitleRunner titleRunner)
    {
        base.Init();
        UpdateAllMainText();
    }

    public void GoToMainScene()
    {
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.HasCompletedIntroTutorial)
        {
            SaveLoadManager.Instance?.StartNewGame(DataManager.Instance);
            SceneLoadManager.LoadScene("Main");
        }
        else
        {
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
