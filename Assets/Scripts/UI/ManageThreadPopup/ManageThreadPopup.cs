using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

/// <summary>
/// 스레드 목록(Thread List)을 관리하고 로드 기능을 제공하는 패널입니다.
/// 카테고리별 필터링, 스레드 선택, 생성 및 삭제 기능을 포함합니다.
/// </summary>
public class ManageThreadPopup : PopupBase
{
    [SerializeField] private GameObject _threadCategoryBtnPrefab = null;
    [SerializeField] private GameObject _threadSelectBtnPrefab = null;
    [SerializeField] private GameObject _threadPlusBtnPrefab = null;

    [SerializeField] private Transform _threadCategoryScrollViewContent = null;
    [SerializeField] private Transform _threadScrollViewContent = null;

    private DataManager _dataManager = null;
    private string _selectedCategoryId = string.Empty;
    private System.Action<string> _onThreadSelectedCallback = null;

    private List<ManageThreadCategoryBtn> _categoryBtns = new List<ManageThreadCategoryBtn>();
    private List<ManageThreadBtn> _threadBtns = new List<ManageThreadBtn>();

    /// <summary>
    /// 패널을 초기화하고 필요한 데이터와 콜백을 설정합니다.
    /// </summary>
    public void Init(System.Action<string> onThreadSelected = null)
    {
        base.Init();

        DataManager dataManager = DataManager.Instance;

        if (dataManager == null)
        {
            Debug.LogError("[ManageThreadPanel] GameDataManager is null.");
            return;
        }

        _dataManager = dataManager;
        _onThreadSelectedCallback = onThreadSelected;

        _selectedCategoryId = string.Empty;

        _dataManager.Thread.OnCategoryChanged += RefreshCategoryList;
        _dataManager.Thread.OnThreadChanged += RefreshThreadList;

        RefreshCategoryList();
        RefreshThreadList();
        
        Show();
    }

    void OnDestroy()
    {
        if (_dataManager != null)
        {
            _dataManager.Thread.OnCategoryChanged -= RefreshCategoryList;
            _dataManager.Thread.OnThreadChanged -= RefreshThreadList;
        }
    }

    /// <summary>
    /// 카테고리 목록을 새로고침하고 UI를 업데이트합니다.
    /// </summary>
    private void RefreshCategoryList()
    {
        if (_dataManager == null || _threadCategoryScrollViewContent == null)
            return;

        GameObjectUtils.ClearChildren(_threadCategoryScrollViewContent);
        _categoryBtns.Clear();

        if (_threadCategoryBtnPrefab != null)
        {
            GameObject allBtnObj = Instantiate(_threadCategoryBtnPrefab, _threadCategoryScrollViewContent);
            ManageThreadCategoryBtn allBtn = allBtnObj.GetComponent<ManageThreadCategoryBtn>();

            if (allBtn != null)
            {
                allBtn.Init(string.Empty, LocalizationUtils.Localize("All"), OnCategorySelected);
                _categoryBtns.Add(allBtn);
            }
        }

        Dictionary<string, ThreadCategory> categories = _dataManager.Thread.GetAllCategories();

        foreach (ThreadCategory category in categories.Values)
        {
            if (category == null || string.IsNullOrEmpty(category.categoryId))
                continue;

            if (_threadCategoryBtnPrefab != null)
            {
                GameObject btnObj = Instantiate(_threadCategoryBtnPrefab, _threadCategoryScrollViewContent);
                ManageThreadCategoryBtn btn = btnObj.GetComponent<ManageThreadCategoryBtn>();

                if (btn != null)
                {
                    btn.Init(category.categoryId, category.categoryName, OnCategorySelected);
                    _categoryBtns.Add(btn);
                }
            }
        }

        UpdateCategoryButtonStates(_selectedCategoryId);
    }

    /// <summary>
    /// 스레드 목록을 새로고침하고 UI를 업데이트합니다.
    /// </summary>
    private void RefreshThreadList()
    {
        if (_dataManager == null || _threadScrollViewContent == null)
            return;

        GameObjectUtils.ClearChildren(_threadScrollViewContent);
        _threadBtns.Clear();

        List<ThreadState> threadsToShow;

        if (string.IsNullOrEmpty(_selectedCategoryId))
        {
            threadsToShow = _dataManager.Thread.GetAllThreadList();
        }
        else
        {
            threadsToShow = _dataManager.Thread.GetThreadsInCategory(_selectedCategoryId);
        }

        foreach (ThreadState thread in threadsToShow)
        {
            if (thread == null || string.IsNullOrEmpty(thread.threadId))
                continue;

            if (_threadSelectBtnPrefab != null)
            {
                GameObject btnObj = Instantiate(_threadSelectBtnPrefab, _threadScrollViewContent);
                ManageThreadBtn btn = btnObj.GetComponent<ManageThreadBtn>();

                if (btn != null)
                {
                    Sprite previewSprite = SpriteUtils.LoadSpriteFromFile(thread.previewImagePath);
                    btn.Init(thread, previewSprite, OnThreadClick, OnThreadEdit, OnThreadDelete);
                    _threadBtns.Add(btn);
                }
            }
        }

        if (_threadPlusBtnPrefab != null)
        {
            GameObject plusBtnObj = Instantiate(_threadPlusBtnPrefab, _threadScrollViewContent);
            ThreadPlusBtn plusBtn = plusBtnObj.GetComponent<ThreadPlusBtn>();
        }
    }

    /// <summary>
    /// 현재 선택된 카테고리 ID를 기반으로 카테고리 버튼의 활성화 상태를 업데이트합니다.
    /// </summary>
    private void UpdateCategoryButtonStates(string activeCategoryId)
    {
        foreach (ManageThreadCategoryBtn btn in _categoryBtns)
        {
            bool isActive = btn.CategoryId == activeCategoryId;
            btn.SetFocused(isActive);
        }
    }

    /// <summary>
    /// 카테고리 선택 시 호출됩니다. 선택된 카테고리로 스레드 목록을 필터링합니다.
    /// </summary>
    private void OnCategorySelected(string categoryId)
    {
        _selectedCategoryId = categoryId;
        UpdateCategoryButtonStates(categoryId);
        RefreshThreadList();
    }

    /// <summary>
    /// 스레드 버튼 클릭 시 호출됩니다. 선택된 스레드를 로드하고 패널을 닫습니다.
    /// </summary>
    private void OnThreadClick(string threadId)
    {
        if (_dataManager == null) return;

        Debug.Log($"[ManageThreadPanel] Thread selected: {threadId}");

        _onThreadSelectedCallback?.Invoke(threadId);
        ClosePanel();
    }

    /// <summary>
    /// 스레드 편집 버튼 클릭 시 호출됩니다. (TODO: 편집 기능)
    /// </summary>
    private void OnThreadEdit(string threadId)
    {
        Debug.Log($"[ManageThreadPanel] Thread edit requested: {threadId}");
    }

    /// <summary>
    /// 스레드 삭제 버튼 클릭 시 호출됩니다.
    /// </summary>
    private void OnThreadDelete(string threadId)
    {
        if (_dataManager == null) return;

        ThreadState thread = _dataManager.Thread.GetThread(threadId);
        string threadName = thread != null ? thread.threadName : threadId;

        bool removed = _dataManager.Thread.RemoveThread(threadId);

        if (removed)
        {
            if (thread != null && File.Exists(thread.previewImagePath))
            {
                SpriteUtils.UnloadSprite(thread.previewImagePath);
                File.Delete(thread.previewImagePath);
                Debug.Log($"[ManageThreadPanel] Deleted preview file: {thread.previewImagePath}");
            }

            if (GameManager.Instance != null)
            {
                UIManager.Instance.ShowWarningPopup(WarningMessage.ThreadDeleted);
            }
        }
        else
        {
            Debug.LogWarning($"[ManageThreadPanel] Failed to delete thread: {threadId}");
        }
    }

    /// <summary>
    /// 패널을 닫고 GameObject를 파괴합니다.
    /// </summary>
    public void ClosePanel()
    {
        Close();
        Destroy(gameObject);
    }
}