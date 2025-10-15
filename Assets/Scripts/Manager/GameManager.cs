using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private IUIManager _uiManager;
    private GameDataManager _gameDataManager;

    public IUIManager UiManager => _uiManager;

    [Header("Common UI")]
    [SerializeField] private GameObject _warningPanelPrefab;

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
}