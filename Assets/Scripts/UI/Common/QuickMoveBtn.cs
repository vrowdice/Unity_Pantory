using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class QuickMoveBtn : MonoBehaviour
{
    [SerializeField] private AudioClip _alartSound;
    [SerializeField] private Image _icon = null;
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private GameObject _alartImage = null;

    private MainPanelType _penalType;
    private IBuildScenePanelHost _panelHost;

    public MainPanelType PanelType => _penalType;

    public void Initialize(IBuildScenePanelHost panelHost, MainPanelType argPenalType)
    {
        _panelHost = panelHost;
        _penalType = argPenalType;

        _text.text = argPenalType.Localize(LocalizationUtils.TABLE_COMMON);
        _icon.sprite = VisualManager.Instance.GetMainPanelIcon(_penalType.ToString());

        _alartImage.SetActive(false);
        InitEvent();
    }

    public void Initialize(MainCanvas argUiManager, MainPanelType argPenalType)
    {
        Initialize((IBuildScenePanelHost)argUiManager, argPenalType);
    }

    public void OnClick()
    {
        _panelHost?.OpenPanel(_penalType);
        SetAlertActive(false);
    }

    private void OnNewsChanged(NewsState newsState)
    {
        if (newsState != null && !newsState.IsExpired)
        {
            SoundManager.Instance.PlaySFX(_alartSound);
            SetAlertActive(true);
        }
    }

    private void OnOrderChanged(OrderState orderState)
    {
        if (orderState != null)
        {
            SoundManager.Instance.PlaySFX(_alartSound);
            SetAlertActive(true);
        }
    }

    private void SetAlertActive(bool isActive)
    {
        _alartImage.transform.DOKill();

        if (isActive)
        {
            _alartImage.SetActive(true);
            _alartImage.transform.localScale = Vector3.zero;
            _alartImage.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
        else
        {
            if (_alartImage.activeSelf)
            {
                _alartImage.transform.DOScale(0f, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    _alartImage.SetActive(false);
                });
            }
        }
    }

    private void InitEvent()
    {
        switch (_penalType)
        {
            case MainPanelType.News:
                DataManager.Instance.News.OnNewsChanged += OnNewsChanged;
                break;
            case MainPanelType.Order:
                DataManager.Instance.Order.OnOrderChanged += OnOrderChanged;
                break;
            default:
                break;
        }
    }
}
