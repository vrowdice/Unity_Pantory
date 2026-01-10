using UnityEngine;
using TMPro;

public class ManageThreadCategoryBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText = null;

    private string _categoryId = string.Empty;
    private System.Action<string> _onClickCallback = null;

    public string CategoryId => _categoryId;

    public void Init(string categoryId, string categoryName, System.Action<string> onClickCallback)
    {
        _categoryId = categoryId;
        _onClickCallback = onClickCallback;

        if (_titleText != null)
        {
            _titleText.text = categoryName;
        }
    }

    public void OnClick()
    {
        if (_onClickCallback != null)
        {
            _onClickCallback(_categoryId);
        }
    }
}
