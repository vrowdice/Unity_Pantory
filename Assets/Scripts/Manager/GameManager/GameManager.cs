using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private IUIManager _uiManager;
    private IGameSceneManager _sceneManager;
    private GameDataManager _gameDataManager;
    private VisualManager _visualManager;
    private MainCameraController _mainCameraController;

    private GameObject _sharedWorldCanvas;
    private RectTransform _canvasRect;
    private Canvas _canvas;
    private CanvasScaler _scaler;

    private string _currentThreadId = string.Empty;

    public IUIManager UiManager => _uiManager;
    public float ProductionIconScale => _productionIconScale;
    public string CurrentThreadId => _currentThreadId;
    private Transform CanvasTransform => _uiManager.CanvasTrans;
    public GameObject ProductionInfoImage => _productionInfoImagePrefab;
    public GameObject TextPairPanelPrefab => _textPairPanelPrefab;
    public GameObject ActionBtnPrefab => _actionBtnPrefab;
    public GameObject GridSortContentPrefab => _gridSortContentPrefab;
    public MainCameraController MainCameraController => _mainCameraController;

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
    [SerializeField] private GameObject _textPairPanelPrefab;
    [SerializeField] private GameObject _actionBtnPrefab;

    [Header("Production Icon Settings")]
    [SerializeField] private float _productionIconScale = 1.0f;

    [Header("GameDataManager Settings")]
    [SerializeField] private GameObject _gameDataManagerPrefab;

    [Header("VisualManager Settings")]
    [SerializeField] private GameObject _visualManagerPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 로드될 때마다 호출되는 콜백. 여기서 모든 매니저와 월드 캔버스를 초기화합니다.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_gameDataManager == null)
        {
            GameObject dataManagerObj = Instantiate(_gameDataManagerPrefab);
            _gameDataManager = dataManagerObj.GetComponent<GameDataManager>();
            _gameDataManager.OnInitialize();
        }

        if (_visualManager == null)
        {
            GameObject visualManagerObj = Instantiate(_visualManagerPrefab);
            _visualManager = visualManagerObj.GetComponent<VisualManager>();
            _visualManager.OnInitialize();
        }

        if (_sharedWorldCanvas != null)
        {
            Destroy(_sharedWorldCanvas);
        }

        MainCameraController mainCamera = GameObject.Find("MainCamera").GetComponent<MainCameraController>();
        _mainCameraController = mainCamera;
        mainCamera.OnInitialize();

        CreateWorldCanvas(_mainCameraController.Camera);

        IGameSceneManager sceneManager = GameObject.Find("GameSceneManager").GetComponent<IGameSceneManager>();
        _sceneManager = sceneManager;
        _sceneManager.OnInitialize(this, _gameDataManager);

        IUIManager uiManager = GameObject.Find("Canvas").GetComponent<IUIManager>();
        _uiManager = uiManager;
        uiManager.OnInitialize(this, _gameDataManager);

        _gameDataManager.Time.PauseTime();
    }

    /// <summary>
    /// 메시지와 함께 경고 패널을 표시합니다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    public void ShowWarningPanel(string message)
    {
        GameObject warningPanelObj = Instantiate(_warningPanelPrefab, CanvasTransform);
        warningPanelObj.GetComponent<WarningPanel>().OnInitialized(message);
    }

    /// <summary>
    /// 자원 선택 패널을 표시합니다.
    /// </summary>
    /// <param name="resourceTypes">선택 가능한 자원 타입 목록</param>
    /// <param name="onResourceSelected">자원 선택 시 호출될 콜백</param>
    /// <param name="producibleResources">생산 가능한 자원 목록 (null이면 해당 타입의 모든 자원 표시)</param>
    /// <returns>생성된 SelectResourcePanel 컴포넌트</returns>
    public SelectResourcePanel ShowSelectResourcePanel(List<ResourceType> resourceTypes, System.Action<ResourceEntry> onResourceSelected, List<ResourceData> producibleResources = null)
    {
        GameObject selectResourcePanelObj = Instantiate(_selectResourcePanelPrefab, CanvasTransform);
        SelectResourcePanel selectResourcePanel = selectResourcePanelObj.GetComponent<SelectResourcePanel>();
        selectResourcePanel.OnInitialize(_gameDataManager, resourceTypes, onResourceSelected, producibleResources);
        return selectResourcePanel.GetComponent<SelectResourcePanel>();
    }

    /// <summary>
    /// 카테고리 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="dataManager">GameDataManager</param>
    /// <param name="onCategorySelected">카테고리 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadCartegoryPanel 컴포넌트</returns>
    public ManageThreadCartegoryPanel ShowManageThreadCartegoryPanel(GameDataManager dataManager, System.Action<string> onCategorySelected)
    {
        GameObject panageThreadCartegoryPanelObj = Instantiate(_manageThreadCartegoryPanelPrefab, CanvasTransform);
        ManageThreadCartegoryPanel panageThreadCartegoryPanel = panageThreadCartegoryPanelObj.GetComponent<ManageThreadCartegoryPanel>();
        panageThreadCartegoryPanel.OnInitialize(dataManager, onCategorySelected);
        return panageThreadCartegoryPanel;
    }

    /// <summary>
    /// Thread 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="onThreadSelected">스레드 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadPanel 컴포넌트</returns>
    public ManageThreadPanel ShowManageThreadPanel(System.Action<string> onThreadSelected)
    {
        GameObject manageThreadPanelObj = Instantiate(_manageThreadPanelPrefab, CanvasTransform);
        ManageThreadPanel manageThreadPanel = manageThreadPanelObj.GetComponent<ManageThreadPanel>();
        manageThreadPanel.OnInitialize(_gameDataManager, onThreadSelected);
        return manageThreadPanel;
    }

    /// <summary>
    /// 이름 입력 패널을 표시합니다.
    /// </summary>
    /// <param name="onConfirm">확인 버튼 클릭 시 호출될 콜백</param>
    /// <returns>생성된 EnterNamePanel 컴포넌트</returns>
    public EnterNamePanel ShowEnterNamePanel(System.Action<string> onConfirm)
    {
        GameObject enterNamePanelObj = Instantiate(_enterNamePanelPrefab, CanvasTransform);
        EnterNamePanel enterNamePanel = enterNamePanelObj.GetComponent<EnterNamePanel>();
        enterNamePanel.OnInitialize(onConfirm);
        return enterNamePanel;
    }

    /// <summary>
    /// 생성된 월드 캔버스의 RectTransform을 반환합니다.
    /// </summary>
    public RectTransform GetWorldCanvas()
    {
        return _canvasRect;
    }

    /// <summary>
    /// 월드 캔버스를 생성하고 초기 설정을 적용합니다.
    /// </summary>
    private void CreateWorldCanvas(Camera worldCamera)
    {
        _sharedWorldCanvas = new GameObject(_worldCanvasName);
        _sharedWorldCanvas.transform.SetParent(this.transform, false);

        _canvas = _sharedWorldCanvas.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = worldCamera != null ? worldCamera : Camera.main;
        _canvas.sortingOrder = _worldCanvasSortingOrder;

        _scaler = _sharedWorldCanvas.AddComponent<CanvasScaler>();
        _scaler.dynamicPixelsPerUnit = _worldCanvasDynamicPixelsPerUnit;

        _canvasRect = _sharedWorldCanvas.GetComponent<RectTransform>();
        _canvasRect.sizeDelta = _worldCanvasSize;
        _canvasRect.localPosition = Vector3.zero;
        _canvasRect.localRotation = Quaternion.identity;

        CanvasGroup group = _sharedWorldCanvas.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    /// <summary>
    /// 아이콘들을 배치할 컨테이너(Grid)를 생성하고 리소스 아이콘들을 함께 생성합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="name">컨테이너 이름</param>
    /// <param name="worldPosition">월드 위치</param>
    /// <param name="containerScale">컨테이너 스케일</param>
    /// <param name="productionCounts">자원 ID와 수량 딕셔너리</param>
    /// <param name="dataManager">게임 데이터 매니저</param>
    /// <returns>생성된 컨테이너 GameObject</returns>
    public GameObject CreateProductionIconContainer(Transform parent, string name, Vector3 worldPosition, float containerScale, Dictionary<string, int> productionCounts)
    {
        GameObject container = Instantiate(_gridSortContentPrefab, parent);
        container.name = name;

        if (container.TryGetComponent(out RectTransform rect))
        {
            rect.sizeDelta = new Vector2(200, 50);
        }

        Transform t = container.transform;
        t.position = worldPosition;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one * containerScale;

        // 아이콘들 생성
        if (productionCounts != null && productionCounts.Count > 0)
        {
            CreateProductionIcons(container.transform, productionCounts);
        }

        return container;
    }

    /// <summary>
    /// 단일 생산 아이콘을 생성하고 데이터를 초기화합니다.
    /// </summary>
    public GameObject CreateProductionIcon(Transform parent, ResourceEntry resourceEntry, int amount)
    {
        GameObject iconObj = Instantiate(_productionInfoImagePrefab, parent);

        if (iconObj.TryGetComponent(out RectTransform rect))
        {
            rect.localScale = Vector3.one * _productionIconScale;
        }

        if (iconObj.TryGetComponent(out ProductionInfoImage iconComponent))
        {
            iconComponent.OnInitialize(resourceEntry, amount);
        }

        return iconObj;
    }

    /// <summary>
    /// 데이터 목록을 기반으로 여러 개의 생산 아이콘을 일괄 생성합니다.
    /// </summary>
    public void CreateProductionIcons(Transform parent, Dictionary<string, int> productionCounts)
    {
        if (productionCounts == null) return;

        foreach (var (resourceId, amount) in productionCounts)
        {
            if (string.IsNullOrEmpty(resourceId)) continue;

            var entry = _gameDataManager.Resource.GetResourceEntry(resourceId);
            if (entry != null)
            {
                CreateProductionIcon(parent, entry, amount);
            }
        }
    }
}