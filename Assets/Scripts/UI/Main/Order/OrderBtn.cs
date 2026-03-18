using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OrderBtn : MonoBehaviour
{
    [SerializeField] private MarketActorPopupBtn _marketActorPopupBtn;
    [SerializeField] private TextMeshProUGUI _marketActorNameText;
    [SerializeField] private TextMeshProUGUI _trustText;

    [SerializeField] private TextMeshProUGUI _orderTitleText;
    [SerializeField] private TextMeshProUGUI _durationDaysText;
    [SerializeField] private TextMeshProUGUI _rewardTrustText;
    [SerializeField] private TextMeshProUGUI _rewardCreditText;

    [SerializeField] private Transform _RequireResrouceScrollViewContent;
    [SerializeField] private GameObject _orderRequireResourceItemPrefab;

    [SerializeField] private Slider _durationDaysSlider;

    private OrderState _orderState;
    private OrderData _orderData;
    private MarketActorEntry _marketActorEntry;
    private DataManager _dataManager;

    private List<OrderRequireResourceItemPanel> _resourceItemPanels = new List<OrderRequireResourceItemPanel>();

    public void Init(OrderState orderState, MainCanvas mainCanvas)
    {
        _orderState = orderState;
        _dataManager = DataManager.Instance;

        _orderData = _dataManager.Order.GetOrderData(orderState.id);
        _marketActorEntry = _dataManager.MarketActor.GetMarketActorEntry(_orderData.senderActorData.id);
        _marketActorPopupBtn.Init(_marketActorEntry);

        if(_orderState.isAccepted)
        {
            _durationDaysSlider.maxValue = _orderData.durationDays;
        }
        else
        {
            _durationDaysSlider.maxValue = _dataManager.InitialOrderData.orderAcceptanceDelayDays;
        }

        UpdateUI();
    }


    public void UpdateUI()
    {
        if (_orderState == null || _orderData == null || _marketActorEntry == null) return;

        _marketActorNameText.text = _marketActorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        _trustText.text = _marketActorEntry.state.trust.ToString();
        _orderTitleText.text = _orderData.id.Localize(LocalizationUtils.TABLE_ORDER);
        _durationDaysText.text = _orderState.durationDays.ToString();
        _rewardTrustText.text = _orderData.rewardTrust.ToString();
        _rewardCreditText.text = ReplaceUtils.FormatNumberWithCommas(_orderState.rewardCredit);

        _durationDaysSlider.value = _orderState.durationDays;

        RefreshRequireResourceItems();
    }

    private void RefreshRequireResourceItems()
    {
        if (_RequireResrouceScrollViewContent == null || _orderRequireResourceItemPrefab == null) return;
        if (_orderState.resourceRequestList == null) return;

        if (_resourceItemPanels.Count == 0)
        {
            foreach (OrderState.ResourceRequest request in _orderState.resourceRequestList)
            {
                ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(request.resourceId);
                if (resourceEntry == null) continue;

                GameObject itemObj = Instantiate(_orderRequireResourceItemPrefab, _RequireResrouceScrollViewContent);
                OrderRequireResourceItemPanel panel = itemObj.GetComponent<OrderRequireResourceItemPanel>();
                if (panel != null)
                {
                    panel.Init(resourceEntry, request.requiredCount);
                    _resourceItemPanels.Add(panel);
                }
            }
        }
        else
        {
            for (int i = 0; i < _resourceItemPanels.Count && i < _orderState.resourceRequestList.Count; i++)
            {
                OrderRequireResourceItemPanel panel = _resourceItemPanels[i];
                if (panel != null)
                {
                    panel.UpdateUI();
                }
            }
        }
    }

    public void OnClick()
    {
        if (_orderState == null) return;

        _dataManager.Order.AcceptAndCompleteOrder(_orderState);

        UpdateUI();
    }
}
