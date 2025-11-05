using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManageThreadCartegoryItemBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _countText;

    private string _categoryId = string.Empty;
    private string _categoryName = string.Empty;
    private ManageThreadCartegoryPanel _parentPanel;

    /// <summary>
    /// 카테고리 아이템 패널을 초기화합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <param name="categoryName">카테고리 이름</param>
    /// <param name="count">속한 스레드 개수</param>
    /// <param name="parentPanel">부모 패널 (ManageThreadCartegoryPanel)</param>
    public void OnInitialize(string categoryId, string categoryName, int count, ManageThreadCartegoryPanel parentPanel)
    {
        _categoryId = categoryId;
        _categoryName = categoryName;
        _parentPanel = parentPanel;

        if (_titleText != null)
        {
            _titleText.text = categoryName;
        }

        if (_countText != null)
        {
            _countText.text = count.ToString();
        }
    }

    /// <summary>
    /// 카테고리 아이템 클릭 시 호출됩니다.
    /// 부모 패널에 선택을 알립니다.
    /// </summary>
    public void OnClick()
    {
        if (_parentPanel != null)
        {
            _parentPanel.OnCategorySelected(_categoryId, _categoryName);
        }
        else
        {
            Debug.LogWarning("[ManageThreadCartegoryItemBtn] Parent panel is null.");
        }
    }

    /// <summary>
    /// 이름 변경 버튼 클릭 시 호출됩니다.
    /// 부모 패널에 처리를 위임합니다.
    /// </summary>
    public void OnClickRename()
    {
        if (_parentPanel != null)
        {
            _parentPanel.OnRequestRenameCategory(_categoryId, _categoryName);
        }
        else
        {
            Debug.LogWarning("[ManageThreadCartegoryItemPanel] Parent panel is null.");
        }
    }

    /// <summary>
    /// 삭제 버튼 클릭 시 호출됩니다.
    /// 부모 패널에 처리를 위임합니다.
    /// </summary>
    public void OnClickDelete()
    {
        if (_parentPanel != null)
        {
            _parentPanel.OnRequestDeleteCategory(_categoryId, _categoryName);
        }
        else
        {
            Debug.LogWarning("[ManageThreadCartegoryItemPanel] Parent panel is null.");
        }
    }
}
