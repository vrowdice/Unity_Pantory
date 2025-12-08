public interface IGameSceneManagerComponent
{
    /// <summary>
    /// GameManager에서 호출되는 초기화 메서드.
    /// </summary>
    /// <param name="gameManager">현재 GameManager 인스턴스</param>
    /// <param name="dataManager">현재 GameDataManager 인스턴스</param>
    void Initialize(GameManager gameManager, GameDataManager dataManager);
}

