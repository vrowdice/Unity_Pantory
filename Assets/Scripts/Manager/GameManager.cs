using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private IUIManager _uiManager;

    public IUIManager UiManager => _uiManager;

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
            _uiManager = canvas.GetComponent<MainUiManager>();
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
}