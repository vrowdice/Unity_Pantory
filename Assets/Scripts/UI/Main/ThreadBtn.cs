using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ThreadBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _text = null;
    private MainUiManager _mainUiManager = null;
    private ThreadState _threadState;

    public void Initialize(MainUiManager argMainUiManager, ThreadState argThreadState)
    {
        _mainUiManager = argMainUiManager;
        if (_text != null)
        {
            _text.text = string.IsNullOrEmpty(argThreadState.threadName) ? argThreadState.threadId : argThreadState.threadName;
        }

        if (_image != null)
        {
            if (!string.IsNullOrEmpty(argThreadState.previewImagePath))
            {
                Sprite sprite = SpriteUtils.LoadSpriteFromFile(argThreadState.previewImagePath);

                if (sprite != null)
                {
                    _image.sprite = sprite;
                    _image.enabled = true;
                }
                else
                {
                    _image.enabled = false;
                }
            }
            else
            {
                _image.enabled = false;
            }
        }

        _threadState = argThreadState;
    }

    public void OnClick()
    {
        
    }

}
