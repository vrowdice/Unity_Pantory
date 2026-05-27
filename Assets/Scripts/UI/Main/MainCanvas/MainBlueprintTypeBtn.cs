using UnityEngine;
using UnityEngine.UI;

public class MainBlueprintTypeBtn : BtnBase
{
    [SerializeField] private Image _focusedImage = null;

    private MainCanvas _mainCanvas;
    
    public void Init(MainCanvas mainCanvas)
    {
        _mainCanvas = mainCanvas;
    }

    protected override void HandleClick()
    {
        _mainCanvas?.OnMainBlueprintBtnClicked();
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedImage != null) _focusedImage.gameObject.SetActive(isFocused);
    }
}
