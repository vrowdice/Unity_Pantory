using UnityEngine;
using System;

public class ManageThreadCartegoryPopup : BasePopup
{
    [SerializeField] private Transform _contentTransform;
    [SerializeField] private GameObject _cartegoryItemBtnPrefab;

    private DataManager _dataManager;
    private Action<string> _onCategorySelected;

    /// <summary>
    /// 패널을 초기화합니다.
    /// </summary>
    /// <param name="dataManager">GameDataManager</param>
    /// <param name="onCategorySelected">카테고리 선택 시 호출될 콜백 (옵션)</param>
    public void Init(DataManager dataManager, Action<string> onCategorySelected = null)
    {
        base.Init();
        
        _dataManager = dataManager;
        _onCategorySelected = onCategorySelected;

        GameObjectUtils.ClearChildren(_contentTransform);
        RefreshCategoryList();
        
        Show();
    }

    /// <summary>
    /// 카테고리 목록을 갱신합니다.
    /// </summary>
    private void RefreshCategoryList()
    {
        if (_dataManager == null || _contentTransform == null || _cartegoryItemBtnPrefab == null)
            return;

        GameObjectUtils.ClearChildren(_contentTransform);
        var categories = _dataManager.Thread.GetAllCategories();

        foreach (var category in categories.Values)
        {
            GameObject itemPanel = Instantiate(_cartegoryItemBtnPrefab, _contentTransform);
            ManageThreadCartegoryItemBtn manageThreadCartegoryItemBtn = itemPanel.GetComponent<ManageThreadCartegoryItemBtn>();

            if (itemPanel != null)
            {
                int threadCount = category.ThreadCount;
                manageThreadCartegoryItemBtn.Init(category.categoryId, category.categoryName, threadCount, this);
            }
        }
    }

    /// <summary>
    /// 카테고리 추가 버튼 클릭 시 호출됩니다.
    /// EnterNamePanel을 열어 새 카테고리 이름을 입력받습니다.
    /// </summary>
    public void OnClickAddCartegory()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[ManageThreadCartegoryPanel] GameManager is null.");
            return;
        }

        gameManager.ShowEnterNamePanel((categoryName) =>
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                gameManager.ShowWarningPanel(WarningMessage.PleaseEnterCategoryName);
                return;
            }

            string categoryId = $"Category_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            var newCategory = _dataManager.Thread.CreateCategory(categoryId, categoryName);
            
            if (newCategory != null)
            {
                Debug.Log($"[ManageThreadCartegoryPanel] Category created: {categoryName} ({categoryId})");
                RefreshCategoryList();
                _onCategorySelected?.Invoke(categoryId);
            }
            else
            {
                gameManager.ShowWarningPanel(WarningMessage.FailedToCreateCategory);
            }
        });
    }

    /// <summary>
    /// 아이템 패널에서 카테고리 이름 변경 요청 시 호출됩니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <param name="categoryName">현재 카테고리 이름</param>
    public void OnRequestRenameCategory(string categoryId, string categoryName)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[ManageThreadCartegoryPanel] GameManager is null.");
            return;
        }

        gameManager.ShowEnterNamePanel((newName) =>
        {
            if (string.IsNullOrEmpty(newName))
            {
                gameManager.ShowWarningPanel(WarningMessage.PleaseEnterCategoryName);
                return;
            }
            bool success = _dataManager.Thread.RenameCategory(categoryId, newName);
            
            if (success)
            {
                Debug.Log($"[ManageThreadCartegoryPanel] Category renamed: {categoryId} -> {newName}");
                RefreshCategoryList();
                _onCategorySelected?.Invoke(categoryId);
            }
            else
            {
                gameManager.ShowWarningPanel(WarningMessage.FailedToRenameCategory);
            }
        });
    }

    /// <summary>
    /// 아이템 패널에서 카테고리 삭제 요청 시 호출됩니다.
    /// 확인 팝업에서 확인 시에만 삭제합니다.
    /// </summary>
    /// <param name="categoryId">카테고리 ID</param>
    /// <param name="categoryName">카테고리 이름</param>
    public void OnRequestDeleteCategory(string categoryId, string categoryName)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[ManageThreadCartegoryPanel] GameManager is null.");
            return;
        }

        gameManager.ShowConfirmPanel(ConfirmMessage.DeleteCategoryConfirm, () =>
        {
            bool success = _dataManager.Thread.RemoveCategory(categoryId);

            if (success)
            {
                Debug.Log($"[ManageThreadCartegoryPanel] Category deleted: {categoryName} ({categoryId})");
                RefreshCategoryList();
            }
            else
            {
                gameManager.ShowWarningPanel(WarningMessage.FailedToDeleteCategory);
            }
        });
    }

    /// <summary>
    /// 카테고리 아이템이 선택되었을 때 호출됩니다.
    /// </summary>
    /// <param name="categoryId">선택된 카테고리 ID</param>
    /// <param name="categoryName">선택된 카테고리 이름</param>
    public void OnCategorySelected(string categoryId, string categoryName)
    {
        Debug.Log($"[ManageThreadCartegoryPanel] Category selected: {categoryName} ({categoryId})");
        _onCategorySelected?.Invoke(categoryId);
        Close();
        Destroy(gameObject);
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void OnClickClose()
    {
        Close();
        Destroy(gameObject);
    }
}
