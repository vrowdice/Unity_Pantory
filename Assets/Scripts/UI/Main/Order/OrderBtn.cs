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
    private GameManager _gameManager;

    private List<OrderRequireResourceItemPanel> _resourceItemPanels = new List<OrderRequireResourceItemPanel>();

    public void Init(OrderState orderState, MainCanvas mainCanvas)
    {
        _orderState = orderState;
        _dataManager = mainCanvas.DataManager;
        _gameManager = mainCanvas.GameManager;

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

        int targetCount = _orderState.resourceRequestList.Count;

        while (_resourceItemPanels.Count > targetCount)
        {
            int lastIndex = _resourceItemPanels.Count - 1;
            OrderRequireResourceItemPanel panelToReturn = _resourceItemPanels[lastIndex];
            _resourceItemPanels.RemoveAt(lastIndex);
            if (panelToReturn != null)
            {
                _gameManager.PoolingManager.ReturnToPool(panelToReturn.gameObject);
            }
        }

        for (int i = 0; i < targetCount; i++)
        {
            OrderState.ResourceRequest request = _orderState.resourceRequestList[i];
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(request.resourceId);
            if (resourceEntry == null) continue;

            if (i >= _resourceItemPanels.Count)
            {
                GameObject itemObj = _gameManager.PoolingManager.GetPooledObject(_orderRequireResourceItemPrefab);
                itemObj.transform.SetParent(_RequireResrouceScrollViewContent, false);
                OrderRequireResourceItemPanel newPanel = itemObj.GetComponent<OrderRequireResourceItemPanel>();
                if (newPanel != null)
                {
                    _resourceItemPanels.Add(newPanel);
                }
            }

            if (i < _resourceItemPanels.Count)
            {
                OrderRequireResourceItemPanel panel = _resourceItemPanels[i];
                if (panel != null)
                {
                    panel.Init(resourceEntry, request.requiredCount);
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
