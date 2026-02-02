using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManageThreadCategoryBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText = null;
    [SerializeField] private Image _focusedImage = null;

    private string _categoryId = string.Empty;
    private System.Action<string> _onClickCallback = null;

    public string CategoryId => _categoryId;

    public void Init(string categoryId, string categoryName, System.Action<string> onClickCallback)
    {
        _categoryId = categoryId;
        _onClickCallback = onClickCallback;
        _titleText.text = categoryName;

        SetFocused(false);
    }

    public void OnClick()
    {
        if (_onClickCallback != null)
        {
            _onClickCallback(_categoryId);
        }
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedImage != null)
        {
            _focusedImage.gameObject.SetActive(isFocused);
        }
    }
}
