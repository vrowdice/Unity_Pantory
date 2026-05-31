using UnityEngine;

public class TitleRunner : RunnerBase
{
    [SerializeField] private TitleCanvas _titleCanvas;

    public override void Init()
    {
        base.Init();

        SaveLoadManager.Instance?.TryLoadUserSettingsAndApply();
        _titleCanvas.Init(this);
    }
}
