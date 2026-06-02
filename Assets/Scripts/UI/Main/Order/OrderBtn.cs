using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OrderBtn : EntryListBtnBase
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
    [SerializeField] private RectTransform _completeAnimationTarget;

    private OrderState _orderState;
    private OrderData _orderData;
    private MarketActorEntry _marketActorEntry;
    private DataManager _dataManager;
    private GameManager _gameManager;
    private AudioClip _requirementCompleteSfx;
    private bool _wasRequirementsReady;

    private readonly List<OrderRequireResourceItemPanel> _resourceItemPanels = new List<OrderRequireResourceItemPanel>();

    public void Init(OrderState orderState, MainCanvas mainCanvas, AudioClip requirementCompleteSfx)
    {
        _orderState = orderState;
        _dataManager = mainCanvas.DataManager;
        _gameManager = mainCanvas.GameManager;
        _requirementCompleteSfx = requirementCompleteSfx;
        _wasRequirementsReady = false;

        _orderData = _dataManager.Order.GetOrderData(orderState.id);
        _marketActorEntry = _dataManager.MarketActor.GetMarketActorEntry(orderState.senderActorId);
        _marketActorPopupBtn.Init(_marketActorEntry);

        if (_orderState.isAccepted)
        {
            _durationDaysSlider.maxValue = _orderData.durationDays;
        }
        else
        {
            _durationDaysSlider.maxValue = _dataManager.InitialOrderData.orderAcceptanceDelayDays;
        }

        Refresh();
    }

    public override void Refresh()
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
        RefreshRequirementReadyFeedback();
    }

    private void RefreshRequirementReadyFeedback()
    {
        bool isReady = _dataManager.Order.CanFulfillOrderRequirements(_orderState);
        Transform target = GetCompleteAnimationTarget();
        RequirementCompleteFeedbackUtils.NotifyBecameReady(
            ref _wasRequirementsReady,
            isReady,
            target,
            _requirementCompleteSfx);
    }

    private Transform GetCompleteAnimationTarget()
    {
        if (_completeAnimationTarget != null)
            return _completeAnimationTarget;

        Evo.UI.Button button = ResolveButton();
        if (button != null)
            return button.transform;

        return transform;
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

    protected override void HandleClick()
    {
        if (_orderState == null) return;

        _dataManager.Order.AcceptAndCompleteOrder(_orderState);

        Refresh();
    }
}
