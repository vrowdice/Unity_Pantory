using UnityEngine;

public class TitleCanvas : CanvasBase
{
    public void Init(TitleRunner titleRunner)
    {
        base.Init();

        UpdateAllMainText();
    }

    public void ShowOptionPanel()
    {
        GameManager.ShowOptionPanel();
    }
}
