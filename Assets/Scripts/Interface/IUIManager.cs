using UnityEngine;

public interface IUIManager
{
    Transform CanvasTrans { get; }
    GameObject ProductionInfoImage { get; }

    void Init();
    void UpdateAllMainText();
}
