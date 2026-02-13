using UnityEngine;
using Evo.UI;

/// <summary>
/// 주문 관리 패널
/// </summary>
public class OrderPanel : BasePanel
{
    [SerializeField] private Transform _orderActionBtnContentTransform;
    [SerializeField] private Transform _orderMarketActorPopupBtnScrollViewContentTransform;
    [SerializeField] private Transform _orderBtnScrollViewContentTransform;
    [SerializeField] private Switch _acceptedOrderSwitch;

    [SerializeField] private GameObject _orderMarketActorPopupBtnPrefab;
    [SerializeField] private GameObject _orderBtnPrefab;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Order.OnOrderChanged -= HandleOrderChanged;
        _dataManager.Order.OnOrderChanged += HandleOrderChanged;

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;
    }

    private void HandleOrderChanged(OrderState orderState)
    {

    }

    private void HandleDayChanged()
    {

    }
}

