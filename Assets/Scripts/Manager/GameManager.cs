using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private IUIManager _uiManager;
    private GameDataManager _gameDataManager;

    private string _currentThreadId = "Sample_Thread";

    public IUIManager UiManager => _uiManager;
    public string CurrentThreadId => _currentThreadId;
    public GameObject ProductionInfoImage => _productionInfoImagePrefab;
    
    /// <summary>
    /// 현재 Thread ID를 설정합니다.
    /// </summary>
    public void SetCurrentThreadId(string threadId)
    {
        _currentThreadId = threadId;
        Debug.Log($"[GameManager] Current thread ID set to: {threadId}");
    }
    public GameObject HorizontalSortContentPrefab => _horizontalSortContentPrefab;
    public float ProductionIconScale => _productionIconScale;

    [Header("Common Panel")]
    [SerializeField] private GameObject _warningPanelPrefab;
    [SerializeField] private GameObject _enterNamePanelPrefab;
    [SerializeField] private GameObject _selectResourcePanelPrefab;
    [SerializeField] private GameObject _manageThreadPanelPrefab;
    [SerializeField] private GameObject _manageThreadCartegoryPanelPrefab;

    [Header("Common UI")]
    [SerializeField] private GameObject _productionInfoImagePrefab;
    [SerializeField] private GameObject _horizontalSortContentPrefab;
    
    [Header("Production Icon Settings")]
    [SerializeField] private float _productionIconScale = 1.0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
            
            // 씬 로드 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            _uiManager = canvas.GetComponent<IUIManager>();
            if (_uiManager == null)
            {
                Debug.LogError("[GameManager] Could not find MainUiManager on Canvas.");
            }
        }
        else
        {
            Debug.LogError("[GameManager] Could not find Canvas object.");
        }

        if (_gameDataManager == null)
        {
            _gameDataManager = GameDataManager.Instance;
        }
    }

    void Start()
    {
        if (_gameDataManager == null)
        {
            _gameDataManager = GameDataManager.Instance;
        }

        // MainUiManager 초기화
        if (_uiManager != null)
        {
            var dataManager = GameDataManager.Instance;
            if (dataManager != null)
            {
                _uiManager.Initialize(this, dataManager);
            }
            else
            {
                Debug.LogError("[GameManager] GameDataManager.Instance is null. Cannot initialize MainUiManager.");
            }
        }
    }

    void OnDestroy()
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 로드될 때마다 호출되는 콜백
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_gameDataManager == null)
        {
            _gameDataManager = GameDataManager.Instance;
        }

        // Canvas와 UIManager 재설정
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            _uiManager = canvas.GetComponent<IUIManager>();
            if (_uiManager == null)
            {
                Debug.LogError("[GameManager] Could not find MainUiManager on Canvas.");
            }
        }
        else
        {
            Debug.LogError("[GameManager] Could not find Canvas object.");
        }

        // Main 씬이 아닐 경우 시간 정지
        if (_gameDataManager != null)
        {
            if (scene.name != "Main")
            {
                _gameDataManager.PauseTime();
            }
        }
        else
        {
            Debug.LogError("[GameManager] GameDataManager.Instance is null.");
        }
    }

    void Update()
    {

    }

    /// <summary>
    /// 경고 패널을 표시합니다.
    /// 현재 활성화된 씬의 Canvas에 생성됩니다.
    /// </summary>
    public void ShowWarningPanel()
    {
        if (_warningPanelPrefab == null)
        {
            Debug.LogWarning("[GameManager] Warning panel prefab is not assigned.");
            return;
        }

        // 경고 패널 생성
        GameObject warningPanel = Instantiate(_warningPanelPrefab, _uiManager.CanvasTrans);
        Debug.Log("[GameManager] Warning panel displayed.");
    }

    /// <summary>
    /// 메시지와 함께 경고 패널을 표시합니다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    public void ShowWarningPanel(string message)
    {
        if (_warningPanelPrefab == null)
        {
            Debug.LogWarning("[GameManager] Warning panel prefab is not assigned.");
            return;
        }

        // 경고 패널 생성
        GameObject warningPanel = Instantiate(_warningPanelPrefab, _uiManager.CanvasTrans);
        
        // 메시지 설정
        var warningPanelComponent = warningPanel.GetComponent<WarningPanel>();
        if (warningPanelComponent != null)
        {
            warningPanelComponent.SetMessage(message);
            Debug.Log($"[GameManager] Warning panel displayed with message: {message}");
        }
        else
        {
            Debug.LogWarning("[GameManager] WarningPanel component not found on instantiated prefab.");
        }
    }

    /// <summary>
    /// 자원 선택 패널을 표시합니다.
    /// </summary>
    /// <param name="resourceTypes">선택 가능한 자원 타입 목록</param>
    /// <param name="onResourceSelected">자원 선택 시 호출될 콜백</param>
    /// <returns>생성된 SelectResourcePanel 컴포넌트</returns>
    public SelectResourcePanel ShowSelectResourcePanel(List<ResourceType> resourceTypes, System.Action<ResourceEntry> onResourceSelected)
    {
        if (_selectResourcePanelPrefab == null)
        {
            Debug.LogWarning("[GameManager] Select resource panel prefab is not assigned.");
            return null;
        }

        if (_gameDataManager == null)
        {
            Debug.LogWarning("[GameManager] GameDataManager is null.");
            return null;
        }

        // 자원 선택 패널 생성
        GameObject selectResourcePanel = Instantiate(_selectResourcePanelPrefab, _uiManager.CanvasTrans);
        SelectResourcePanel panelComponent = selectResourcePanel.GetComponent<SelectResourcePanel>();
        
        if (panelComponent != null)
        {
            // 패널 초기화
            panelComponent.OnInitialize(_gameDataManager, resourceTypes, onResourceSelected);
            Debug.Log("[GameManager] Select resource panel displayed.");
        }
        else
        {
            Debug.LogWarning("[GameManager] SelectResourcePanel component not found on instantiated prefab.");
        }

        return panelComponent;
    }

    /// <summary>
    /// 카테고리 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="dataManager">GameDataManager</param>
    /// <param name="onCategorySelected">카테고리 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadCartegoryPanel 컴포넌트</returns>
    public ManageThreadCartegoryPanel ShowManageThreadCartegoryPanel(GameDataManager dataManager, System.Action<string> onCategorySelected = null)
    {
        if (_manageThreadCartegoryPanelPrefab == null)
        {
            Debug.LogWarning("[GameManager] ManageThreadCartegoryPanel prefab is not assigned.");
            return null;
        }

        // 카테고리 관리 패널 생성
        GameObject panel = Instantiate(_manageThreadCartegoryPanelPrefab, _uiManager.CanvasTrans);
        ManageThreadCartegoryPanel panelComponent = panel.GetComponent<ManageThreadCartegoryPanel>();
        
        if (panelComponent != null)
        {
            // 패널 초기화
            panelComponent.OnInitialize(dataManager, onCategorySelected);
            Debug.Log("[GameManager] ManageThreadCartegoryPanel displayed.");
        }
        else
        {
            Debug.LogWarning("[GameManager] ManageThreadCartegoryPanel component not found on instantiated prefab.");
        }

        return panelComponent;
    }

    /// <summary>
    /// Thread 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="onThreadSelected">스레드 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadPanel 컴포넌트</returns>
    public ManageThreadPanel ShowManageThreadPanel(System.Action<string> onThreadSelected = null)
    {
        if (_manageThreadPanelPrefab == null)
        {
            Debug.LogWarning("[GameManager] ManageThreadPanel prefab is not assigned.");
            return null;
        }

        if (_gameDataManager == null)
        {
            Debug.LogWarning("[GameManager] GameDataManager is null.");
            return null;
        }

        // Thread 관리 패널 생성
        GameObject panel = Instantiate(_manageThreadPanelPrefab, _uiManager.CanvasTrans);
        ManageThreadPanel panelComponent = panel.GetComponent<ManageThreadPanel>();
        
        if (panelComponent != null)
        {
            // 패널 초기화 (콜백 포함)
            panelComponent.OnInitialize(_gameDataManager, onThreadSelected);
            Debug.Log("[GameManager] ManageThreadPanel displayed.");
        }
        else
        {
            Debug.LogWarning("[GameManager] ManageThreadPanel component not found on instantiated prefab.");
        }

        return panelComponent;
    }

    /// <summary>
    /// 이름 입력 패널을 표시합니다.
    /// </summary>
    /// <param name="message">안내 메시지</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 호출될 콜백</param>
    /// <returns>생성된 EnterNamePanel 컴포넌트</returns>
    public EnterNamePanel ShowEnterNamePanel(System.Action<string> onConfirm)
    {
        if (_enterNamePanelPrefab == null)
        {
            Debug.LogWarning("[GameManager] Rename panel prefab is not assigned.");
            return null;
        }

        // 이름 입력 패널 생성
        GameObject panel = Instantiate(_enterNamePanelPrefab, _uiManager.CanvasTrans);
        EnterNamePanel panelComponent = panel.GetComponent<EnterNamePanel>();
        
        if (panelComponent != null)
        {
            // 패널 초기화
            panelComponent.OnInitialize(onConfirm);
            Debug.Log("[GameManager] EnterNamePanel displayed.");
        }
        else
        {
            Debug.LogWarning("[GameManager] EnterNamePanel component not found on instantiated prefab.");
        }

        return panelComponent;
    }

    // ================== Production Icon Helper Methods ==================

    /// <summary>
    /// World Space Canvas로 설정된 생산 아이콘 컨테이너를 생성합니다.
    /// 이 컨테이너는 자식으로 추가된 ProductionInfoImage를 자동으로 정렬합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="name">컨테이너 이름</param>
    /// <param name="localPosition">로컬 위치</param>
    /// <param name="containerScale">컨테이너 스케일 (기본 0.01f, -1이면 _productionIconScale 사용)</param>
    /// <returns>생성된 컨테이너 GameObject</returns>
    public GameObject CreateProductionIconContainer(Transform parent, string name, Vector3 localPosition, float containerScale = 0.01f)
    {
        if (_horizontalSortContentPrefab == null)
        {
            Debug.LogWarning("[GameManager] HorizontalSortContentPrefab is not assigned.");
            return null;
        }

        GameObject container = Instantiate(_horizontalSortContentPrefab, parent);
        container.name = name;

        // Canvas 설정 (World Space)
        Canvas canvas = container.GetComponent<Canvas>();
        if (canvas == null)
            canvas = container.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // CanvasScaler 추가
        UnityEngine.UI.CanvasScaler scaler = container.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null)
            scaler = container.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        // RectTransform 설정
        RectTransform rectTransform = container.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(200, 50);
        }

        // 위치 및 스케일 설정
        // containerScale이 -1이면 _productionIconScale 사용 (Inspector 설정)
        float finalScale = containerScale < 0 ? _productionIconScale : containerScale;
        
        container.transform.localPosition = localPosition;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one * finalScale;

        return container;
    }

    /// <summary>
    /// 생산 아이콘 컨테이너를 생성합니다 (Canvas 없이).
    /// 공용 World Space Canvas 아래에서 사용하기 위한 메서드입니다.
    /// </summary>
    /// <param name="parent">부모 Transform (공용 Canvas)</param>
    /// <param name="name">컨테이너 이름</param>
    /// <param name="worldPosition">월드 위치</param>
    /// <param name="containerScale">컨테이너 스케일 (기본 0.01f)</param>
    /// <returns>생성된 컨테이너 GameObject</returns>
    public GameObject CreateProductionIconContainerWithoutCanvas(Transform parent, string name, Vector3 worldPosition, float containerScale = 0.01f)
    {
        if (_horizontalSortContentPrefab == null)
        {
            Debug.LogWarning("[GameManager] HorizontalSortContentPrefab is not assigned.");
            return null;
        }

        GameObject container = Instantiate(_horizontalSortContentPrefab, parent);
        container.name = name;

        // RectTransform 설정
        RectTransform rectTransform = container.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(200, 50);
        }

        // 위치 및 스케일 설정 (World Position 사용)
        container.transform.position = worldPosition;
        container.transform.rotation = Quaternion.identity;
        container.transform.localScale = Vector3.one * containerScale;

        return container;
    }

    /// <summary>
    /// 생산 아이콘을 생성하고 초기화합니다.
    /// HorizontalSortContentPrefab에 넣으면 자동으로 정렬됩니다.
    /// </summary>
    /// <param name="parent">부모 Transform (보통 HorizontalSortContent)</param>
    /// <param name="resourceEntry">자원 정보</param>
    /// <returns>생성된 아이콘 GameObject</returns>
    public GameObject CreateProductionIcon(Transform parent, ResourceEntry resourceEntry)
    {
        if (_productionInfoImagePrefab == null)
        {
            Debug.LogWarning("[GameManager] ProductionInfoImagePrefab is not assigned.");
            return null;
        }

        if (resourceEntry == null)
        {
            Debug.LogWarning("[GameManager] ResourceEntry is null.");
            return null;
        }

        // 프리팹 생성
        GameObject iconObj = Instantiate(_productionInfoImagePrefab, parent);

        // 아이콘 크기 조정 (자동 정렬에 영향 없음)
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        if (iconRect != null)
        {
            iconRect.localScale = Vector3.one * _productionIconScale;
        }

        // 아이콘 초기화
        var iconComponent = iconObj.GetComponent<ProductionInfoImage>();
        if (iconComponent != null)
        {
            iconComponent.OnInitialize(resourceEntry);
        }

        return iconObj;
    }

    /// <summary>
    /// 여러 생산 아이콘을 생성합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="productionIds">생산 ID 목록</param>
    /// <param name="dataManager">데이터 매니저</param>
    public void CreateProductionIcons(Transform parent, List<string> productionIds, GameDataManager dataManager)
    {
        if (productionIds == null || dataManager == null)
            return;

        foreach (var productionId in productionIds)
        {
            ResourceEntry resourceEntry = dataManager.GetResourceEntry(productionId);
            if (resourceEntry != null)
            {
                CreateProductionIcon(parent, resourceEntry);
            }
        }
    }
}