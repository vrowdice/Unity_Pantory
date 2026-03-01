using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class MainCanvas
{
    [Header("Thread")]
    [SerializeField] private Image _cancelPlacementBtnImage;
    [SerializeField] private Image _removalModeBtnImage;
    [SerializeField] private GameObject _threadCategoryBtnPrefab;
    [SerializeField] private Transform _threadCategoryScrollViewContent;
    [SerializeField] private GameObject _threadBtnPrefab;
    [SerializeField] private GameObject _threadPlusBtnPrefab;
    [SerializeField] private Transform _threadScrollViewContent;

    private readonly List<ThreadCategoryBtn> _threadCategoryBtns = new List<ThreadCategoryBtn>();
    private readonly List<ThreadBtn> _threadBtns = new List<ThreadBtn>();
    private string _selectedThreadCategoryId = string.Empty;

    private void RefreshThreadCategories()
    {
        GameObjectUtils.ClearChildren(_threadCategoryScrollViewContent);
        _threadCategoryBtns.Clear();

        if (_threadCategoryBtnPrefab == null)
        {
            return;
        }

        Dictionary<string, ThreadCategory> categories = DataManager.Thread.GetAllCategories();

        if (!string.IsNullOrEmpty(_selectedThreadCategoryId) && (categories == null || !categories.ContainsKey(_selectedThreadCategoryId)))
        {
            _selectedThreadCategoryId = string.Empty;
        }

        CreateThreadCategoryButton(string.Empty, LocalizationUtils.Localize("All"));

        if (categories != null)
        {
            foreach (ThreadCategory category in categories.Values)
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
        GameObjectUtils.ClearChildren(_threadScrollViewContent);
        _threadBtns.Clear();

        if (_threadBtnPrefab == null)
        {
            return;
        }

        List<ThreadState> threadsToShow;
        if (string.IsNullOrEmpty(_selectedThreadCategoryId))
        {
            threadsToShow = DataManager.Thread.GetAllThreadList();
        }
        else
        {
            threadsToShow = DataManager.Thread.GetThreadsInCategory(_selectedThreadCategoryId);
        }

        if (threadsToShow != null)
        {
            foreach (ThreadState thread in threadsToShow)
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

        UpdateThreadButtonStates();
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

    public void RegisterThreadTileManager(MainRunner threadTileManager)
    {
        _mainRunner = threadTileManager;
        UpdateThreadModeButtons(
            _mainRunner != null && _mainRunner.IsPlacementMode,
            _mainRunner != null && _mainRunner.IsRemovalMode);
    }

    public void StartThreadPlacement(ThreadState threadState)
    {
        if (threadState == null)
        {
            return;
        }

        if (_mainRunner == null)
        {
            Debug.LogWarning("[MainUiManager] ThreadTileManager is not assigned. Cannot start thread placement.");
            return;
        }

        if (_mainRunner.IsRemovalMode)
        {
            _mainRunner.CancelRemovalMode();
            UpdateThreadModeButtons(false, false);
        }

        if (_mainRunner.IsPlacementMode && _mainRunner.CurrentPlacementThread == threadState)
        {
            _mainRunner.CancelPlacementMode();
            UpdateThreadModeButtons(false, false);
            UpdateThreadButtonStates();
            return;
        }

        _mainRunner.StartPlacementMode(threadState);
        UpdateThreadModeButtons(true, false);
        UpdateThreadButtonStates();
    }

    public void CancelThreadPlacement()
    {
        if (_mainRunner == null)
        {
            return;
        }

        _mainRunner.CancelPlacementMode();
        UpdateThreadModeButtons(false, _mainRunner.IsRemovalMode);
        UpdateThreadButtonStates();
    }

    public void ToggleThreadRemovalMode()
    {
        if (_mainRunner == null)
        {
            Debug.LogWarning("[MainUiManager] ThreadTileManager is not assigned. Cannot toggle removal mode.");
            return;
        }

        _mainRunner.ToggleRemovalMode();
        UpdateThreadModeButtons(false, _mainRunner.IsRemovalMode);
        UpdateThreadButtonStates();
    }

    private void UpdateThreadCategoryButtonStates()
    {
        foreach (ThreadCategoryBtn btn in _threadCategoryBtns)
        {
            bool isActive = btn.CategoryId == _selectedThreadCategoryId;
            btn.SetFocused(isActive);
        }
    }

    private void UpdateThreadButtonStates()
    {
        ThreadState currentPlacementThread = null;
        if (_mainRunner != null && _mainRunner.IsPlacementMode)
        {
            currentPlacementThread = _mainRunner.CurrentPlacementThread;
        }

        foreach (ThreadBtn btn in _threadBtns)
        {
            if (btn == null)
                continue;

            ThreadState btnThreadState = btn.GetThreadState();
            bool isActive = (currentPlacementThread != null && btnThreadState != null &&
                           currentPlacementThread == btnThreadState);
            btn.SetFocused(isActive);
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
