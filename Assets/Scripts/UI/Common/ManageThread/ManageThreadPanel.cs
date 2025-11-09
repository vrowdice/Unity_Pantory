using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

/// <summary>
/// 스레드 목록(Thread List)을 관리하고 로드 기능을 제공하는 패널입니다.
/// 카테고리별 필터링, 스레드 선택, 생성 및 삭제 기능을 포함합니다.
/// </summary>
public class ManageThreadPanel : MonoBehaviour
{
    [SerializeField] private GameObject _threadCategoryBtnPrefab = null;
    [SerializeField] private GameObject _threadSelectBtnPrefab = null;
    [SerializeField] private GameObject _threadPlusBtnPrefab = null;

    [SerializeField] private Transform _threadCategoryScrollViewContent = null;
    [SerializeField] private Transform _threadScrollViewContent = null;

    private GameDataManager _dataManager = null;
    private string _selectedCategoryId = string.Empty; // 현재 선택된 카테고리 ID (Empty = All)
    private System.Action<string> _onThreadSelectedCallback = null; // 스레드 선택 시 콜백

    // 생성된 버튼들 추적
    private List<ManageThreadCategoryBtn> _categoryBtns = new List<ManageThreadCategoryBtn>();
    private List<ManageThreadBtn> _threadBtns = new List<ManageThreadBtn>();

    /// <summary>
    /// 패널을 초기화하고 필요한 데이터와 콜백을 설정합니다.
    /// </summary>
    public void OnInitialize(GameDataManager dataManager, System.Action<string> onThreadSelected = null)
    {
        if (dataManager == null)
        {
            Debug.LogError("[ManageThreadPanel] GameDataManager is null.");
            return;
        }

        _dataManager = dataManager;
        _onThreadSelectedCallback = onThreadSelected;

        // 초기 상태를 "All" 카테고리로 설정
        _selectedCategoryId = string.Empty;

        // 이벤트 구독
        _dataManager.OnCategoryChanged += RefreshCategoryList;
        _dataManager.OnThreadChanged += RefreshThreadList;

        // 초기 표시
        RefreshCategoryList();
        RefreshThreadList();
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_dataManager != null)
        {
            _dataManager.OnCategoryChanged -= RefreshCategoryList;
            _dataManager.OnThreadChanged -= RefreshThreadList;
        }
    }

    /// <summary>
    /// 카테고리 목록을 새로고침하고 UI를 업데이트합니다.
    /// </summary>
    private void RefreshCategoryList()
    {
        if (_dataManager == null || _threadCategoryScrollViewContent == null)
            return;

        // 기존 버튼 제거 및 리스트 클리어
        GameObjectUtils.ClearChildren(_threadCategoryScrollViewContent);
        _categoryBtns.Clear();

        // 1. "All" 카테고리 버튼 추가 (categoryId = string.Empty)
        if (_threadCategoryBtnPrefab != null)
        {
            GameObject allBtnObj = Instantiate(_threadCategoryBtnPrefab, _threadCategoryScrollViewContent);
            ManageThreadCategoryBtn allBtn = allBtnObj.GetComponent<ManageThreadCategoryBtn>();

            if (allBtn != null)
            {
                allBtn.OnInitialize(string.Empty, "All", OnCategorySelected);
                _categoryBtns.Add(allBtn);
            }
        }

        // 2. 등록된 모든 카테고리 버튼 추가
        var categories = _dataManager.GetAllCategories();

        foreach (var category in categories.Values)
        {
            if (category == null || string.IsNullOrEmpty(category.categoryId))
                continue;

            if (_threadCategoryBtnPrefab != null)
            {
                GameObject btnObj = Instantiate(_threadCategoryBtnPrefab, _threadCategoryScrollViewContent);
                ManageThreadCategoryBtn btn = btnObj.GetComponent<ManageThreadCategoryBtn>();

                if (btn != null)
                {
                    btn.OnInitialize(category.categoryId, category.categoryName, OnCategorySelected);
                    _categoryBtns.Add(btn);
                }
            }
        }

        // 3. 현재 선택된 카테고리 버튼의 활성화 상태 업데이트
        // RefreshCategoryList가 호출된 후에도 기존 선택이 유지되도록 처리
        UpdateCategoryButtonStates(_selectedCategoryId);
    }

    /// <summary>
    /// 스레드 목록을 새로고침하고 UI를 업데이트합니다. (선택된 카테고리 기준으로 필터링)
    /// </summary>
    private void RefreshThreadList()
    {
        if (_dataManager == null || _threadScrollViewContent == null)
            return;

        // 기존 버튼 제거 및 리스트 클리어
        GameObjectUtils.ClearChildren(_threadScrollViewContent);
        _threadBtns.Clear();

        List<ThreadState> threadsToShow;

        if (string.IsNullOrEmpty(_selectedCategoryId))
        {
            // "All" 선택 시: 모든 스레드 표시
            threadsToShow = _dataManager.GetAllThreadList();
        }
        else
        {
            // 특정 카테고리 선택 시: 해당 카테고리에 속한 스레드만 표시
            threadsToShow = _dataManager.GetThreadsInCategory(_selectedCategoryId);
        }

        // 1. 스레드 버튼 생성
        foreach (var thread in threadsToShow)
        {
            if (thread == null || string.IsNullOrEmpty(thread.threadId))
                continue;

            if (_threadSelectBtnPrefab != null)
            {
                GameObject btnObj = Instantiate(_threadSelectBtnPrefab, _threadScrollViewContent);
                ManageThreadBtn btn = btnObj.GetComponent<ManageThreadBtn>();

                if (btn != null)
                {
                    // 미리보기 이미지 로드
                    Sprite previewSprite = LoadPreviewImage(thread.previewImagePath);
                    btn.OnInitialize(thread, previewSprite, OnThreadClick, OnThreadEdit, OnThreadDelete);
                    _threadBtns.Add(btn);
                }
            }
        }

        // 2. (+) Plus 버튼을 목록의 가장 마지막에 추가
        if (_threadPlusBtnPrefab != null)
        {
            GameObject plusBtnObj = Instantiate(_threadPlusBtnPrefab, _threadScrollViewContent);
            ManageThreadPlusBtn plusBtn = plusBtnObj.GetComponent<ManageThreadPlusBtn>();
            if (plusBtn != null)
            {
                plusBtn.OnInitialize(OnPlusBtnClick);
            }
        }
    }

    /// <summary>
    /// 현재 선택된 카테고리 ID를 기반으로 카테고리 버튼의 활성화 상태를 업데이트합니다.
    /// </summary>
    private void UpdateCategoryButtonStates(string activeCategoryId)
    {
        foreach (var btn in _categoryBtns)
        {
            bool isActive = btn.CategoryId == activeCategoryId;
            
            Image button = btn.GetComponent<Image>();
            if (button != null)
            {
                button.color = isActive ? Color.yellow : Color.white;
            }
        }
    }


    /// <summary>
    /// 저장된 이미지 파일 경로로부터 Sprite를 로드합니다.
    /// </summary>
    private Sprite LoadPreviewImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return null;

        try
        {
            byte[] imageData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            // Texture2D는 Sprite에 묶이므로, 바로 파괴하지 않고 반환합니다.
            return sprite;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ManageThreadPanel] Failed to load preview image: {e.Message}");
            return null;
        }
    }

    // ================== 버튼 클릭 핸들러 ==================

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

        // 콜백 호출: 선택된 스레드 ID를 DesignUiManager에 전달
        _onThreadSelectedCallback?.Invoke(threadId);

        // 패널 닫기 및 제거
        ClosePanel();
    }

    /// <summary>
    /// 스레드 편집 버튼 클릭 시 호출됩니다. (TODO: 편집 기능)
    /// </summary>
    private void OnThreadEdit(string threadId)
    {
        Debug.Log($"[ManageThreadPanel] Thread edit requested: {threadId}");
        // TODO: 편집 UI를 열거나 해당 스레드를 로드하여 Design Mode로 전환
    }

    /// <summary>
    /// 스레드 삭제 버튼 클릭 시 호출됩니다.
    /// </summary>
    private void OnThreadDelete(string threadId)
    {
        if (_dataManager == null) return;

        ThreadState thread = _dataManager.GetThread(threadId);
        string threadName = thread != null ? thread.threadName : threadId;

        // 스레드 삭제 (ThreadDataHandler 내부에서 저장 자동 트리거)
        bool removed = _dataManager.RemoveThread(threadId);

        if (removed)
        {
            // 미리보기 이미지 파일도 삭제 (선택 사항)
            if (thread != null && File.Exists(thread.previewImagePath))
            {
                File.Delete(thread.previewImagePath);
                Debug.Log($"[ManageThreadPanel] Deleted preview file: {thread.previewImagePath}");
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowWarningPanel($"Thread '{threadName}' has been deleted.");
            }

            // UI는 이벤트(OnThreadChanged)에 의해 자동 갱신됩니다.
            Debug.Log($"[ManageThreadPanel] Thread deleted: {threadId}");
        }
        else
        {
            Debug.LogWarning($"[ManageThreadPanel] Failed to delete thread: {threadId}");
        }
    }

    /// <summary>
    /// Plus 버튼 클릭 시 호출됩니다. 새 스레드 생성 프로세스를 시작합니다.
    /// </summary>
    private void OnPlusBtnClick()
    {
        if (GameManager.Instance == null || _dataManager == null) return;

        // 새 스레드 이름 입력 패널 표시
        GameManager.Instance.ShowEnterNamePanel((threadName) =>
        {
            if (string.IsNullOrEmpty(threadName)) return;

            // 스레드 ID 생성 (이름 기반)
            string baseId = $"thread_{threadName.Replace(" ", "_").ToLower()}";
            string threadId = baseId;
            int suffix = 0;

            // 중복 확인 및 처리
            while (_dataManager.HasThread(threadId))
            {
                suffix++;
                threadId = $"{baseId}_{suffix}";
            }

            // 새 스레드 생성 (자동 저장/이벤트 트리거)
            _dataManager.CreateThread(threadId, threadName);

            // 선택된 카테고리가 있으면 새 스레드를 카테고리에 추가
            if (!string.IsNullOrEmpty(_selectedCategoryId))
            {
                _dataManager.AddThreadToCategory(_selectedCategoryId, threadId);
            }

            Debug.Log($"[ManageThreadPanel] New thread created: {threadId} ({threadName})");
        });
    }

    /// <summary>
    /// 패널을 닫고 GameObject를 파괴합니다.
    /// </summary>
    public void ClosePanel()
    {
        Destroy(gameObject);
    }
}