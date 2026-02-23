using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    private readonly List<Action> _closeStack = new List<Action>();

    private CanvasBase _currentCanvasBase;
    private RunnerBase _currentRunnerBase;
    private DataManager _dataManager;
    private VisualManager _visualManager;
    private SaveLoadManager _saveLoadManager;
    private PoolingManager _poolingManager;
    private MainCameraController _mainCameraController;

    private Canvas _currnetWorldCanvas;
    private GameObject _sharedWorldCanvas;
    private RectTransform _canvasRect;
    private CanvasScaler _scaler;

    private Transform _managerCanvasTransform;
    private string _currentThreadId = string.Empty;

    public CanvasBase CanvasBase => _currentCanvasBase;
    public float ProductionIconScale => _productionIconScale;
    public string CurrentThreadId => _currentThreadId;
    private Transform CanvasTransform => _currentCanvasBase.CanvasTrans;
    public GameObject ProductionInfoImage => _productionInfoImagePrefab;
    public GameObject TextPairPanelPrefab => _textPairPanelPrefab;
    public GameObject ActionBtnPrefab => _actionBtnPrefab;
    public GameObject GridSortContentPrefab => _gridSortContentPrefab;
    public MainCameraController MainCameraController => _mainCameraController;
    public PoolingManager PoolingManager => _poolingManager;
    public Transform ManagerCanvasTransform => _managerCanvasTransform;

    [Header("World Space Canvas Settings")]
    [SerializeField] private string _worldCanvasName = "SharedWorldCanvas";
    [SerializeField] private int _worldCanvasSortingOrder = 1;
    [SerializeField] private float _worldCanvasDynamicPixelsPerUnit = 100f;
    [SerializeField] private Vector2 _worldCanvasSize = new Vector2(1000f, 1000f);

    [Header("Common Panel")]
    [SerializeField] private GameObject _optionPanelPrefab;
    [SerializeField] private GameObject _warningPanelPrefab;
    [SerializeField] private GameObject _confirmPanelPrefab;
    [SerializeField] private GameObject _enterNamePanelPrefab;
    [SerializeField] private GameObject _selectResourcePanelPrefab;
    [SerializeField] private GameObject _manageThreadPanelPrefab;
    [SerializeField] private GameObject _manageThreadCartegoryPanelPrefab;
    [SerializeField] private GameObject _saveLoadPopupPrefab;

    [Header("Common UI")]
    [SerializeField] private GameObject _productionInfoImagePrefab;
    [SerializeField] private GameObject _gridSortContentPrefab;
    [SerializeField] private GameObject _textPairPanelPrefab;
    [SerializeField] private GameObject _actionBtnPrefab;
    [SerializeField] private GameObject _effectTextPairPanelPrefab;

    [Header("Production Icon Settings")]
    [SerializeField] private float _productionIconScale = 1.0f;

    [Header("Manager Settings")]
    [SerializeField] private GameObject _dataManagerPrefab;
    [SerializeField] private GameObject _visualManagerPrefab;
    [SerializeField] private GameObject _saveLoadManagerPrefab;
    [SerializeField] private GameObject _sceneLoadManagerPrefab;
    [SerializeField] private GameObject _soundManagerPrefab;
    [SerializeField] private GameObject _poolingManagerPrefab;

    private SceneLoadManager _sceneLoadManager;

    public GameObject EffectTextPairPanelPrefab => _effectTextPairPanelPrefab;

    protected override void Awake()
    {
        base.Awake();
        
        if (Instance != this) return;
        
        CreateManagerCanvas();
        
        if (_saveLoadManager == null)
        {
            GameObject saveLoadManagerObj = Instantiate(_saveLoadManagerPrefab);
            _saveLoadManager = saveLoadManagerObj.GetComponent<SaveLoadManager>();
            _saveLoadManager.Init();
        }

        if (_dataManager == null)
        {
            GameObject dataManagerObj = Instantiate(_dataManagerPrefab);
            _dataManager = dataManagerObj.GetComponent<DataManager>();
            _dataManager.Init();
        }

        if (_visualManager == null)
        {
            GameObject visualManagerObj = Instantiate(_visualManagerPrefab);
            _visualManager = visualManagerObj.GetComponent<VisualManager>();
            _visualManager.Init();
        }

        if (_sceneLoadManager == null)
        {
            GameObject sceneLoadManagerObj = Instantiate(_sceneLoadManagerPrefab);
            _sceneLoadManager = sceneLoadManagerObj.GetComponent<SceneLoadManager>();
            _sceneLoadManager.Init();
        }

        if (SoundManager.Instance == null && _soundManagerPrefab != null)
        {
            Instantiate(_soundManagerPrefab);
        }

        if (_poolingManager == null && _poolingManagerPrefab != null)
        {
            GameObject poolingManagerObj = Instantiate(_poolingManagerPrefab);
            _poolingManager = poolingManagerObj.GetComponent<PoolingManager>();
            _poolingManager.Init(this);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TryCloseTopmost();
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void PushCloseable(Action onClose)
    {
        if (onClose != null)
        {
            _closeStack.Add(onClose);
        }
    }

    public void RemoveCloseable(Action onClose)
    {
        if (onClose != null)
        {
            for (int i = _closeStack.Count - 1; i >= 0; i--)
            {
                if (_closeStack[i] == onClose)
                {
                    _closeStack.RemoveAt(i);
                    return;
                }
            }
        }
    }

    public void TryCloseTopmost()
    {
        if (_closeStack.Count == 0) return;
        int last = _closeStack.Count - 1;
        Action close = _closeStack[last];
        _closeStack.RemoveAt(last);
        close?.Invoke();
    }

    public void ClearCloseStack()
    {
        _closeStack.Clear();
    }

    public void CloseAllPopups()
    {
        if (_closeStack.Count == 0) return;
        var copy = new List<Action>(_closeStack);
        _closeStack.Clear();
        for (int i = copy.Count - 1; i >= 0; i--)
        {
            copy[i]?.Invoke();
        }
    }

    /// <summary>
    /// 씬이 로드될 때마다 호출되는 콜백. 여기서 모든 매니저와 월드 캔버스를 초기화합니다.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearCloseStack();

        if (_sharedWorldCanvas != null)
        {
            Destroy(_sharedWorldCanvas);
        }

        _dataManager.ClearAllEventSubscriptions();
        _dataManager.Time.PauseTime();

        MainCameraController mainCamera = GameObject.Find("MainCamera").GetComponent<MainCameraController>();
        _mainCameraController = mainCamera;
        mainCamera.Init();

        CreateWorldCanvas(_mainCameraController.Camera);

        RunnerBase runnerBase = FindAnyObjectByType<RunnerBase>();
        _currentRunnerBase = runnerBase;
        _currentRunnerBase.Init();

        if (_managerCanvasTransform != null)
        {
            Canvas managerCanvas = _managerCanvasTransform.GetComponent<Canvas>();
            if (managerCanvas != null)
            {
                managerCanvas.worldCamera = Camera.main;
            }
        }
    }

    /// <summary>
    /// GameManager 전용 Canvas를 생성합니다.
    /// </summary>
    private void CreateManagerCanvas()
    {
        GameObject canvasObj = new GameObject("ManagerCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 100;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1200);

        _managerCanvasTransform = canvasObj.transform;
        DontDestroyOnLoad(canvasObj);
    }

    /// <summary>
    /// 경고 패널을 표시합니다. 메시지는 WarningMessage 테이블의 키로 전달합니다.
    /// </summary>
    /// <param name="messageKey">WarningMessage 테이블의 로컬라이즈 키</param>
    public void ShowWarningPopup(string messageKey)
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return;
        }

        GameObject warningPanelObj = _poolingManager.GetPooledObject(_warningPanelPrefab);
        warningPanelObj.transform.SetParent(_managerCanvasTransform, false);
        warningPanelObj.GetComponent<WarningPopup>().Init(messageKey);
    }

    /// <summary>
    /// 자원 선택 패널을 표시합니다.
    /// </summary>
    /// <param name="resourceTypes">선택 가능한 자원 타입 목록</param>
    /// <param name="onResourceSelected">자원 선택 시 호출될 콜백</param>
    /// <param name="producibleResources">생산 가능한 자원 목록 (null이면 해당 타입의 모든 자원 표시)</param>
    /// <returns>생성된 SelectResourcePanel 컴포넌트</returns>
    public SelectResourcePopup ShowSelectResourcePopup(List<ResourceType> resourceTypes, System.Action<ResourceEntry> onResourceSelected, List<ResourceData> producibleResources = null)
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return null;
        }

        GameObject selectResourcePanelObj = Instantiate(_selectResourcePanelPrefab, _managerCanvasTransform, false);
        SelectResourcePopup selectResourcePanel = selectResourcePanelObj.GetComponent<SelectResourcePopup>();
        selectResourcePanel.Init(_dataManager, resourceTypes, onResourceSelected, producibleResources);
        return selectResourcePanel.GetComponent<SelectResourcePopup>();
    }

    /// <summary>
    /// 카테고리 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="dataManager">GameDataManager</param>
    /// <param name="onCategorySelected">카테고리 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadCartegoryPanel 컴포넌트</returns>
    public ManageThreadCartegoryPopup ShowManageThreadCartegoryPopup(DataManager dataManager, System.Action<string> onCategorySelected)
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return null;
        }

        GameObject panageThreadCartegoryPanelObj = Instantiate(_manageThreadCartegoryPanelPrefab, _managerCanvasTransform, false);
        ManageThreadCartegoryPopup panageThreadCartegoryPanel = panageThreadCartegoryPanelObj.GetComponent<ManageThreadCartegoryPopup>();
        panageThreadCartegoryPanel.Init(dataManager, onCategorySelected);
        return panageThreadCartegoryPanel;
    }

    /// <summary>
    /// Thread 관리 패널을 표시합니다.
    /// </summary>
    /// <param name="onThreadSelected">스레드 선택 시 호출될 콜백 (옵션)</param>
    /// <returns>생성된 ManageThreadPanel 컴포넌트</returns>
    public ManageThreadPopup ShowManageThreadPopup(System.Action<string> onThreadSelected)
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return null;
        }

        GameObject manageThreadPanelObj = Instantiate(_manageThreadPanelPrefab, _managerCanvasTransform, false);
        ManageThreadPopup manageThreadPanel = manageThreadPanelObj.GetComponent<ManageThreadPopup>();
        manageThreadPanel.Init(onThreadSelected);
        return manageThreadPanel;
    }

    /// <summary>
    /// 확인 팝업을 표시합니다. 메시지는 ConfirmMessage 테이블의 키로 전달합니다.
    /// </summary>
    /// <param name="messageKey">ConfirmMessage 테이블의 로컬라이즈 키</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 호출될 콜백</param>
    /// <returns>생성된 ConfirmPopup 컴포넌트</returns>
    public ConfirmPopup ShowConfirmPopup(string messageKey, System.Action onConfirm)
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return null;
        }

        GameObject confirmPanelObj = Instantiate(_confirmPanelPrefab, _managerCanvasTransform, false);
        ConfirmPopup confirmPanel = confirmPanelObj.GetComponent<ConfirmPopup>();
        confirmPanel.Init(messageKey, onConfirm);
        return confirmPanel;
    }

    /// <summary>
    /// 이름 입력 패널을 표시합니다.
    /// </summary>
    /// <param name="onConfirm">확인 버튼 클릭 시 호출될 콜백</param>
    /// <returns>생성된 EnterNamePanel 컴포넌트</returns>
    public EnterNamePopup ShowEnterNamePopup(System.Action<string> onConfirm)
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return null;
        }

        GameObject enterNamePanelObj = Instantiate(_enterNamePanelPrefab, _managerCanvasTransform, false);
        EnterNamePopup enterNamePanel = enterNamePanelObj.GetComponent<EnterNamePopup>();
        enterNamePanel.Init(onConfirm);
        return enterNamePanel;
    }

    /// <summary>
    /// 옵션 패널을 표시합니다.
    /// </summary>
    /// <returns>생성된 OptionPanel 컴포넌트</returns>
    public OptionPopup ShowOptionPopup()
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return null;
        }

        GameObject optionPanelObj = Instantiate(_optionPanelPrefab, _managerCanvasTransform, false);
        OptionPopup optionPanel = optionPanelObj.GetComponent<OptionPopup>();

        optionPanel.Init();
        return optionPanel;
    }

    /// <summary>
    /// 세이브/로드 팝업을 표시합니다.
    /// </summary>
    /// <param name="isSaveMode">true면 세이브 모드, false면 로드 모드</param>
    /// <returns>생성된 SaveLoadPopup 컴포넌트</returns>
    public SaveLoadPopup ShowSaveLoadPopup(bool isSaveMode)
    {
        if (_managerCanvasTransform == null)
        {
            Debug.LogError("[GameManager] ManagerCanvas is not initialized.");
            return null;
        }

        if (_saveLoadPopupPrefab == null)
        {
            Debug.LogError("[GameManager] SaveLoadPopupPrefab is not assigned.");
            return null;
        }

        GameObject saveLoadPopupObj = Instantiate(_saveLoadPopupPrefab, _managerCanvasTransform, false);
        SaveLoadPopup saveLoadPopup = saveLoadPopupObj.GetComponent<SaveLoadPopup>();
        saveLoadPopup.Init(isSaveMode);
        return saveLoadPopup;
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

        _currnetWorldCanvas = _sharedWorldCanvas.AddComponent<Canvas>();
        _currnetWorldCanvas.renderMode = RenderMode.WorldSpace;
        _currnetWorldCanvas.worldCamera = worldCamera != null ? worldCamera : Camera.main;
        _currnetWorldCanvas.sortingOrder = _worldCanvasSortingOrder;

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

        if (productionCounts != null && productionCounts.Count > 0)
        {
            CreateProductionIcons(container.transform, productionCounts);
        }

        return container;
    }

    /// <summary>
    /// 단일 생산 아이콘을 생성하고 데이터를 초기화합니다. (PoolingManager 사용)
    /// </summary>
    public GameObject CreateProductionIcon(Transform parent, ResourceEntry resourceEntry, int amount)
    {
        if (_poolingManager == null || _productionInfoImagePrefab == null)
        {
            Debug.LogWarning("[GameManager] PoolingManager or ProductionInfoImagePrefab is null.");
            return null;
        }

        GameObject iconObj = _poolingManager.GetPooledObject(_productionInfoImagePrefab);
        iconObj.transform.SetParent(parent, false);

        if (iconObj.TryGetComponent(out RectTransform rect))
        {
            rect.localScale = Vector3.one * _productionIconScale;
        }

        if (iconObj.TryGetComponent(out ProductionInfoImage iconComponent))
        {
            iconComponent.Init(resourceEntry, amount);
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

            var entry = _dataManager.Resource.GetResourceEntry(resourceId);
            if (entry != null)
            {
                CreateProductionIcon(parent, entry, amount);
            }
        }
    }

    /// <summary>
    /// 단일 액션 버튼을 생성하고 반환합니다.
    /// 각 패널에서 Enum 값들을 순회하며 이 메서드를 호출하여 버튼을 생성하세요.
    /// </summary>
    /// <param name="parent">버튼을 생성할 부모 Transform</param>
    /// <param name="label">버튼에 표시할 텍스트</param>
    /// <param name="onClick">버튼 클릭 시 호출될 콜백</param>
    /// <returns>생성된 ActionBtn 컴포넌트</returns>
    public ActionBtn CreateActionButton(Transform parent, string label, System.Action onClick)
    {
        if (_actionBtnPrefab == null || parent == null)
        {
            Debug.LogWarning("[GameManager] ActionBtnPrefab or parent Transform is null.");
            return null;
        }

        GameObject btnObj = Instantiate(_actionBtnPrefab, parent);
        ActionBtn btn = btnObj.GetComponent<ActionBtn>();
        if (btn != null)
        {
            btn.Init(label, onClick);
        }
        return btn;
    }
    
    /// <summary>
    /// Effect TextPairPanel을 생성하고 초기화합니다. (PoolingManager 사용)
    /// </summary>
    public TextPairPanel CreateEffectTextPairPanel(Transform parent, EffectState effectState, Color mainTextColor = default)
    {
        if (_effectTextPairPanelPrefab == null || parent == null)
        {
            Debug.LogWarning("[GameManager] EffectTextPairPanelPrefab or parent is null.");
            return null;
        }

        GameObject panelObj = _poolingManager.GetPooledObject(_effectTextPairPanelPrefab);
        panelObj.transform.SetParent(parent, false);
        
        TextPairPanel panel = panelObj.GetComponent<TextPairPanel>();
        if (panel != null)
        {
            string mainText = effectState.id.Localize(LocalizationUtils.TABLE_EFFECT);
            string secondText = _dataManager.Effect.FormatEffectValue(effectState.value, effectState.modifierType);

            panel.Init(mainText, secondText, effectState.value, mainTextColor);
        }
        return panel;
    }
}