using UnityEngine;
using UnityEngine.UI;

public class ToggleBtn : MonoBehaviour
{
    [SerializeField] private Image _targetImage;
    [SerializeField] private Sprite _firstSprite;
    [SerializeField] private Sprite _secondSprite;

    private void Start()
    {
        _targetImage.sprite = _firstSprite;
    }

    public void ToggleImage()
    {
        if (_targetImage == null)
        {
            Debug.LogWarning("[ToggleBtn] Target Image is not assigned.");
            return;
        }
        if (_targetImage.sprite == _firstSprite)
        {
            _targetImage.sprite = _secondSprite;
        }
        else
        {
            _targetImage.sprite = _firstSprite;
        }
    }
}
