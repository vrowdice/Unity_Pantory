using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using Pantory.Managers;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private IUIManager _uiManager;
    private GameDataManager _gameDataManager;
    private MainCameraController _mainCameraController;
    private string _currentThreadId = "Sample_Thread";

    private GameUiPanelHandler _uiPanelHandler;
    private GameProductionIconHandler _productionIconHandler;
    private GameWorldCanvasHandler _worldCanvasHandler;

    public IUIManager UiManager => _uiManager;
    public string CurrentThreadId => _currentThreadId;
    public GameObject ProductionInfoImage => _productionInfoImagePrefab;
    public GameObject ActionBtnPrefab => _actionBtnPrefab;
    public MainCameraController MainCameraController => _mainCameraController;
    /// <summary>
    /// 현재 Thread ID를 설정합니다.
    /// </summary>
    public void SetCurrentThreadId(string threadId)
    {
        _currentThreadId = threadId;
        Debug.Log($"[GameManager] Current thread ID set to: {threadId}");
    }
    public GameObject GridSortContentPrefab => _gridSortContentPrefab;
    public float ProductionIconScale => _productionIconScale;

    [Header("World Space Canvas Settings")]
    [SerializeField] private string _worldCanvasName = "SharedWorldCanvas";
    [SerializeField] private int _worldCanvasSortingOrder = 1;
    [SerializeField] private float _worldCanvasDynamicPixelsPerUnit = 100f;
    [SerializeField] private Vector2 _worldCanvasSize = new Vector2(1000f, 1000f);

    [Header("Common Panel")]
    [SerializeField] private GameObject _warningPanelPrefab;
    [SerializeField] private GameObject _enterNamePanelPrefab;
    [SerializeField] private GameObject _selectResourcePanelPrefab;
    [SerializeField] private GameObject _manageThreadPanelPrefab;
    [SerializeField] private GameObject _manageThreadCartegoryPanelPrefab;

    [Header("Common UI")]
    [SerializeField] private GameObject _productionInfoImagePrefab;
    [SerializeField] private GameObject _gridSortContentPrefab;
    [SerializeField] private GameObject _actionBtnPrefab;
    
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

        InitializeHandlers();
    }

    void Start()
    {

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

        _mainCameraController = GameObject.Find("MainCamera").GetComponent<MainCameraController>();
        if (_mainCameraController == null)
        {
            Debug.LogError("[GameManager] Could not find MainCameraController on Main Camera.");
        }

        // Main 씬이 아닐 경우 시간 정지
        if (_gameDataManager != null)
        {
            if (scene.name != "Main")
            {
                _gameDataManager.Time.PauseTime();
            }
        }
        else
        {
            Debug.LogError("[GameManager] GameDataManager.Instance is null.");
        }

        InitializeHandlers();

        Camera targetCamera = _mainCameraController != null ? _mainCameraController.Camera : Camera.main;
        if (scene.name != "Title")
        {
            _worldCanvasHandler?.EnsureWorldCanvas(null, targetCamera);
        }
        else
        {
            _worldCanvasHandler?.DestroyCanvas();
        }
    }

    /// <summary>
    /// 경고 패널을 표시합니다.
    /// 현재 활성화된 씬의 Canvas에 생성됩니다.
    /// </summary>
    public void ShowWarningPanel()
    {
        if (_uiPanelHandler == null)
        {
            Debug.LogWarning("[GameManager] UI panel handler is not initialized.");
            return;
        }

        _uiPanelHandler.ShowWarningPanel();
    }

    /// <summary>
    /// 메시지와 함께 경고 패널을 표시합니다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    public void ShowWarningPanel(string message)
    {
        if (_uiPanelHandler == null)
        {
            Debug.LogWarning("[GameManager] UI panel handler is not initialized.");
            return;
        }

        _uiPanelHandler.ShowWarningPanel(message);
    }

    /// <summary>
    /// 자원 선택 패널을 표시합니다.
    /// </summary>
    /// <param name="resourceTypes">선택 가능한 자원 타입 목록</param>
    /// <param name="onResourceSelected">자원 선택 시 호출될 콜백</param>
    /// <returns>생성된 SelectResourcePanel 컴포넌트</returns>
    public SelectResourcePanel ShowSelectResourcePanel(List<ResourceType> resourceTypes, System.Action<ResourceEntry> onResourceSelected)
    {
        if (_uiPanelHandler == null)
        {
            Debug.LogWarning("[GameManager] UI panel handler is not initialized.");
            return null;
        }

        return _uiPanelHandler.ShowSelectResourcePanel(resourceTypes, onResourceSelected);
    }

    /// <summary>
    /// 카테고리 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="dataManager">GameDataManager</param>
    /// <param name="onCategorySelected">카테고리 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadCartegoryPanel 컴포넌트</returns>
    public ManageThreadCartegoryPanel ShowManageThreadCartegoryPanel(GameDataManager dataManager, System.Action<string> onCategorySelected = null)
    {
        if (_uiPanelHandler == null)
        {
            Debug.LogWarning("[GameManager] UI panel handler is not initialized.");
            return null;
        }

        return _uiPanelHandler.ShowManageThreadCategoryPanel(dataManager, onCategorySelected);
    }

    /// <summary>
    /// Thread 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="onThreadSelected">스레드 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadPanel 컴포넌트</returns>
    public ManageThreadPanel ShowManageThreadPanel(System.Action<string> onThreadSelected = null)
    {
        if (_uiPanelHandler == null)
        {
            Debug.LogWarning("[GameManager] UI panel handler is not initialized.");
            return null;
        }

        return _uiPanelHandler.ShowManageThreadPanel(onThreadSelected);
    }

    /// <summary>
    /// 이름 입력 패널을 표시합니다.
    /// </summary>
    /// <param name="message">안내 메시지</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 호출될 콜백</param>
    /// <returns>생성된 EnterNamePanel 컴포넌트</returns>
    public EnterNamePanel ShowEnterNamePanel(System.Action<string> onConfirm)
    {
        if (_uiPanelHandler == null)
        {
            Debug.LogWarning("[GameManager] UI panel handler is not initialized.");
            return null;
        }

        return _uiPanelHandler.ShowEnterNamePanel(onConfirm);
    }

    // ================== Production Icon Helper Methods ==================

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
        if (_productionIconHandler == null)
        {
            Debug.LogWarning("[GameManager] Production icon handler is not initialized.");
            return null;
        }

        return _productionIconHandler.CreateProductionIconContainerWithoutCanvas(parent, name, worldPosition, containerScale);
    }

    /// <summary>
    /// 생산 아이콘을 생성하고 초기화합니다.
    /// HorizontalSortContentPrefab에 넣으면 자동으로 정렬됩니다.
    /// </summary>
    /// <param name="parent">부모 Transform (보통 HorizontalSortContent)</param>
    /// <param name="resourceEntry">자원 정보</param>
    /// <param name="amount">생산/소모량</param>
    /// <returns>생성된 아이콘 GameObject</returns>
    public GameObject CreateProductionIcon(Transform parent, ResourceEntry resourceEntry, int amount = -1)
    {
        if (_productionIconHandler == null)
        {
            Debug.LogWarning("[GameManager] Production icon handler is not initialized.");
            return null;
        }

        return _productionIconHandler.CreateProductionIcon(parent, resourceEntry, amount);
    }

    /// <summary>
    /// 여러 생산 아이콘을 생성합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="productionCounts">자원 ID와 생산/소모량 매핑</param>
    /// <param name="dataManager">데이터 매니저</param>
    /// <param name="isOutput">true면 생산, false면 소모</param>
    public void CreateProductionIcons(Transform parent, Dictionary<string, int> productionCounts, GameDataManager dataManager)
    {
        if (_productionIconHandler == null)
        {
            Debug.LogWarning("[GameManager] Production icon handler is not initialized.");
            return;
        }

        _productionIconHandler.CreateProductionIcons(parent, productionCounts, dataManager);
    }

    // ================== Shared World Canvas Helper ==================

    /// <summary>
    /// 월드 스페이스 캔버스를 가져오거나 새로 만듭니다.
    /// </summary>
    public RectTransform GetWorldCanvas(Transform parent = null, Camera worldCamera = null)
    {
        return _worldCanvasHandler != null ? _worldCanvasHandler.GetWorldCanvas(parent, worldCamera) : null;
    }

    /// <summary>
    /// 월드 스페이스 캔버스 Transform을 반환합니다.
    /// </summary>
    public Transform GetWorldCanvasTransform()
    {
        return _worldCanvasHandler != null ? _worldCanvasHandler.GetWorldCanvasTransform() : null;
    }

    /// <summary>
    /// 월드 스페이스 캔버스 위치를 반환합니다.
    /// </summary>
    public Vector3? GetWorldCanvasPosition()
    {
        return _worldCanvasHandler != null ? _worldCanvasHandler.GetWorldCanvasPosition() : (Vector3?)null;
    }

    private void InitializeHandlers()
    {
        if (_uiPanelHandler == null)
        {
            _uiPanelHandler = new GameUiPanelHandler(
                _uiManager,
                _gameDataManager,
                _warningPanelPrefab,
                _enterNamePanelPrefab,
                _selectResourcePanelPrefab,
                _manageThreadPanelPrefab,
                _manageThreadCartegoryPanelPrefab);
        }
        else
        {
            _uiPanelHandler.UpdateReferences(_uiManager, _gameDataManager);
        }

        if (_productionIconHandler == null)
        {
            _productionIconHandler = new GameProductionIconHandler(_gridSortContentPrefab, _productionInfoImagePrefab, _productionIconScale);
        }
        else
        {
            _productionIconHandler.UpdateSettings(_gridSortContentPrefab, _productionInfoImagePrefab, _productionIconScale);
        }

        if (_worldCanvasHandler == null)
        {
            _worldCanvasHandler = new GameWorldCanvasHandler(transform, _worldCanvasName, _worldCanvasSortingOrder, _worldCanvasDynamicPixelsPerUnit, _worldCanvasSize);
        }
        else
        {
            _worldCanvasHandler.UpdateSettings(_worldCanvasName, _worldCanvasSortingOrder, _worldCanvasDynamicPixelsPerUnit, _worldCanvasSize);
        }

        InitializeSceneManagers();
    }

    private void InitializeSceneManagers()
    {
        if (_gameDataManager == null)
        {
            Debug.LogWarning("[GameManager] GameDataManager is null. Cannot initialize scene managers.");
            return;
        }

        GameObject sceneManagerRoot = GameObject.Find("SceneManager");
        if (sceneManagerRoot == null)
        {
            Debug.Log("[GameManager] SceneManager object not found in the scene. Skipping scene manager initialization.");
            return;
        }

        var managers = sceneManagerRoot
            .GetComponentsInChildren<MonoBehaviour>(true)
            .OfType<ISceneManagerComponent>()
            .ToList();

        if (managers.Count == 0)
        {
            Debug.Log("[GameManager] No ISceneManagerComponent implementations found under SceneManager.");
            return;
        }

        foreach (var manager in managers)
        {
            manager.Initialize(this, _gameDataManager);
        }
    }
}