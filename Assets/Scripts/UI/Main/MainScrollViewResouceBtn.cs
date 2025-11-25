using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainScrollViewResouceBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _valueText = null;
    [SerializeField] private TextMeshProUGUI _changeValueText = null;

    private MainUiManager _mainUiManager = null;
    private ResourceEntry _resourceEntry = null;

    public void OnInitialize(MainUiManager argMainUiManager, ResourceEntry resourceEntry)
    {
        _mainUiManager = argMainUiManager;
        _resourceEntry = resourceEntry;

        _image.sprite = resourceEntry.resourceData.icon;
        // 플레이어 개인 창고(playerInventory) 표시 (시장 재고가 아님)
        _valueText.text = resourceEntry.resourceState.playerInventory.ToString("N0");
        UpdateChangeValue();
    }

    public void OnClick()
    {
        
    }

    private void UpdateChangeValue()
    {
        var resourceState = _resourceEntry?.resourceState;
        if (_changeValueText == null || resourceState == null)
        {
            return;
        }

        // 플레이어 재고 변화량 표시 (생산/소비/거래)
        long delta = resourceState.playerInventoryDelta;
        if (delta > 0)
        {
            _changeValueText.text = $"+{delta:N0}";
        }
        else if (delta < 0)
        {
            _changeValueText.text = delta.ToString("N0");
        }
        else
        {
            _changeValueText.text = "";
        }
    }
}
