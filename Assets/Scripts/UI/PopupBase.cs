using System;
using System.Collections;
using UnityEngine;

public class PopupBase : TutorialBase
{
    [Header("Sound")]
    [SerializeField] private AudioClip _openPopupSfx;

    public const float DISPLAY_TIME = 0.6f;
    public const float MOVE_DURATION = 0.5f;
    public const float PUNCH_SCALE_DURATION = 1f;

    protected Coroutine _showCoroutine = null;
    protected Coroutine _closeCoroutine = null;

    private Action _onShowCompleteCallback = null;
    private Action _cachedClose;

    protected DataManager _dataManager;
    private bool _isDayEventSubscribed;
    private bool _isHourEventSubscribed;
    private bool _isClosing;
    private bool _destroyAfterClose;

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeDayEvents();
        UnsubscribeHourEvents();

        if (_cachedClose != null && UIManager.Instance != null)
        {
            UIManager.Instance.RemoveCloseable(_cachedClose);
        }
    }

    public virtual void Init()
    {
        GameObjectUtils.ApplyPrefabInstanceName(gameObject);

        CacheOriginalScale();
        EnsureCanvasGroup();

        SetDataManager(DataManager.Instance);
        SubscribeDayEvents();

        gameObject.SetActive(true);
    }

    protected void SetDataManager(DataManager dataManager)
    {
        _dataManager = dataManager ?? DataManager.Instance;
    }

    protected void SubscribeDayEvents()
    {
        if (_isDayEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;
        _isDayEventSubscribed = true;
    }

    protected void UnsubscribeDayEvents()
    {
        if (!_isDayEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _isDayEventSubscribed = false;
    }

    protected void SubscribeHourEvents()
    {
        if (_isHourEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnHourChanged -= HandleHourChanged;
        _dataManager.Time.OnHourChanged += HandleHourChanged;
        _isHourEventSubscribed = true;
    }

    protected void UnsubscribeHourEvents()
    {
        if (!_isHourEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnHourChanged -= HandleHourChanged;
        _isHourEventSubscribed = false;
    }

    protected virtual void HandleDayChanged()
    {
    }

    protected virtual void HandleHourChanged()
    {
    }

    public virtual void Show()
    {
        _isClosing = false;
        if (_cachedClose == null) _cachedClose = Close;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RemoveCloseable(_cachedClose);
            UIManager.Instance.PushCloseable(_cachedClose);
        }
        gameObject.SetActive(true);

        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }
        _showCoroutine = StartCoroutine(ShowEffectCoroutine());
        TryAutoStartTutorialIfPending();
    }

    protected override void Start()
    {
        // 팝업은 Show() 시점에만 자동 튜토리얼을 띄웁니다.
    }

    public virtual Coroutine ShowCoroutine()
    {
        gameObject.SetActive(true);
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }
        _showCoroutine = StartCoroutine(ShowEffectCoroutine());
        return _showCoroutine;
    }

    public virtual IEnumerator ShowEffectCoroutine()
    {
        if (_openPopupSfx != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(_openPopupSfx);
        }

        yield return PlayShowEffectCoroutine();
        _onShowCompleteCallback?.Invoke();
    }

    public virtual void Close()
    {
        RequestClose(false);
    }

    protected void CloseAndDestroy()
    {
        RequestClose(true);
    }

    private void RequestClose(bool destroyAfterClose)
    {
        if (this == null) return;
        if (_isClosing) return;

        if (_cachedClose != null && UIManager.Instance != null)
            UIManager.Instance.RemoveCloseable(_cachedClose);

        if (_showCoroutine != null)
        {
            if (gameObject != null && gameObject.activeInHierarchy)
            {
                StopCoroutine(_showCoroutine);
            }
            _showCoroutine = null;
        }

        if (UsesAnimatedClose() && gameObject.activeInHierarchy)
        {
            _isClosing = true;
            _destroyAfterClose = destroyAfterClose;

            if (_closeCoroutine != null)
                StopCoroutine(_closeCoroutine);
            _closeCoroutine = StartCoroutine(CloseEffectCoroutine());
            return;
        }

        FinalizeClose(destroyAfterClose);
    }

    private void FinalizeClose(bool destroyAfterClose)
    {
        _isClosing = false;
        _closeCoroutine = null;
        gameObject.SetActive(false);

        if (destroyAfterClose)
            Destroy(gameObject);
    }

    protected virtual IEnumerator CloseEffectCoroutine()
    {
        yield return PlayCloseEffectCoroutine();

        _closeCoroutine = null;
        FinalizeClose(_destroyAfterClose);
    }
}
