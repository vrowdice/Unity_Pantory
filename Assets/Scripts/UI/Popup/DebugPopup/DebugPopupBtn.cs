using UnityEngine;
using TMPro;
using System;

public class DebugPopupBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonText;
    private Action _onClickAction;

    public void Init(string text, Action onClickAction)
    {
        _onClickAction = onClickAction;
        if (buttonText != null)
            buttonText.text = text;
    }

    public void OnClick()
    {
        _onClickAction?.Invoke();
    }
}
