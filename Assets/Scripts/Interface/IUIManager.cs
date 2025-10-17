using UnityEngine;

public interface IUIManager
{
    Transform CanvasTrans { get; }
    GameObject ProductionInfoImage { get; }

    void UpdateAllMainText();

    void Initialize(GameManager argGameManager, GameDataManager argGameDataManager);
}
