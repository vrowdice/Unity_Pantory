/// <summary>
/// GameSaveData에 상태를 저장/복원하는 핸들러.
/// </summary>
public interface IGameSaveHandler
{
    void CaptureTo(GameSaveData saveData);
    void ApplyFromSave(GameSaveData saveData);
}
