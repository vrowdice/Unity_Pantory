using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInfoPopupResourceBtn : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amountText;

    private BuildingObject _building;
    private string _resourceId;
    private bool _fromInputBuffer;
    private BuildingInfoPopup _popup;

    public void Init(
        BuildingObject building,
        string resourceId,
        bool fromInputBuffer,
        ResourceEntry resourceEntry,
        int amount,
        BuildingInfoPopup popup)
    {
        _building = building;
        _resourceId = resourceId;
        _fromInputBuffer = fromInputBuffer;
        _popup = popup;

        _icon.sprite = resourceEntry.data.icon;
        _amountText.text = amount.ToString("N0");
    }

    public void OnClick()
    {
        _building.TryReturnBufferResourceToDataManager(DataManager.Instance, _resourceId, _fromInputBuffer);
        _popup.RefreshAfterRuntimeBufferChanged();
    }
}
