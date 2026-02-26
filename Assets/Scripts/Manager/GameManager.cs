using System;
using System.Collections.Generic;
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

    private string _currentThreadId = string.Empty;

    public CanvasBase CanvasBase => _currentCanvasBase;
    public string CurrentThreadId => _currentThreadId;
    private Transform CanvasTransform => _currentCanvasBase.CanvasTrans;
    public MainCameraController MainCameraController => _mainCameraController;
    public PoolingManager PoolingManager => _poolingManager;

    [Header("World Space Canvas Settings")]
    [SerializeField] private string _worldCanvasName = "SharedWorldCanvas";
    [SerializeField] private int _worldCanvasSortingOrder = 1;
    [SerializeField] private float _worldCanvasDynamicPixelsPerUnit = 100f;
    [SerializeField] private Vector2 _worldCanvasSize = new Vector2(1000f, 1000f);

    [Header("Manager Settings")]
    [SerializeField] private GameObject _dataManagerPrefab;
    [SerializeField] private GameObject _visualManagerPrefab;
    [SerializeField] private GameObject _saveLoadManagerPrefab;
    [SerializeField] private GameObject _sceneLoadManagerPrefab;
    [SerializeField] private GameObject _soundManagerPrefab;
    [SerializeField] private GameObject _poolingManagerPrefab;
    [SerializeField] private GameObject _uiManagerPrefab;

    private SceneLoadManager _sceneLoadManager;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return;

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

        if (_uiManagerPrefab != null)
        {
            GameObject uiManagerObj = Instantiate(_uiManagerPrefab);
            DontDestroyOnLoad(uiManagerObj);
            uiManagerObj.GetComponent<UIManager>().Init();
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

        UIManager.Instance?.RefreshCamera();
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
}