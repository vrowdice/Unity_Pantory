using UnityEngine;
using UnityEngine.UI;

public class ResourceImage : MonoBehaviour
{
    [SerializeField] private Image _iconImage;

    public void Init(ResourceEntry resourceEntry)
    {
        if (resourceEntry == null || resourceEntry.data == null)
            return;

        if (_iconImage == null)
            _iconImage = GetComponentInChildren<Image>(true);

        if (_iconImage != null && resourceEntry.data.icon != null)
            _iconImage.sprite = resourceEntry.data.icon;

        if (_iconImage != null)
            _iconImage.raycastTarget = false;
    }
}
