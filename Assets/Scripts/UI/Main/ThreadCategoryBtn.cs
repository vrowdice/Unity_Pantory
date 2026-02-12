using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThreadCategoryBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText = null;
    [SerializeField] private Image _focusedImage = null;

    private string _categoryId = string.Empty;
    private System.Action<ThreadCategoryBtn> _onClickCallback = null;

    public string CategoryId => _categoryId;

    public void Initialize(string categoryId, string categoryName, System.Action<ThreadCategoryBtn> onClickCallback)
    {
        _categoryId = categoryId ?? string.Empty;
        _onClickCallback = onClickCallback;

        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrEmpty(categoryName) ? LocalizationUtils.Localize("All") : categoryName;
        }
    }

    public void OnClick()
    {
        _onClickCallback?.Invoke(this);
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedImage != null)
        {
            _focusedImage.gameObject.SetActive(isFocused);
        }
    }
}
