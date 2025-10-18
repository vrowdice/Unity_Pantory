using UnityEngine;

/// <summary>
/// 모든 패널의 베이스 클래스
/// 공통 기능을 구현합니다.
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    protected GameDataManager _dataManager;
    protected IUIManager _uiManager;

    /// <summary>
    /// 패널이 열릴 때 호출됩니다.
    /// </summary>
    public void OnOpen(GameDataManager argDataManager, IUIManager argUIManager)
    {
        _dataManager = argDataManager;
        _uiManager = argUIManager;

        // 패널 표시
        gameObject.SetActive(true);

        // 초기화 (자식 클래스에서 구현)
        // DataManager가 null이어도 패널은 열리고, 나중에 사용 가능할 때 초기화
        OnInitialize();

        Debug.Log($"[{GetType().Name}] Panel opened.");
    }

    /// <summary>
    /// 패널이 닫힐 때 호출됩니다.
    /// </summary>
    public void OnClose()
    {
        // 패널 숨김
        gameObject.SetActive(false);

        Debug.Log($"[{GetType().Name}] Panel closed.");
    }

    /// <summary>
    /// 패널 초기화 (자식 클래스에서 구현)
    /// </summary>
    protected abstract void OnInitialize();

    void Start()
    {
        // MainUiManager에서 초기화를 관리합니다
    }
}
