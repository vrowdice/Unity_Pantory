using TMPro;
using UnityEngine;
using System;

public class DebugPopupBtn : BtnBase
{
    [SerializeField] private TextMeshProUGUI buttonText;
    private Action _onClickAction;

    public void Init(string text, Action onClickAction)
    {
        _onClickAction = onClickAction;
        if (buttonText != null)
            buttonText.text = text;
    }

    protected override void HandleClick()
    {
        _onClickAction?.Invoke();
    }
}
