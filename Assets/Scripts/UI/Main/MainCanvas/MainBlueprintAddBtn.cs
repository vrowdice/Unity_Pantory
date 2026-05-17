using UnityEngine;
using UnityEngine.UI;

public class MainBlueprintAddBtn : MonoBehaviour
{
    [SerializeField] private Image _focusedImage = null;
    private MainCanvas _mainCanvas;
    private bool _isSelected;

    public void Init(MainCanvas mainCanvas)
    {
        _mainCanvas = mainCanvas;
    }

    public void OnClick()
    {
        _mainCanvas?.ToggleBlueprintMode();
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        if (_focusedImage != null)
            _focusedImage.gameObject.SetActive(isSelected);
    }
}