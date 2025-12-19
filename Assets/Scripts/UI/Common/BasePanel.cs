using UnityEngine;

/// <summary>
/// 모든 패널의 베이스 클래스
/// 공통 기능을 구현합니다.
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    protected GameManager _gameManager;
    protected GameDataManager _dataManager;
    protected MainUiManager _uiManager;
    
    [Header("Animation")]
    [SerializeField] private bool _usePanelAnimation = true;
    [SerializeField] private bool _deactivateAfterClose = true;

    private PanelDoAni _panelAnimator;
    private bool _isAnimatorCached;

    /// <summary>
    /// 패널이 열릴 때 호출됩니다.
    /// </summary>
    public void OnOpen(GameManager argGameManager ,GameDataManager argDataManager, MainUiManager argUIManager)
    {
        _gameManager = argGameManager;
        _dataManager = argDataManager;
        _uiManager = argUIManager;

        PanelDoAni animator = null;
        if (TryGetPanelAnimator(out animator))
        {
            animator.EnsureOpenedPositionCached();
            animator.SnapToClosedPosition();
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (animator != null)
        {
            animator.OpenPanel();
        }

        // 초기화 (자식 클래스에서 구현)
        // DataManager가 null이어도 패널은 열리고, 나중에 사용 가능할 때 초기화
        OnInitialize();
    }

    /// <summary>
    /// 패널이 닫힐 때 호출됩니다.
    /// </summary>
    public void OnClose()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (TryGetPanelAnimator(out var animator))
        {
            animator.ClosePanel(() =>
            {
                if (_deactivateAfterClose && this != null && gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            });
            return;
        }

        if (_deactivateAfterClose)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 패널 초기화 (자식 클래스에서 구현)
    /// </summary>
    protected abstract void OnInitialize();

    void Start()
    {
        // MainUiManager에서 초기화를 관리합니다
    }

    private bool TryGetPanelAnimator(out PanelDoAni animator)
    {
        animator = null;

        if (!_usePanelAnimation)
        {
            return false;
        }

        if (!_isAnimatorCached || _panelAnimator == null)
        {
            _panelAnimator = GetComponent<PanelDoAni>();
            _isAnimatorCached = true;
        }

        if (_panelAnimator == null)
        {
            return false;
        }

        animator = _panelAnimator;
        return true;
    }
}
