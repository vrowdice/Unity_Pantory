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
