using UnityEngine;

/// <summary>
/// 게임 데이터 저장/로드를 처리하는 매니저 클래스
/// 각 데이터 타입별 저장/로드 기능은 핸들러로 구분되어 관리됩니다.
/// </summary>
public class SaveLoadManager : Singleton<SaveLoadManager>
{
    public ThreadSaveLoadHandler Thread { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        
        if (Instance != this) return;
        
        InitializeHandlers();
    }

    public void Init()
    {
        if (Instance != this) return;
        
        InitializeHandlers();
    }

    private void InitializeHandlers()
    {
        Thread = new ThreadSaveLoadHandler(this);
    }
}
