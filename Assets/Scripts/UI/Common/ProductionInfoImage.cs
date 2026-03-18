using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
public class ProductionInfoImage : MonoBehaviour
{
    [SerializeField] private Image _productionImage;
    [SerializeField] private TextMeshProUGUI _productionText;
    [SerializeField] private TextMeshProUGUI _productionCountText;

    public void Init(ResourceEntry resourceEntry)
    {
        Init(resourceEntry, -1);
    }

    public void Init(ResourceEntry resourceEntry, int amount)
    {
        _productionImage.sprite = resourceEntry.data.icon;
        _productionText.text = resourceEntry.data.id.Localize(LocalizationUtils.TABLE_RESOURCE);
        SetRaycastTargets(false);

        if (amount >= 0)
        {
            _productionCountText.text = $"{amount}";
        }
        else
        {
            _productionCountText.text = $"-";
        }
    }

    private void SetRaycastTargets(bool enabled)
    {
        if (_productionImage != null)
        {
            _productionImage.raycastTarget = enabled;
        }

        if (_productionText != null)
        {
            _productionText.raycastTarget = enabled;
        }

        if (_productionCountText != null)
        {
            _productionCountText.raycastTarget = enabled;
        }
    }
}
