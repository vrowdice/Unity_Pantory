using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    private readonly List<Action> _closeStack = new List<Action>();

    private Transform _managerCanvasTransform;

    public Transform ManagerCanvasTransform => _managerCanvasTransform;
    public GameObject ProductionInfoImagePrefab => _productionInfoImagePrefab;
    public GameObject TextPairPanelPrefab => _textPairPanelPrefab;
    public GameObject ActionBtnPrefab => _actionBtnPrefab;
    public GameObject GridSortContentPrefab => _gridSortContentPrefab;
    public GameObject EffectTextPairPanelPrefab => _effectTextPairPanelPrefab;
    public float ProductionIconScale => _productionIconScale;

    [Header("Common Panel")]
    [SerializeField] private GameObject _optionPanelPrefab;
    [SerializeField] private GameObject _warningPanelPrefab;
    [SerializeField] private GameObject _confirmPanelPrefab;
    [SerializeField] private GameObject _enterNamePanelPrefab;
    [SerializeField] private GameObject _selectResourcePanelPrefab;
    [SerializeField] private GameObject _saveLoadPopupPrefab;
    [SerializeField] private GameObject _tutorialPopupPrefab;

    [Header("Main Info Panels")]
    [SerializeField] private GameObject _creditTopInfoPopupPrefab;
    [SerializeField] private GameObject _researchInfoPopupPrefab;
    [SerializeField] private GameObject _marketActorInfoPopupPrefab;
    [SerializeField] private GameObject _newsPopupPrefab;
    [SerializeField] private GameObject _buildingInfoPopupPrefab;

    [Header("Common UI")]
    [SerializeField] private GameObject _productionInfoImagePrefab;
    [SerializeField] private GameObject _gridSortContentPrefab;
    [SerializeField] private GameObject _textPairPanelPrefab;
    [SerializeField] private GameObject _actionBtnPrefab;
    [SerializeField] private GameObject _effectTextPairPanelPrefab;

    [Header("Production Icon Settings")]
    [SerializeField] private float _productionIconScale = 1.0f;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
    }

    private void Update()
    {
        if (Instance != this) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            TryCloseTopmost();
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
    /// GameManager에서 매니저 생성 후 호출합니다. ManagerCanvas를 생성합니다.
    /// </summary>
    public void Init()
    {
        CreateManagerCanvas();
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

    public void ShowWarningPopup(string messageKey)
    {
        GameObject warningPanelObj = GameManager.Instance.PoolingManager.GetPooledObject(_warningPanelPrefab);
        warningPanelObj.transform.SetParent(_managerCanvasTransform, false);
        warningPanelObj.GetComponent<WarningPopup>().Init(messageKey);
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
            GameObject selectResourcePanelObj = Instantiate(_selectResourcePanelPrefab, _managerCanvasTransform, false);
            panel = selectResourcePanelObj.GetComponent<SelectResourcePopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init(DataManager.Instance, resourceTypes, onResourceSelected, producibleResources);
        return panel;
    }

    public ConfirmPopup ShowConfirmPopup(string messageKey, Action onConfirm)
    {
        GameObject panelObj = Instantiate(_confirmPanelPrefab, _managerCanvasTransform, false);
        ConfirmPopup panel = panelObj.GetComponent<ConfirmPopup>();
        panel.Init(messageKey, onConfirm);
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
            GameObject panelObj = Instantiate(_enterNamePanelPrefab, _managerCanvasTransform, false);
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
            GameObject panelObj = Instantiate(_optionPanelPrefab, _managerCanvasTransform, false);
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
        {
            panel = _managerCanvasTransform.GetComponentInChildren<TutorialPopup>(true);
        }

        if (panel == null)
        {
            GameObject panelObj = Instantiate(_tutorialPopupPrefab, _managerCanvasTransform, false);
            panel = panelObj.GetComponent<TutorialPopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init(tutorialDataList, gameObjectName);
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
            GameObject panelObj = Instantiate(_saveLoadPopupPrefab, _managerCanvasTransform, false);
            panel = panelObj.GetComponent<SaveLoadPopup>();
        }

        panel.gameObject.SetActive(true);
        panel.Init(isSaveMode);
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
            GameObject obj = Instantiate(_researchInfoPopupPrefab, _managerCanvasTransform, false);
            popup = obj.GetComponent<ResearchInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.Init(researchEntry);
        return popup;
    }

    public MarketActorInfoPopup ShowMarketActorInfoPopup(MarketActorEntry marketActorEntry, MainCanvas mainCanvas)
    {
        MarketActorInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<MarketActorInfoPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = Instantiate(_marketActorInfoPopupPrefab, _managerCanvasTransform, false);
            popup = obj.GetComponent<MarketActorInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.Init(marketActorEntry, mainCanvas);
        return popup;
    }

    public NewsPopup ShowNewsPopup(NewsState newsState, MainCanvas mainCanvas)
    {
        NewsPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<NewsPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = Instantiate(_newsPopupPrefab, _managerCanvasTransform, false);
            popup = obj.GetComponent<NewsPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.Init(newsState, mainCanvas);
        return popup;
    }

    public CreditTopInfoPopup ShowCreditTopInfoPopup()
    {
        CreditTopInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<CreditTopInfoPopup>(true);
        }

        if (popup == null && _creditTopInfoPopupPrefab != null)
        {
            GameObject obj = Instantiate(_creditTopInfoPopupPrefab, _managerCanvasTransform, false);
            popup = obj.GetComponent<CreditTopInfoPopup>();
        }

        if (popup != null)
        {
            popup.Init();
            popup.gameObject.SetActive(true);
            popup.ShowCreditInfo();
        }
        return popup;
    }

    public BuildingInfoPopup ShowBuildingInfoPopup(BuildingData buildingData, BuildingState buildingState)
    {
        BuildingInfoPopup popup = null;
        if (_managerCanvasTransform != null)
        {
            popup = _managerCanvasTransform.GetComponentInChildren<BuildingInfoPopup>(true);
        }

        if (popup == null)
        {
            GameObject obj = Instantiate(_buildingInfoPopupPrefab, _managerCanvasTransform, false);
            popup = obj.GetComponent<BuildingInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.ShowBuildingInfo(buildingData, buildingState);
        return popup;
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
            GameObject obj = Instantiate(_buildingInfoPopupPrefab, _managerCanvasTransform, false);
            popup = obj.GetComponent<BuildingInfoPopup>();
        }

        popup.gameObject.SetActive(true);
        popup.ShowBuildingInfo(buildingObject);
        return popup;
    }

    public GameObject CreateProductionIconContainer(Transform parent, string name, Vector3 worldPosition, float containerScale, Dictionary<string, int> productionCounts)
    {
        GameObject container = Instantiate(_gridSortContentPrefab, parent);
        container.name = name;

        if (container.TryGetComponent(out RectTransform rect))
            rect.sizeDelta = new Vector2(200, 50);

        Transform t = container.transform;
        t.position = worldPosition;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one * containerScale;

        if (productionCounts != null && productionCounts.Count > 0)
            CreateProductionIcons(container.transform, productionCounts);

        return container;
    }

    public GameObject CreateProductionIcon(Transform parent, ResourceEntry resourceEntry, int amount)
    {
        GameObject iconObj = GameManager.Instance.PoolingManager.GetPooledObject(_productionInfoImagePrefab);
        iconObj.transform.SetParent(parent, false);

        if (iconObj.TryGetComponent(out RectTransform rect))
            rect.localScale = Vector3.one * _productionIconScale;

        if (iconObj.TryGetComponent(out ProductionInfoImage iconComponent))
            iconComponent.Init(resourceEntry, amount);

        return iconObj;
    }

    public void CreateProductionIcons(Transform parent, Dictionary<string, int> productionCounts)
    {
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

    public ActionBtn CreateActionButton(Transform parent, string label, Action onClick)
    {
        GameObject btnObj = Instantiate(_actionBtnPrefab, parent);
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
