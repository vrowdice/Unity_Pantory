using System;
using UnityEngine;

/// <summary>
/// 모든 패널의 베이스 클래스
/// 공통 기능을 구현합니다.
/// </summary>
public abstract class MainCanvasPanelBase : TutorialBase
{
    protected GameManager _gameManager;
    protected DataManager _dataManager;
    protected SoundManager _soundManager;
    protected VisualManager _visualManager;
    protected UIManager _panelUIManager;
    protected MainCanvas _uiManager;
    private Action _cachedOnClose;

    /// <summary>메인 캔버스. 서브 컴포넌트에서 DataManager 등에 접근할 때 사용합니다.</summary>
    public MainCanvas Host => _uiManager;

    /// <summary>Init(MainCanvas) 이후 유효합니다.</summary>
    public UIManager PanelUIManager => _panelUIManager;

    [Header("Animation")]
    [SerializeField] private RectTransform _panelAnimationTarget;
    [SerializeField] private bool _usePanelAnimation = true;
    [SerializeField] private bool _deactivateAfterClose = true;

    [Header("Sound")]
    [SerializeField] private AudioClip _openPanelSfx;

    private PanelDoAni _panelAnimator;
    private bool _isAnimatorCached;

    /// <summary>
    /// 패널이 열릴 때 호출됩니다.
    /// </summary>
    public virtual void Init(MainCanvas argUIManager)
    {
        _gameManager = argUIManager.GameManager;
        _dataManager = argUIManager.DataManager;
        _soundManager = argUIManager.SoundManager;
        _visualManager = argUIManager.VisualManager;
        _panelUIManager = argUIManager.UIManager;
        _uiManager = argUIManager;

        if (_cachedOnClose == null) _cachedOnClose = OnClose;
        _panelUIManager?.PushCloseable(_cachedOnClose);

        gameObject.SetActive(true);

        if (_openPanelSfx != null)
        {
            _soundManager.PlaySFX(_openPanelSfx);
        }
        if (TryGetPanelAnimator(out PanelDoAni animator))
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
        if (_cachedOnClose != null && _panelUIManager != null)
            _panelUIManager.RemoveCloseable(_cachedOnClose);

        if (!gameObject.activeSelf)
        {
            return;
        }

        if (TryGetPanelAnimator(out PanelDoAni animator))
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
