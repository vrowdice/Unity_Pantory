using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageResourceBtn : MonoBehaviour
{
    [SerializeField] private Image _resourceIconImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceCountText;

    private StoragePanel _storagePanel;
    private ResourceEntry _resourceEntry;

    public void OnInitialize(StoragePanel storagePanel, ResourceEntry resourceEntry)
    {
        _storagePanel = storagePanel;
        _resourceEntry = resourceEntry;

        _resourceIconImage.sprite = _resourceEntry.resourceData.icon;
        _resourceNameText.text = _resourceEntry.resourceData.displayName;
        // 플레이어 개인 창고(playerInventory) 표시 (시장 재고가 아님)
        _resourceCountText.text = _resourceEntry.resourceState.playerInventory.ToString("N0");
    }

    public void OnClick()
    {
        Debug.Log("OnClick");
    }
}
