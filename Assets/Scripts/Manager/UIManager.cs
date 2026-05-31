using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIManager : Singleton<UIManager>
{
    private readonly List<Action> _closeStack = new List<Action>();

    private Transform _managerCanvasTransform;
    private MainCanvas _mainCanvas;
    private RectTransform _creditTopInfoToggleRect;

    [Header("Common Panel")]
    [SerializeField] private GameObject _optionPanelPrefab;
    [SerializeField] private GameObject _warningPanelPrefab;
    [SerializeField] private GameObject _confirmPanelPrefab;
    [SerializeField] private GameObject _bankruptcyGameOverPopupPrefab;
    [SerializeField] private GameObject _enterNamePanelPrefab;
    [SerializeField] private GameObject _selectResourcePanelPrefab;
    [SerializeField] private GameObject _saveLoadPopupPrefab;
    [SerializeField] private GameObject _tutorialPopupPrefab;
    [SerializeField] private GameObject _tutorialGuidedPopupPrefab;
    [SerializeField] private GameObject _debugPopupPrefab;

    [Header("Main Info Panels")]
    [SerializeField] private GameObject _creditTopInfoPopupPrefab;
    [SerializeField] private GameObject _researchInfoPopupPrefab;
    [SerializeField] private GameObject _marketActorInfoPopupPrefab;
    [SerializeField] private GameObject _newsPopupPrefab;
    [SerializeField] private GameObject _unionPopupPrefab;
    [SerializeField] private GameObject _buildingInfoPopupPrefab;
    [SerializeField] private GameObject _buildingHelpPopupPrefab;
    [SerializeField] private GameObject _resourceHelpPopupPrefab;
    [SerializeField] private GameObject _goalPopupPrefab;
    [SerializeField] private GameObject _rawBuildingInfoPanelPrefab;

    [Header("Common UI")]
    [SerializeField] private GameObject _productionInfoImagePrefab;
    [SerializeField] private GameObject _resourceImagePrefab;
    [SerializeField] private GameObject _gridSortContentPrefab;
    [SerializeField] private GameObject _textPairPanelPrefab;
    [SerializeField] private GameObject _actionBtnPrefab;
    [SerializeField] private GameObject _effectTextPairPanelPrefab;

    [Header("Production Icon Settings")]
    [SerializeField] private float _productionIconScale = 1.0f;
    [SerializeField] private float _worldResourceImageScale = 1.0f;

    const int ActionBtnInitialPoolCount = 8;
    const int ResourceImageInitialPoolCount = 16;

    public GameObject ActionBtnPrefab => _actionBtnPrefab;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearCloseStack();
        _mainCanvas = null;

        if (scene.name == "Title")
            ClearManagerCanvasPopups();
    }

    private void Update()
    {
        if (Instance != this) return;
        if (IsTypingInTextInput()) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (HasAnyOpenCloseablePanel())
            {
                TryCloseTopmost();
            }
            else if (TryCancelMainBuildMode())
            {
            }
            else if (_optionPanelPrefab != null)
            {
                ShowOptionPopup();
            }
        }

        if (Input.GetKeyDown(KeyCode.F12))
            ShowDebugPopup();
    }

    public void PushCloseable(Action onClose)
    {
        if (onClose != null)
            _closeStack.Add(onClose);
    }

    public void RemoveCloseable(Action onClose)
    {
        if (onClose == null) return;
        for (int i = _closeStack.Count - 1; i >= 0; i--)
        {
            if (_closeStack[i] == onClose)
            {
                _closeStack.RemoveAt(i);
                return;
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
        List<Action> copy = new List<Action>(_closeStack);
        _closeStack.Clear();
        for (int i = copy.Count - 1; i >= 0; i--)
            copy[i]?.Invoke();
    }

    /// <summary>
    /// DontDestroy ManagerCanvas에 남은 팝업을 즉시 제거합니다 (씬 전환·타이틀 복귀용).
    /// </summary>
    public void ClearManagerCanvasPopups()
    {
        ClearCloseStack();

        if (_managerCanvasTransform == null)
            return;

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in _managerCanvasTransform)
            children.Add(child.gameObject);

        for (int i = 0; i < children.Count; i++)
            Destroy(children[i]);
    }

    /// <summary>
    /// 닫기 스택에 등록된 패널/팝업이 하나 이상 열려있는지 반환합니다.
    /// </summary>
    public bool HasAnyOpenCloseablePanel()
    {
        return _closeStack.Count > 0;
    }

    public bool IsTypingInTextInput()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return false;

        if (eventSystem.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
            return true;

        if (eventSystem.currentSelectedGameObject.GetComponent<InputField>() != null)
            return true;

        return false;
    }

    /// <summary>
    /// GameManager에서 매니저 생성 후 호출합니다. ManagerCanvas를 생성합니다.
    /// </summary>
    public void Init()
    {
        CreateManagerCanvas();
        EnsureActionBtnPool();
        EnsureResourceImagePool();
    }

    void EnsureActionBtnPool()
    {
        if (_actionBtnPrefab == null) return;

        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool == null) return;

        pool.CreatePool(_actionBtnPrefab, ActionBtnInitialPoolCount);
    }

    void EnsureResourceImagePool()
    {
        if (_resourceImagePrefab == null) return;

        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool == null) return;

        pool.CreatePool(_resourceImagePrefab, ResourceImageInitialPoolCount);
    }

    /// <summary>
    /// 씬 로드 후 ManagerCanvas에 월드 카메라를 연결할 때 호출합니다.
    /// </summary>
    public void RefreshCamera()
    {
        if (_managerCanvasTransform == null) return;
        Canvas managerCanvas = _managerCanvasTransform.GetComponent<Canvas>();
        if (managerCanvas != null)
            managerCanvas.worldCamera = Camera.main;
    }

    /// <summary>
    /// 메인 씬 로드 후 MainCanvas를 한 번 찾아 캐시합니다. 팝업 표시 시 인자로 넘기지 않습니다.
    /// </summary>
    public void RefreshMainCanvas()
    {
        _mainCanvas = UnityEngine.Object.FindAnyObjectByType<MainCanvas>();
    }

    public MainCanvas MainCanvas => _mainCanvas;

    private bool TryCancelMainBuildMode()
    {
        if (_mainCanvas == null)
            _mainCanvas = UnityEngine.Object.FindAnyObjectByType<MainCanvas>();

        return _mainCanvas != null && _mainCanvas.TryCancelActiveBuildMode();
    }

    private void CreateManagerCanvas()
    {
        GameObject canvasObj = new GameObject("ManagerCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1200);

        _managerCanvasTransform = canvasObj.transform;
        DontDestroyOnLoad(canvasObj);
    }

    private GameObject InstantiatePopupPrefab(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, _managerCanvasTransform, false);
        GameObjectUtils.ApplyPrefabInstanceName(instance, prefab);
        return instance;
    }

    public void ShowWarningPopup(string messageKey, params object[] formatArgs)
    {
        GameObject warningPanelObj = GameManager.Instance.PoolingManager.GetPooledObject(_warningPanelPrefab);
        warningPanelObj.transform.SetParent(_managerCanvasTransform, false);
        string message = formatArgs != null && formatArgs.Length > 0
            ? messageKey.LocalizeFormat(LocalizationUtils.TABLE_COMMON, formatArgs)
            : messageKey.Localize(LocalizationUtils.TABLE_COMMON);
        warningPanelObj.GetComponent<WarningPopup>().Init(message);
    }

    public SelectResourcePopup ShowSelectResourcePopup(List<ResourceType> resourceTypes, Action<ResourceEntry> onResourceSelected, List<ResourceData> producibleResources = null)
    {
        SelectResourcePopup panel = null;
        if (_managerCanvasTransform != null)
        {
            panel = _managerCanvasTransform.GetComponentInChildren<SelectResourcePopup>(true);
        }

        if (panel == null)
        {
            GameObject selectResourcePanelObj = InstantiatePopupPrefab(_selectResourcePanelPrefab);
            panel = selectResourcePanelObj.GetComponent<SelectResourcePopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init(DataManager.Instance, resourceTypes, onResourceSelected, producibleResources);
        return panel;
    }

    public ConfirmPopup ShowConfirmPopup(string messageKey, Action onConfirm, Action onRefuse = null)
    {
        GameObject panelObj = InstantiatePopupPrefab(_confirmPanelPrefab);
        ConfirmPopup panel = panelObj.GetComponent<ConfirmPopup>();
        panel.Init(messageKey, onConfirm, onRefuse);
        return panel;
    }

    public GameOverPopup ShowBankruptcyGameOverPopup()
    {
        return ShowGameOverPopup(GameOverType.Bankruptcy);
    }

    public GameOverPopup ShowGameOverPopup(GameOverType gameOverType)
    {
        if (_bankruptcyGameOverPopupPrefab == null)
        {
            Debug.LogError("[UIManager] BankruptcyGameOverPopup prefab is not assigned.");
            return null;
        }

        GameObject panelObj = InstantiatePopupPrefab(_bankruptcyGameOverPopupPrefab);
        GameOverPopup panel = panelObj.GetComponent<GameOverPopup>();
        panel.Init(gameOverType);
        return panel;
    }

    public EnterNamePopup ShowEnterNamePopup(Action<string> onConfirm)
    {
        EnterNamePopup panel = null;
        if (_managerCanvasTransform != null)
        {
            panel = _managerCanvasTransform.GetComponentInChildren<EnterNamePopup>(true);
        }

        if (panel == null)
        {
            GameObject panelObj = InstantiatePopupPrefab(_enterNamePanelPrefab);
            panel = panelObj.GetComponent<EnterNamePopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init(onConfirm);
        return panel;
    }

    public OptionPopup ShowOptionPopup()
    {
        OptionPopup panel = null;
        if (_managerCanvasTransform != null)
        {
            panel = _managerCanvasTransform.GetComponentInChildren<OptionPopup>(true);
        }

        if (panel == null)
        {
            GameObject panelObj = InstantiatePopupPrefab(_optionPanelPrefab);
            panel = panelObj.GetComponent<OptionPopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init();
        return panel;
    }

    public TutorialPopup ShowTutorialPopup(List<TutorialData> tutorialDataList, string gameObjectName)
    {
        TutorialPopup panel = null;
        if (_managerCanvasTransform != null)
            panel = _managerCanvasTransform.GetComponentInChildren<TutorialPopup>(true);

        if (panel == null)
        {
            if (_tutorialPopupPrefab == null)
            {
                Debug.LogError("[UIManager] Tutorial popup prefab is not assigned.");
                return null;
            }

            GameObject panelObj = InstantiatePopupPrefab(_tutorialPopupPrefab);
            panel = panelObj != null ? panelObj.GetComponent<TutorialPopup>() : null;
        }

        if (panel == null)
            return null;

        panel.gameObject.SetActive(true);
        panel.Init(tutorialDataList, gameObjectName);
        return panel;
    }

    public TutorialGuidedPopup ShowTutorialGuidedPopup()
    {
        GameObject panelObj = InstantiatePopupPrefab(_tutorialGuidedPopupPrefab);
        TutorialGuidedPopup panel = panelObj != null ? panelObj.GetComponent<TutorialGuidedPopup>() : null;

        if (panel == null)
            return null;

        panel.gameObject.SetActive(true);
        panel.Init();
        return panel;
    }

    public SaveLoadPopup ShowSaveLoadPopup(bool isSaveMode)
    {
        SaveLoadPopup panel = null;
        if (_managerCanvasTransform != null)
        {
            panel = _managerCanvasTransform.GetComponentInChildren<SaveLoadPopup>(true);
        }

        if (panel == null)
        {
            GameObject panelObj = InstantiatePopupPrefab(_saveLoadPopupPrefab);
            panel = panelObj.GetComponent<SaveLoadPopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init(isSaveMode);
        return panel;
    }

    public DebugPopup ShowDebugPopup()
    {
        DebugPopup panel = null;
        if (_managerCanvasTransform != null)
            panel = _managerCanvasTransform.GetComponentInChildren<DebugPopup>(true);

        if (panel == null)
        {
            if (_debugPopupPrefab == null)
            {
                Debug.LogWarning("[UIManager] Debug popup prefab is missing.");
                return null;
            }

            GameObject panelObj = InstantiatePopupPrefab(_debugPopupPrefab);
            panel = panelObj.GetComponent<DebugPopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init();
        return panel;
    }

    public ResearchInfoPopup ShowResearchInfoPopup(ResearchEntry researchEntry)
    {
        ResearchInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<ResearchInfoPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = InstantiatePopupPrefab(_researchInfoPopupPrefab);
            popup = obj.GetComponent<ResearchInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.Init(researchEntry);
        return popup;
    }

    public MarketActorInfoPopup ShowMarketActorInfoPopup(MarketActorEntry marketActorEntry)
    {
        MarketActorInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<MarketActorInfoPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = InstantiatePopupPrefab(_marketActorInfoPopupPrefab);
            popup = obj.GetComponent<MarketActorInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.Init(marketActorEntry);
        return popup;
    }

    public NewsPopup ShowNewsPopup(NewsState newsState)
    {
        NewsPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<NewsPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = InstantiatePopupPrefab(_newsPopupPrefab);
            popup = obj.GetComponent<NewsPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.Init(newsState);
        return popup;
    }

    public UnionPopup ShowUnionPopup()
    {
        UnionPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<UnionPopup>(true);
        }

        if (popup == null && _unionPopupPrefab != null)
        {
            GameObject obj = InstantiatePopupPrefab(_unionPopupPrefab);
            popup = obj.GetComponent<UnionPopup>();
        }

        if (popup == null)
        {
            Debug.LogWarning("[UIManager] Union popup prefab is missing.");
            return null;
        }

        popup.Init();
        return popup;
    }

    public NewsPopup ShowMainEventAnnouncementPopup(InitialMainEventModuleData moduleData)
    {
        if (moduleData == null)
        {
            return null;
        }

        NewsPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<NewsPopup>(true);
        }

        if (popup == null)
        {
            if (_newsPopupPrefab == null)
            {
                Debug.LogWarning("[UIManager] News popup prefab is missing.");
                return null;
            }

            GameObject obj = InstantiatePopupPrefab(_newsPopupPrefab);
            popup = obj.GetComponent<NewsPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.InitMainEventAnnouncement(moduleData);
        return popup;
    }

    public void SetCreditTopInfoToggleRect(RectTransform toggleButtonRect)
    {
        _creditTopInfoToggleRect = toggleButtonRect;
        ApplyCreditTopInfoToggleRect();
    }

    public CreditTopInfoPopup ToggleCreditTopInfoPopup()
    {
        CreditTopInfoPopup popup = GetOrCreateCreditTopInfoPopup();
        if (popup == null)
        {
            return null;
        }

        popup.ToggleCreditInfo();
        return popup;
    }

    private CreditTopInfoPopup GetOrCreateCreditTopInfoPopup()
    {
        CreditTopInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<CreditTopInfoPopup>(true);
        }

        if (popup == null && _creditTopInfoPopupPrefab != null)
        {
            GameObject obj = InstantiatePopupPrefab(_creditTopInfoPopupPrefab);
            popup = obj.GetComponent<CreditTopInfoPopup>();
            popup?.Init();
        }

        ApplyCreditTopInfoToggleRect(popup);
        return popup;
    }

    private void ApplyCreditTopInfoToggleRect()
    {
        if (_managerCanvasTransform == null)
        {
            return;
        }

        CreditTopInfoPopup popup = _managerCanvasTransform.GetComponentInChildren<CreditTopInfoPopup>(true);
        ApplyCreditTopInfoToggleRect(popup);
    }

    private void ApplyCreditTopInfoToggleRect(CreditTopInfoPopup popup)
    {
        if (popup == null || _creditTopInfoToggleRect == null)
        {
            return;
        }

        popup.SetToggleButtonRect(_creditTopInfoToggleRect);
    }

    public BuildingInfoPopup ShowBuildingInfoPopup(BuildingObject buildingObject)
    {
        BuildingInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<BuildingInfoPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = InstantiatePopupPrefab(_buildingInfoPopupPrefab);
            popup = obj.GetComponent<BuildingInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.ShowBuildingInfo(buildingObject);
        return popup;
    }

    public BuildingInfoPopup ShowBuildingInfoPopup(RawBuildingObject rawBuildingObject)
    {
        BuildingInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<BuildingInfoPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = InstantiatePopupPrefab(_buildingInfoPopupPrefab);
            popup = obj.GetComponent<BuildingInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.ShowRawBuildingInfo(rawBuildingObject);
        return popup;
    }

    public BuildingHelpPopup ShowBuildingHelpPopup(BuildingData buildingData)
    {
        BuildingHelpPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<BuildingHelpPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = InstantiatePopupPrefab(_buildingHelpPopupPrefab);
            popup = obj.GetComponent<BuildingHelpPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.Init(buildingData);
        return popup;
    }

    public ResourceHelpPopup ShowResourceHelpPopup(ResourceData resourceData)
    {
        ResourceHelpPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<ResourceHelpPopup>(true);
        }

        if (popup == null)
        {
            if (_resourceHelpPopupPrefab == null)
            {
                Debug.LogWarning("[UIManager] Resource help popup prefab is missing.");
                return null;
            }

            GameObject obj = InstantiatePopupPrefab(_resourceHelpPopupPrefab);
            popup = obj.GetComponent<ResourceHelpPopup>();
        }

        if (popup == null)
            return null;

        popup.gameObject.SetActive(true);
        popup.Init(resourceData);
        return popup;
    }

    public GoalPopup ShowGoalPopup()
    {
        GoalPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<GoalPopup>(true);
        }

        if (popup == null)
        {
            if (_goalPopupPrefab == null)
            {
                Debug.LogWarning("[UIManager] Goal popup prefab is missing.");
                return null;
            }

            GameObject obj = InstantiatePopupPrefab(_goalPopupPrefab);
            popup = obj.GetComponent<GoalPopup>();
        }

        if (popup == null)
            return null;

        popup.gameObject.SetActive(true);
        popup.Init();
        return popup;
    }

    public RawBuildingInfoPanel ShowRawBuildingInfoPanel(RawBuildingObject targetRawBuilding)
    {
        if (_rawBuildingInfoPanelPrefab == null)
        {
            Debug.LogWarning("[UIManager] RawBuildingInfoPanel prefab is not assigned.");
            return null;
        }

        // 메인 스크린 캔버스가 아닌 월드 캔버스 아래에 부모로 설정
        Transform parent = GameManager.Instance != null && GameManager.Instance.GetWorldCanvas() != null 
            ? GameManager.Instance.GetWorldCanvas() 
            : (_mainCanvas != null ? _mainCanvas.transform : _managerCanvasTransform);

        GameObject obj = Instantiate(_rawBuildingInfoPanelPrefab, parent, false);

        RawBuildingInfoPanel panel = obj.GetComponent<RawBuildingInfoPanel>();
        panel.Init(targetRawBuilding);
        return panel;
    }

    public GameObject CreateResourceImageContainer(Transform parent, string name, Vector3 worldPosition, float containerScale, Dictionary<string, int> resourceCounts)
    {
        GameObject container = Instantiate(_gridSortContentPrefab, parent);
        container.name = name;

        if (container.TryGetComponent(out RectTransform rect))
            rect.sizeDelta = new Vector2(200, 50);

        Transform t = container.transform;
        t.position = worldPosition;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one * containerScale;

        if (resourceCounts != null && resourceCounts.Count > 0)
            CreateResourceImages(container.transform, resourceCounts);

        return container;
    }

    public GameObject CreateResourceImage(Transform parent, ResourceEntry resourceEntry)
    {
        if (_resourceImagePrefab == null || parent == null || resourceEntry == null)
            return null;

        GameObject iconObj;
        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool != null)
        {
            iconObj = pool.GetPooledObject(_resourceImagePrefab);
            iconObj.transform.SetParent(parent, false);
        }
        else
        {
            iconObj = Instantiate(_resourceImagePrefab, parent);
        }

        if (iconObj.TryGetComponent(out RectTransform rect))
            rect.localScale = Vector3.one * _worldResourceImageScale;

        if (iconObj.TryGetComponent(out ResourceImage iconComponent))
            iconComponent.Init(resourceEntry);

        return iconObj;
    }

    public void CreateResourceImages(Transform parent, Dictionary<string, int> resourceCounts)
    {
        if (parent == null || resourceCounts == null)
            return;

        foreach (KeyValuePair<string, int> kvp in resourceCounts)
        {
            string resourceId = kvp.Key;
            if (string.IsNullOrEmpty(resourceId))
                continue;

            ResourceEntry entry = DataManager.Instance.Resource.GetResourceEntry(resourceId);
            if (entry != null)
                CreateResourceImage(parent, entry);
        }
    }

    public void RepopulateResourceImages(Transform parent, Dictionary<string, int> resourceCounts)
    {
        if (parent == null)
            return;

        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool != null)
            pool.ClearChildrenToPool(parent);
        else
            GameObjectUtils.ClearChildren(parent);

        if (resourceCounts == null || resourceCounts.Count == 0)
            return;

        CreateResourceImages(parent, resourceCounts);
    }

    public GameObject CreateProductionIconContainer(Transform parent, string name, Vector3 worldPosition, float containerScale, Dictionary<string, int> productionCounts)
    {
        return CreateResourceImageContainer(parent, name, worldPosition, containerScale, productionCounts);
    }

    public GameObject CreateProductionIcon(Transform parent, ResourceEntry resourceEntry, int amount)
    {
        if (_productionInfoImagePrefab == null || parent == null || resourceEntry == null) return null;

        GameObject iconObj;
        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool != null)
        {
            iconObj = pool.GetPooledObject(_productionInfoImagePrefab);
            iconObj.transform.SetParent(parent, false);
        }
        else
        {
            iconObj = Instantiate(_productionInfoImagePrefab, parent);
        }

        if (iconObj.TryGetComponent(out RectTransform rect))
            rect.localScale = Vector3.one * _productionIconScale;

        if (iconObj.TryGetComponent(out ResourceInfoImage iconComponent))
            iconComponent.Init(resourceEntry, amount);

        return iconObj;
    }

    public void CreateProductionIcons(Transform parent, Dictionary<string, int> productionCounts)
    {
        if (parent == null || productionCounts == null) return;

        foreach (KeyValuePair<string, int> kvp in productionCounts)
        {
            string resourceId = kvp.Key;
            int amount = kvp.Value;
            if (string.IsNullOrEmpty(resourceId)) continue;
            ResourceEntry entry = DataManager.Instance.Resource.GetResourceEntry(resourceId);
            if (entry != null)
                CreateProductionIcon(parent, entry, amount);
        }
    }

    /// <summary>
    /// parent 아래 ProductionInfoImage 자식을 풀로 반환한 뒤 productionCounts로 다시 채웁니다.
    /// </summary>
    public void RepopulateProductionInfoImages(Transform parent, Dictionary<string, int> productionCounts)
    {
        if (parent == null) return;

        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool != null)
            pool.ClearChildrenToPool(parent);
        else
            GameObjectUtils.ClearChildren(parent);

        if (productionCounts == null || productionCounts.Count == 0) return;
        CreateProductionIcons(parent, productionCounts);
    }

    public ActionBtn CreateActionButton(Transform parent, string label, Action onClick)
    {
        if (_actionBtnPrefab == null || parent == null) return null;

        GameObject btnObj;
        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool != null)
        {
            btnObj = pool.GetPooledObject(_actionBtnPrefab);
            btnObj.transform.SetParent(parent, false);
        }
        else
        {
            btnObj = Instantiate(_actionBtnPrefab, parent);
        }

        ActionBtn btn = btnObj.GetComponent<ActionBtn>();
        if (btn != null)
            btn.Init(label, onClick);
        return btn;
    }

    public TextPairPanel CreateEffectTextPairPanel(Transform parent, EffectState effectState, Color mainTextColor = default)
    {
        GameObject panelObj = GameManager.Instance.PoolingManager.GetPooledObject(_effectTextPairPanelPrefab);
        panelObj.transform.SetParent(parent, false);

        TextPairPanel panel = panelObj.GetComponent<TextPairPanel>();
        if (panel != null)
        {
            string mainText = effectState.id.Localize(LocalizationUtils.TABLE_EFFECT);
            string secondText = DataManager.Instance.Effect.FormatEffectValue(effectState.value, effectState.modifierType);
            panel.Init(mainText, secondText, effectState.value, mainTextColor);
        }
        return panel;
    }
}
