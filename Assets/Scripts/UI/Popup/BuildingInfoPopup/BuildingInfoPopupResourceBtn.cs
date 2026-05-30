using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInfoPopupResourceBtn : BtnBase
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amountText;

    private BuildingObject _building;
    private string _resourceId;
    private BuildingInfoPopup _popup;

    public void Init(
        BuildingObject building,
        string resourceId,
        ResourceEntry resourceEntry,
        int amount,
        BuildingInfoPopup popup)
    {
        _building = building;
        _resourceId = resourceId;
        _popup = popup;

        _icon.sprite = resourceEntry.data.icon;
        _amountText.text = amount.ToString("N0");
    }

    protected override void HandleClick()
    {
        _building.TryReturnInputBufferResourceToDataManager(DataManager.Instance, _resourceId);
        _popup.RefreshAfterRuntimeInputBufferChanged();
    }
}
