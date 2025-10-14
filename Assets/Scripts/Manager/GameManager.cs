using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private IUIManager _uiManager;

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
    }

    void Start()
    {
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