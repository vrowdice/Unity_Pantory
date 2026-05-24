using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 연구 관리 패널
/// </summary>
public class ResearchCanvas : MainCanvasPanelBase
{
    [Header("Research Type Buttons")]
    [SerializeField] private Transform _researchActionBtnContent;

    [SerializeField] private TextMeshProUGUI _researchPointText;
    [SerializeField] private TextMeshProUGUI _researcherText;

    [SerializeField] private GameObject _researchBtnPrefab;
    [SerializeField] private GameObject _researchBtnContainerPrefab;
    [SerializeField] private Transform _researchBtnContainerContentTransform;

    [SerializeField] private GameObject _linePrefab;
    
    [SerializeField] private Transform _researchEffectScrollViewContentTransform;

    private RectTransform _lineParent;
    private List<Transform> _researchBtnContainerList = new List<Transform>();
    private Dictionary<string, RectTransform> _buttonMap = new Dictionary<string, RectTransform>();
    private List<ActionBtn> _researchTypeButtons = new List<ActionBtn>();
    private ResearchType _selectedResearchType = ResearchType.unlock_building;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Research.OnResearchPointsChanged -= ResearchChanged;
        _dataManager.Research.OnResearchPointsChanged += ResearchChanged;

        _dataManager.Time.OnDayChanged -= ResearchChanged;
        _dataManager.Time.OnDayChanged += ResearchChanged;

        SetupResearchTypeButtons();

        List<ResearchType> researchTypes = EnumUtils.GetAllEnumValues<ResearchType>();
        if (researchTypes.Count > 0)
        {
            OnResearchTypeClick(researchTypes[0]);
        }
        else
        {
            UpdateResearchScrollView();
        }

        UpdateAllText();
        UpdateResearchEffectStatus();
    }

    public void ResearchChanged()
    {
        UpdateAllText();
        UpdateResearchEffectStatus();
        UpdateResearchScrollView();
    }

    /// <summary>
    /// 연구 카테고리(ResearchType) 선택 시 호출
    /// </summary>
    public void OnResearchTypeClick(ResearchType researchType)
    {
        _selectedResearchType = researchType;
        UpdateResearchScrollView();
        UpdateResearchTypeButtonHighlight();
    }

    private void SetupResearchTypeButtons()
    {
        if (_panelUIManager?.ActionBtnPrefab == null || _researchActionBtnContent == null)
        {
            return;
        }

        int targetCount = EnumUtils.GetAllEnumValues<ResearchType>().Count;
        if (_researchActionBtnContent.childCount == targetCount)
        {
            _researchTypeButtons.Clear();
            foreach (Transform child in _researchActionBtnContent)
            {
                ActionBtn btn = child.GetComponent<ActionBtn>();
                if (btn != null)
                {
                    _researchTypeButtons.Add(btn);
                }
            }

            UpdateResearchTypeButtonHighlight();
            return;
        }

        _gameManager.PoolingManager.ClearChildrenToPool(_researchActionBtnContent);
        _researchTypeButtons.Clear();

        foreach (ResearchType researchType in EnumUtils.GetAllEnumValues<ResearchType>())
        {
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_panelUIManager.ActionBtnPrefab);
            btnObj.transform.SetParent(_researchActionBtnContent, false);
            ActionBtn btn = btnObj.GetComponent<ActionBtn>();
            if (btn != null)
            {
                ResearchType capturedType = researchType;
                string localizedName = capturedType.Localize(LocalizationUtils.TABLE_RESEARCH);
                btn.Init(localizedName, () =>
                {
                    OnResearchTypeClick(capturedType);
                });
                _researchTypeButtons.Add(btn);
            }
        }

        UpdateResearchTypeButtonHighlight();
    }

    private void UpdateResearchTypeButtonHighlight()
    {
        if (_researchTypeButtons.Count == 0)
        {
            return;
        }

        List<ResearchType> types = EnumUtils.GetAllEnumValues<ResearchType>();
        for (int i = 0; i < types.Count && i < _researchTypeButtons.Count; i++)
        {
            _researchTypeButtons[i].SetHighlight(_selectedResearchType == types[i]);
        }
    }

    public void UpdateResearchScrollView()
    {
        _gameManager.PoolingManager.ClearChildrenToPool(_researchBtnContainerContentTransform);
        GameObject lineParentObj = new GameObject("LineParent", typeof(RectTransform));
        RectTransform lineParentRect = lineParentObj.GetComponent<RectTransform>();
        lineParentRect.SetParent(_researchBtnContainerContentTransform, false);
        lineParentRect.anchorMin = Vector2.zero;
        lineParentRect.anchorMax = Vector2.one;
        lineParentRect.offsetMin = Vector2.zero;
        lineParentRect.offsetMax = Vector2.zero;
        lineParentRect.SetAsFirstSibling();
        _lineParent = lineParentRect;
        _researchBtnContainerList.Clear();
        _buttonMap.Clear();

        Dictionary<string, int> depths = new Dictionary<string, int>();
        List<ResearchEntry> allEntries = _dataManager.Research.GetAllResearchEntries();

        foreach (ResearchEntry entry in allEntries)
        {
            if (entry.data == null)
            {
                continue;
            }

            if (entry.data.isDefaultUnlocked && entry.data.researchType == _selectedResearchType)
            {
                SetDepthRecursive(entry.data, 0, depths);
            }
        }

        StartCoroutine(DrawLinesStepByStep(allEntries));
    }

    private void SetDepthRecursive(ResearchData data, int currentDepth, Dictionary<string, int> depths)
    {
        if (data == null || data.researchType != _selectedResearchType)
        {
            return;
        }

        ResearchEntry entry = _dataManager.Research.GetResearchEntry(data.id);
        if (entry == null)
        {
            return;
        }

        if (depths.ContainsKey(data.id) && depths[data.id] >= currentDepth)
        {
            return;
        }

        depths[data.id] = currentDepth;

        for (int i = _researchBtnContainerList.Count; i <= currentDepth; i++)
        {
            GameObject containerObj = _gameManager.PoolingManager.GetPooledObject(_researchBtnContainerPrefab);
            containerObj.transform.SetParent(_researchBtnContainerContentTransform, false);
            containerObj.name = $"Layer_{i}";
            _researchBtnContainerList.Add(containerObj.transform);
        }

        if (!_buttonMap.ContainsKey(data.id))
        {
            Transform parentLayer = _researchBtnContainerList[currentDepth];
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_researchBtnPrefab);
            btnObj.transform.SetParent(parentLayer, false);

            _buttonMap.Add(data.id, btnObj.GetComponent<RectTransform>());

            ResearchBtn script = btnObj.GetComponent<ResearchBtn>();
            script.Init(entry, this);
        }

        if (data.unlockResearchList == null)
        {
            return;
        }

        foreach (ResearchData childData in data.unlockResearchList)
        {
            if (childData == null || childData.researchType != _selectedResearchType)
            {
                continue;
            }

            SetDepthRecursive(childData, currentDepth + 1, depths);
        }
    }

    private IEnumerator DrawLinesStepByStep(List<ResearchEntry> allEntries)
    {
        yield return new WaitForEndOfFrame();

        foreach (ResearchEntry entry in allEntries)
        {
            if (entry.data == null || !_buttonMap.ContainsKey(entry.data.id))
            {
                continue;
            }

            if (entry.data.unlockResearchList == null)
            {
                continue;
            }

            foreach (ResearchData childData in entry.data.unlockResearchList)
            {
                if (childData != null && _buttonMap.ContainsKey(childData.id))
                {
                    ConnectButtons(_buttonMap[entry.data.id], _buttonMap[childData.id]);
                }
            }
        }
    }

    private void ConnectButtons(RectTransform start, RectTransform end)
    {
        GameObject lineObj = _gameManager.PoolingManager.GetPooledObject(_linePrefab);
        lineObj.transform.SetParent(_lineParent, false);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();

        Vector2 localStart = _lineParent.InverseTransformPoint(start.position);
        Vector2 localEnd = _lineParent.InverseTransformPoint(end.position);
        Vector2 diff = localEnd - localStart;
        float distance = diff.magnitude;

        if (distance < 0.01f)
        {
            return;
        }

        float lineHeight = lineRect.sizeDelta.y;
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = localStart + diff * 0.5f;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        lineRect.sizeDelta = new Vector2(distance, lineHeight);
    }

    private void UpdateAllText()
    {
        long researchPoints = _dataManager.Research.ResearchPoint;
        long deltaResearch = _dataManager.Research.CalculateDailyRPProduction();
        _researchPointText.text = $"{ReplaceUtils.FormatNumberWithCommas(researchPoints)} +{ReplaceUtils.FormatNumberWithCommas(deltaResearch)}";
        _researchPointText.color = _visualManager.GetDeltaColor(deltaResearch);
        _researcherText.text = _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Researcher).ToString();
    }

    private void UpdateResearchEffectStatus()
    {
        if (_researchEffectScrollViewContentTransform == null)
        {
            return;
        }

        _gameManager.PoolingManager.ClearChildrenToPool(_researchEffectScrollViewContentTransform);

        if (_dataManager?.Effect == null)
        {
            return;
        }

        List<EffectState> effects = _dataManager.Effect.GetEffectStatEffects(
            EffectTargetType.Research,
            EffectStatType.Research_RPProduction
        );

        foreach (EffectState effectState in effects)
        {
            if (effectState == null) continue;
            _panelUIManager.CreateEffectTextPairPanel(_researchEffectScrollViewContentTransform, effectState);
        }
    }

    private void OnDisable()
    {
        if (_dataManager != null)
        {
            _dataManager.Research.OnResearchPointsChanged -= ResearchChanged;
            _dataManager.Time.OnDayChanged -= ResearchChanged;
        }
    }
}
