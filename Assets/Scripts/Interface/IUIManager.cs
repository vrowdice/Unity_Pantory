using UnityEngine;

public interface IUIManager
{
    Transform CanvasTrans { get; }

    void UpdateAllMainText();

    void Initialize(GameManager argGameManager, GameDataManager argGameDataManager);
}
