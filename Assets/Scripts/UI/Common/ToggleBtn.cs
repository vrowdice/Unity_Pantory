using UnityEngine;
using UnityEngine.UI;

public class ToggleBtn : BtnBase
{
    [Header("Target")]
    [SerializeField] private Image _targetImage;

    [Header("Panel (Optional)")]
    [SerializeField] private PanelDoAni _targetPanel;

    [Header("Sprites")]
    [SerializeField] private Sprite _closedSprite;
    [SerializeField] private Sprite _openedSprite;

    public bool IsOpened { get; private set; }

    private void Start()
    {
        if (_targetPanel != null)
        {
            if (_targetPanel.IsOpen)
            {
                SetOpened();
            }
            else
            {
                SetClosed();
            }
        }
        else
        {
            SetOpened();
        }
    }

    protected override void HandleClick()
    {
        if (_targetImage == null)
        {
            Debug.LogWarning("[ToggleBtn] Target Image is not assigned.");
            return;
        }

        if (IsOpened)
        {
            SetClosed();
        }
        else
        {
            SetOpened();
        }
    }

    public void SetOpened()
    {
        IsOpened = true;
        if (_targetImage != null && _openedSprite != null)
        {
            _targetImage.sprite = _openedSprite;
        }

        if (_targetPanel != null && !_targetPanel.IsOpen)
        {
            _targetPanel.OpenPanel();
        }
    }

    public void SetClosed()
    {
        IsOpened = false;
        if (_targetImage != null && _closedSprite != null)
        {
            _targetImage.sprite = _closedSprite;
        }

        if (_targetPanel != null && _targetPanel.IsOpen)
        {
            _targetPanel.ClosePanel();
        }
    }
}
