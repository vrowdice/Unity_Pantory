using UnityEngine;

public class ManageThreadPlusBtn : MonoBehaviour
{
    private System.Action _onClickCallback = null;

    public void OnInitialize(System.Action onClickCallback)
    {
        _onClickCallback = onClickCallback;
    }

    public void OnClick()
    {
        if (_onClickCallback != null)
        {
            _onClickCallback();
        }
    }
}
