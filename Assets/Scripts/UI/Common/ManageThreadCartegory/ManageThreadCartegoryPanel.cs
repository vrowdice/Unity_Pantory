using UnityEngine;
using System;

public class ManageThreadCartegoryPanel : MonoBehaviour
{
    [SerializeField] private Transform _contentTransform;
    [SerializeField] private ManageThreadCartegoryItemBtn _itemPanelPrefab;

    private dataManager _dataManager;
    private Action<string> _onCategorySelected;

    /// <summary>
    /// 패널을 초기화합니다.
    /// </summary>
    /// <param name="dataManager">GameDataManager</param>
    /// <param name="onCategorySelected">카테고리 선택 시 호출될 콜백 (옵션)</param>
    public void OnInitialize(dataManager dataManager, Action<string> onCategorySelected = null)
    {
        _dataManager = dataManager;
        _onCategorySelected = onCategorySelected;

        // 기존 아이템 제거
        if (_contentTransform != null)
        {
            GameObjectUtils.ClearChildren(_contentTransform);
        }

        // 카테고리 목록 표시
        RefreshCategoryList();
    }

    /// <summary>
    /// 카테고리 목록을 갱신합니다.
    /// </summary>
    private void RefreshCategoryList()
    {
        if (_dataManager == null || _contentTransform == null || _itemPanelPrefab == null)
            return;

        // 기존 아이템 제거
        GameObjectUtils.ClearChildren(_contentTransform);

        // 모든 카테고리 가져오기
        var categories = _dataManager.Thread.GetAllCategories();

        foreach (var category in categories.Values)
        {
            // 카테고리 아이템 생성
            ManageThreadCartegoryItemBtn itemPanel = Instantiate(_itemPanelPrefab, _contentTransform);
            
            if (itemPanel != null)
            {
                // 카테고리에 속한 스레드 개수
                int threadCount = category.ThreadCount;
                
                // 아이템 초기화
                itemPanel.OnInitialize(category.categoryId, category.categoryName, threadCount, this);
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

        // EnterNamePanel을 열어 카테고리 이름 입력받기
        gameManager.ShowEnterNamePanel((categoryName) =>
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                gameManager.ShowWarningPanel("Please enter a category name.");
                return;
            }

            // 카테고리 ID 생성 (중복 방지)
            string categoryId = $"Category_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

            // 카테고리 생성
            var newCategory = _dataManager.Thread.CreateCategory(categoryId, categoryName);
            
            if (newCategory != null)
            {
                Debug.Log($"[ManageThreadCartegoryPanel] Category created: {categoryName} ({categoryId})");
                
                // 카테고리 목록 갱신
                RefreshCategoryList();
                
                // 콜백 호출
                _onCategorySelected?.Invoke(categoryId);
            }
            else
            {
                gameManager.ShowWarningPanel("Failed to create category.");
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

        // EnterNamePanel을 열어 새 이름 입력받기
        gameManager.ShowEnterNamePanel((newName) =>
        {
            if (string.IsNullOrEmpty(newName))
            {
                gameManager.ShowWarningPanel("Please enter a category name.");
                return;
            }

            // 카테고리 이름 변경
            bool success = _dataManager.Thread.RenameCategory(categoryId, newName);
            
            if (success)
            {
                Debug.Log($"[ManageThreadCartegoryPanel] Category renamed: {categoryId} -> {newName}");
                
                // 카테고리 목록 갱신
                RefreshCategoryList();
                
                // 콜백 호출
                _onCategorySelected?.Invoke(categoryId);
            }
            else
            {
                gameManager.ShowWarningPanel("Failed to rename category.");
            }
        });
    }

    /// <summary>
    /// 아이템 패널에서 카테고리 삭제 요청 시 호출됩니다.
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

        // 확인 메시지 표시
        gameManager.ShowWarningPanel($"Delete category '{categoryName}'");
        
        // 카테고리 삭제
        bool success = _dataManager.Thread.RemoveCategory(categoryId);
        
        if (success)
        {
            Debug.Log($"[ManageThreadCartegoryPanel] Category deleted: {categoryName} ({categoryId})");
            
            // 카테고리 목록 갱신
            RefreshCategoryList();
        }
        else
        {
            gameManager.ShowWarningPanel("Failed to delete category.");
        }
    }

    /// <summary>
    /// 카테고리 아이템이 선택되었을 때 호출됩니다.
    /// </summary>
    /// <param name="categoryId">선택된 카테고리 ID</param>
    /// <param name="categoryName">선택된 카테고리 이름</param>
    public void OnCategorySelected(string categoryId, string categoryName)
    {
        Debug.Log($"[ManageThreadCartegoryPanel] Category selected: {categoryName} ({categoryId})");
        
        // 콜백 호출
        _onCategorySelected?.Invoke(categoryId);
        
        // 패널 닫기
        Destroy(gameObject);
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void OnClickClose()
    {
        Destroy(gameObject);
    }
}
