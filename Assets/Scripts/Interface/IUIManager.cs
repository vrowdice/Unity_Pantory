using UnityEngine;

public interface IUIManager
{
    Transform CanvasTrans { get; }
    GameObject ProductionInfoImage { get; }

    void UpdateAllMainText();

    void OnInitialize(GameManager argGameManager, DataManager argGameDataManager);
}
