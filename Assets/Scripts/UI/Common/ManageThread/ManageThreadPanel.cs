using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ManageThreadPanel : MonoBehaviour
{
    [SerializeField] private GameObject _threadCategoryBtnPrefab = null;
    [SerializeField] private GameObject _threadSelectBtnPrefab = null;
    [SerializeField] private GameObject _threadPlusBtnPrefab = null;

    [SerializeField] private Transform _threadCategoryScrollViewContent = null;
    [SerializeField] private Transform _threadScrollViewContent = null;

    private GameDataManager _dataManager = null;
    private string _selectedCategoryId = string.Empty;
    private System.Action<string> _onThreadSelectedCallback = null; // 스레드 선택 시 콜백

    // 생성된 버튼들 추적
    private List<ManageThreadCategoryBtn> _categoryBtns = new List<ManageThreadCategoryBtn>();
    private List<ManageThreadBtn> _threadBtns = new List<ManageThreadBtn>();

    public void OnInitialize(GameDataManager dataManager, System.Action<string> onThreadSelected = null)
    {
        if (dataManager == null)
        {
            Debug.LogError("[ManageThreadPanel] GameDataManager is null.");
            return;
        }

        _dataManager = dataManager;
        _onThreadSelectedCallback = onThreadSelected;

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
    /// 카테고리 목록을 새로고침합니다.
    /// </summary>
    private void RefreshCategoryList()
    {
        if (_dataManager == null || _threadCategoryScrollViewContent == null)
            return;

        // 기존 버튼 제거
        GameObjectUtils.ClearChildren(_threadCategoryScrollViewContent);
        _categoryBtns.Clear();

        // "All" 카테고리 버튼 먼저 추가
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

        // 모든 카테고리 가져오기
        var categories = _dataManager.GetAllCategories();
        
        foreach (var category in categories.Values)
        {
            if (category == null || string.IsNullOrEmpty(category.categoryId))
                continue;

            // 카테고리 버튼 생성
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
    }

    /// <summary>
    /// 스레드 목록을 새로고침합니다.
    /// </summary>
    private void RefreshThreadList()
    {
        if (_dataManager == null || _threadScrollViewContent == null)
            return;

        // 기존 버튼 제거
        GameObjectUtils.ClearChildren(_threadScrollViewContent);
        _threadBtns.Clear();

        List<ThreadState> threadsToShow = new List<ThreadState>();
        
        if (string.IsNullOrEmpty(_selectedCategoryId))
        {
            // 카테고리가 선택되지 않았으면 모든 스레드 표시
            threadsToShow = _dataManager.GetAllThreadList();
        }
        else
        {
            // 선택된 카테고리에 속한 스레드만 표시
            threadsToShow = _dataManager.GetThreadsInCategory(_selectedCategoryId);
        }

        Debug.Log(threadsToShow.Count);

/*        // Plus 버튼 추가
        if (_threadPlusBtnPrefab != null)
        {
            GameObject plusBtnObj = Instantiate(_threadPlusBtnPrefab, _threadScrollViewContent);
            ManageThreadPlusBtn plusBtn = plusBtnObj.GetComponent<ManageThreadPlusBtn>();
            if (plusBtn != null)
            {
                plusBtn.OnInitialize(OnPlusBtnClick);
            }
        }*/

        // 스레드 버튼 생성
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
    }

    /// <summary>
    /// 미리보기 이미지를 로드합니다.
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
    /// 카테고리 선택 시 호출됩니다.
    /// </summary>
    private void OnCategorySelected(string categoryId)
    {
        _selectedCategoryId = categoryId;
        RefreshThreadList();
    }

    /// <summary>
    /// 스레드 클릭 시 호출됩니다.
    /// </summary>
    private void OnThreadClick(string threadId)
    {
        if (_dataManager == null)
            return;

        Debug.Log($"[ManageThreadPanel] Thread selected: {threadId}");

        // 콜백 호출
        if (_onThreadSelectedCallback != null)
        {
            _onThreadSelectedCallback(threadId);
        }

        // 패널 닫기
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// 스레드 편집 클릭 시 호출됩니다.
    /// </summary>
    private void OnThreadEdit(string threadId)
    {
        if (_dataManager == null)
            return;

        // TODO: 스레드 편집 기능 구현
        Debug.Log($"[ManageThreadPanel] Thread edit: {threadId}");
    }

    /// <summary>
    /// 스레드 삭제 클릭 시 호출됩니다.
    /// </summary>
    private void OnThreadDelete(string threadId)
    {
        if (_dataManager == null)
            return;

        // 확인 후 삭제 (간단하게 메시지만 표시하고 삭제)
        ThreadState thread = _dataManager.GetThread(threadId);
        string threadName = thread != null ? thread.threadName : threadId;
        
        // 스레드 삭제
        bool removed = _dataManager.RemoveThread(threadId);
        
        if (removed)
        {
            // 데이터 저장
            _dataManager.SaveThreadData();
            
            // UI 새로고침 (이벤트로 자동 갱신되지만 확실하게)
            RefreshThreadList();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowWarningPanel($"Thread '{threadName}' has been deleted.");
            }
            
            Debug.Log($"[ManageThreadPanel] Thread deleted: {threadId}");
        }
        else
        {
            Debug.LogWarning($"[ManageThreadPanel] Failed to delete thread: {threadId}");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowWarningPanel($"Failed to delete thread '{threadName}'.");
            }
        }
    }

    /// <summary>
    /// Plus 버튼 클릭 시 호출됩니다.
    /// </summary>
    private void OnPlusBtnClick()
    {
        if (GameManager.Instance == null)
            return;

        // 새 스레드 이름 입력
        GameManager.Instance.ShowEnterNamePanel((threadName) =>
        {
            if (string.IsNullOrEmpty(threadName))
                return;

            // 스레드 ID 생성 (이름을 기반으로)
            string threadId = $"thread_{threadName.Replace(" ", "_")}";
            
            // 중복 확인
            if (_dataManager.HasThread(threadId))
            {
                threadId = $"{threadId}_{System.DateTime.Now.Ticks}";
            }

            // 새 스레드 생성
            _dataManager.CreateThread(threadId, threadName);
            
            // 선택된 카테고리에 추가
            if (!string.IsNullOrEmpty(_selectedCategoryId))
            {
                _dataManager.AddThreadToCategory(_selectedCategoryId, threadId);
            }

            Debug.Log($"[ManageThreadPanel] New thread created: {threadId} ({threadName})");
        });
    }

    /// <summary>
    /// 패널을 닫습니다.
    /// </summary>
    public void ClosePanel()
    {
        Destroy(gameObject);
    }
}
