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
    protected IBuildScenePanelHost _panelHost;
    protected MainCanvas _uiManager;
    private Action _cachedOnClose;

    public MainCanvas Host => _uiManager;
    public IBuildScenePanelHost PanelHost => _panelHost;
    public GameManager GameManager => _gameManager;
    public VisualManager VisualManager => _visualManager;
    public DataManager DataManager => _dataManager;
    public UIManager UIManager => _panelUIManager;
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
        Init((IBuildScenePanelHost)argUIManager);
    }

    public virtual void Init(IBuildScenePanelHost panelHost)
    {
        if (panelHost != null)
        {
            _panelHost = panelHost;
            _uiManager = panelHost as MainCanvas;
            _gameManager = panelHost.GameManager;
            _dataManager = panelHost.DataManager;
            _soundManager = panelHost.SoundManager;
            _visualManager = panelHost.VisualManager;
            _panelUIManager = panelHost.UIManager;
        }

        EnsurePanelServices();

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

        _uiManager?.CloseAllPanels();
        _panelHost?.CloseAllPanels();
    }

    protected void EnsurePanelServices()
    {
        if (_gameManager == null)
            _gameManager = GameManager.Instance;
        if (_dataManager == null)
            _dataManager = DataManager.Instance;
        if (_visualManager == null)
            _visualManager = VisualManager.Instance;
        if (_soundManager == null)
            _soundManager = SoundManager.Instance;
        if (_panelUIManager == null)
            _panelUIManager = global::UIManager.Instance;
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
