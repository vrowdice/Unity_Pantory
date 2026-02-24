using System;
using UnityEngine;

/// <summary>
/// 모든 패널의 베이스 클래스
/// 공통 기능을 구현합니다.
/// </summary>
public abstract class BaseMainCanvasPanel : MonoBehaviour
{
    protected GameManager _gameManager;
    protected DataManager _dataManager;
    protected MainCanvas _uiManager;
    private Action _cachedOnClose;

    [Header("Animation")]
    [SerializeField] private RectTransform _panelAnimationTarget;
    [SerializeField] private bool _usePanelAnimation = true;
    [SerializeField] private bool _deactivateAfterClose = true;

    private PanelDoAni _panelAnimator;
    private bool _isAnimatorCached;

    /// <summary>
    /// 패널이 열릴 때 호출됩니다.
    /// </summary>
    public virtual void Init(MainCanvas argUIManager)
    {
        _gameManager = GameManager.Instance;
        _dataManager = DataManager.Instance;
        _uiManager = argUIManager;

        if (_cachedOnClose == null) _cachedOnClose = OnClose;
        if (_gameManager != null)
        {
            _gameManager.PushCloseable(_cachedOnClose);
        }

        gameObject.SetActive(true);

        if (TryGetPanelAnimator(out var animator))
        {
            animator.SnapToClosedPosition();
            animator.OpenPanel();
        }
    }

    /// <summary>
    /// 패널이 닫힐 때 호출됩니다.
    /// </summary>
    public void OnClose()
    {
        if (_cachedOnClose != null && _gameManager != null)
        {
            _gameManager.RemoveCloseable(_cachedOnClose);
        }

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

        _uiManager.CloseAllPanels();
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
            Transform target = _panelAnimationTarget != null ? _panelAnimationTarget : transform;
            _panelAnimator = target.GetComponent<PanelDoAni>();
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
