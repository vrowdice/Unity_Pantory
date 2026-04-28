using UnityEngine;

public class TitleRunner : RunnerBase
{
    [SerializeField] private TitleCanvas _titleCanvas;

    override public void Init()
    {
        base.Init();

        SaveLoadManager.Instance?.TryLoadUserSettingsAndApply();
        _titleCanvas.Init(this);
    }
}
