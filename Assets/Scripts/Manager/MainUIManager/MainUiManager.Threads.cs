using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class MainUiManager
{
    private void RefreshThreadCategories()
    {
        if (_dataManager == null || _threadCategoryScrollViewContent == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_threadCategoryScrollViewContent);
        _threadCategoryBtns.Clear();

        if (_threadCategoryBtnPrefab == null)
        {
            return;
        }

        var categories = _dataManager.Thread.GetAllCategories();

        if (!string.IsNullOrEmpty(_selectedThreadCategoryId) && (categories == null || !categories.ContainsKey(_selectedThreadCategoryId)))
        {
            _selectedThreadCategoryId = string.Empty;
        }

        CreateThreadCategoryButton(string.Empty, "All");

        if (categories != null)
        {
            foreach (var category in categories.Values)
            {
                if (category == null || string.IsNullOrEmpty(category.categoryId))
                {
                    continue;
                }

                CreateThreadCategoryButton(category.categoryId, category.categoryName);
            }
        }

        UpdateThreadCategoryButtonStates();
    }

    private void CreateThreadCategoryButton(string categoryId, string categoryName)
    {
        GameObject btnObj = Instantiate(_threadCategoryBtnPrefab, _threadCategoryScrollViewContent);
        ThreadCategoryBtn categoryBtn = btnObj.GetComponent<ThreadCategoryBtn>();
        if (categoryBtn != null)
        {
            categoryBtn.Initialize(categoryId, categoryName, HandleThreadCategoryButtonClicked);
            _threadCategoryBtns.Add(categoryBtn);
        }
    }

    private void RefreshThreadButtons()
    {
        if (_dataManager == null || _threadScrollViewContent == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_threadScrollViewContent);
        _threadBtns.Clear();

        if (_threadBtnPrefab == null)
        {
            return;
        }

        List<ThreadState> threadsToShow;
        if (string.IsNullOrEmpty(_selectedThreadCategoryId))
        {
            threadsToShow = _dataManager.Thread.GetAllThreadList();
        }
        else
        {
            threadsToShow = _dataManager.Thread.GetThreadsInCategory(_selectedThreadCategoryId);
        }

        if (threadsToShow != null)
        {
            foreach (var thread in threadsToShow)
            {
                if (thread == null || string.IsNullOrEmpty(thread.threadId))
                {
                    continue;
                }

                GameObject btnObj = Instantiate(_threadBtnPrefab, _threadScrollViewContent);
                ThreadBtn threadBtn = btnObj.GetComponent<ThreadBtn>();
                if (threadBtn != null)
                {
                    threadBtn.Initialize(this, thread);
                    _threadBtns.Add(threadBtn);
                }
            }
        }

        if (_threadPlusBtnPrefab != null)
        {
            Instantiate(_threadPlusBtnPrefab, _threadScrollViewContent);
        }
    }

    private void HandleThreadCategoryButtonClicked(ThreadCategoryBtn btn)
    {
        if (btn == null)
        {
            return;
        }

        _selectedThreadCategoryId = btn.CategoryId ?? string.Empty;
        UpdateThreadCategoryButtonStates();
        RefreshThreadButtons();
    }

    public void RegisterThreadTileManager(ThreadTileManager threadTileManager)
    {
        _threadTileManager = threadTileManager;
        UpdateThreadModeButtons(
            _threadTileManager != null && _threadTileManager.IsPlacementMode,
            _threadTileManager != null && _threadTileManager.IsRemovalMode);
    }

    public void StartThreadPlacement(ThreadState threadState)
    {
        if (threadState == null)
        {
            return;
        }

        if (_threadTileManager == null)
        {
            Debug.LogWarning("[MainUiManager] ThreadTileManager is not assigned. Cannot start thread placement.");
            return;
        }

        if (_threadTileManager.IsRemovalMode)
        {
            _threadTileManager.CancelRemovalMode();
            UpdateThreadModeButtons(false, false);
        }

        if (_threadTileManager.IsPlacementMode && _threadTileManager.CurrentPlacementThread == threadState)
        {
            _threadTileManager.CancelPlacementMode();
            UpdateThreadModeButtons(false, false);
            return;
        }

        _threadTileManager.StartPlacementMode(threadState);
        UpdateThreadModeButtons(true, false);
    }

    public void CancelThreadPlacement()
    {
        if (_threadTileManager == null)
        {
            return;
        }

        _threadTileManager.CancelPlacementMode();
        UpdateThreadModeButtons(false, _threadTileManager.IsRemovalMode);
    }

    public void StartThreadRemovalMode()
    {
        if (_threadTileManager == null)
        {
            Debug.LogWarning("[MainUiManager] ThreadTileManager is not assigned. Cannot start removal mode.");
            return;
        }

        _threadTileManager.StartRemovalMode();
        UpdateThreadModeButtons(_threadTileManager.IsPlacementMode, true);
    }

    public void CancelThreadRemovalMode()
    {
        if (_threadTileManager == null)
        {
            Debug.LogWarning("[MainUiManager] ThreadTileManager is not assigned. Cannot cancel removal mode.");
            return;
        }

        _threadTileManager.CancelRemovalMode();
        UpdateThreadModeButtons(_threadTileManager.IsPlacementMode, false);
    }

    public void ToggleThreadRemovalMode()
    {
        if (_threadTileManager == null)
        {
            Debug.LogWarning("[MainUiManager] ThreadTileManager is not assigned. Cannot toggle removal mode.");
            return;
        }

        _threadTileManager.ToggleRemovalMode();
        UpdateThreadModeButtons(_threadTileManager.IsPlacementMode, _threadTileManager.IsRemovalMode);
    }

    private void UpdateThreadCategoryButtonStates()
    {
        foreach (var btn in _threadCategoryBtns)
        {
            bool isActive = btn.CategoryId == _selectedThreadCategoryId;

            Image image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = isActive ? Color.yellow : Color.white;
            }
        }
    }

    private void UpdateThreadModeButtons(bool isPlacementMode, bool isRemovalMode)
    {
        VisualManager visualManager = VisualManager.Instance;

        if (_cancelPlacementBtnImage != null)
        {
            _cancelPlacementBtnImage.color = (isPlacementMode && visualManager != null)
                ? visualManager.ValidColor
                : Color.white;
        }

        if (_removalModeBtnImage != null)
        {
            _removalModeBtnImage.color = (isRemovalMode && visualManager != null)
                ? visualManager.InvalidColor
                : Color.white;
        }
    }
}
