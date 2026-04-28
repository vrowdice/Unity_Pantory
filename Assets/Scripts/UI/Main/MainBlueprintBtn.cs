using UnityEngine;
using UnityEngine.UI;

public class MainBlueprintBtn : MonoBehaviour
{
    [SerializeField] private Image _focusedImage = null;

    private MainCanvas _mainCanvas;
    
    public void Init(MainCanvas mainCanvas)
    {
        _mainCanvas = mainCanvas;
    }

    public void OnClick()
    {
        _mainCanvas?.OnMainBlueprintBtnClicked();
    }
}