using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderBtn : MonoBehaviour
{
    [SerializeField] private MarketActorPopupBtn _marketActorPopupBtn;
    [SerializeField] private TextMeshProUGUI _marketActorNameText;
    [SerializeField] private TextMeshProUGUI _trustText;

    [SerializeField] private TextMeshProUGUI _orderTitleText;
    [SerializeField] private TextMeshProUGUI _durationDaysText;
    [SerializeField] private TextMeshProUGUI _getTrustText;

    [SerializeField] private Transform _RequireResrouceScrollViewContent;
    [SerializeField] private GameObject _orderRequireResourceItemPrefab;

    [SerializeField] private Slider _durationDaysSlider;

    private OrderState _orderState;

    public void Init(OrderState orderState)
    {
        _orderState = orderState;
    }

    public void UpdateUI()
    {

    }

    public void OnClick()
    {

    }
}
