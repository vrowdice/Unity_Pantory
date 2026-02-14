using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class QuickMoveBtn : MonoBehaviour
{
    [SerializeField] private Image _icon = null;
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private GameObject _alartImage = null;

    private MainPanelType _penalType;
    MainCanvas _mainUiManager = null;

    public void Initialize(MainCanvas argUiManager, MainPanelType argPenalType)
    {
        _mainUiManager = argUiManager;
        _penalType = argPenalType;

        _text.text = argPenalType.Localize();
        _icon.sprite = VisualManager.Instance.GetMainPanelIcon(_penalType.ToString());

        _alartImage.SetActive(false);
        InitEvent();
    }

    public void OnClick()
    {
        _mainUiManager.OpenPanel(_penalType);
        SetAlertActive(false);
    }

    private void OnNewsChanged(NewsState newsState)
    {
        if (newsState != null && !newsState.IsExpired)
        {
            SetAlertActive(true);
        }
    }

    private void OnOrderChanged(OrderState orderState)
    {
        if (orderState != null)
        {
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
