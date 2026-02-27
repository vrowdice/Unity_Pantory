using UnityEngine;
using TMPro;
using DG.Tweening;

public class WarningPopup : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private TextMeshProUGUI _messageText;

    [Header("Fade Animation")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float _displayDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float _fadeOutDuration = 0.3f;

    private Sequence _sequence;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
    }

    /// <summary>
    /// 경고 메시지 키로 로컬라이즈 후 표시하고 페이드인 → 표시 → 페이드아웃 후 파괴
    /// </summary>
    /// <param name="messageKey">WarningMessage 테이블의 로컬라이즈 키</param>
    public void Init(string messageKey)
    {
        if (_messageText != null)
        {
            _messageText.text = messageKey.Localize(LocalizationUtils.TABLE_WARNING_MESSAGE);
        }

        _sequence?.Kill();

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        _sequence = DOTween.Sequence();
        _sequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration).SetEase(Ease.OutCubic));
        _sequence.AppendInterval(_displayDuration);
        _sequence.Append(_canvasGroup.DOFade(0f, _fadeOutDuration).SetEase(Ease.InCubic));
        _sequence.OnComplete(DestroyObj);
        _sequence.SetUpdate(UpdateType.Normal);
    }

    private void DestroyObj()
    {
        _sequence = null;
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        Destroy(gameObject);
    }
}
