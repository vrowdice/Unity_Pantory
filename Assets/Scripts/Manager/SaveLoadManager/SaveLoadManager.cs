using UnityEngine;

/// <summary>
/// 게임 데이터 저장/로드를 처리하는 매니저 클래스
/// 각 데이터 타입별 저장/로드 기능은 핸들러로 구분되어 관리됩니다.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    public ThreadSaveLoadHandler Thread { get; private set; }

    public void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeHandlers();
    }

    private void InitializeHandlers()
    {
        Thread = new ThreadSaveLoadHandler(this);
    }
}
