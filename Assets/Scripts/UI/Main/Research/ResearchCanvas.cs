using System;
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

    [Header("Complete Feedback")]
    [SerializeField] private AudioClip _researchCompleteSfx;

    private RectTransform _lineParent;
    private List<Transform> _researchBtnContainerList = new List<Transform>();
    private readonly Dictionary<string, ResearchBtn> _researchBtnMap = new Dictionary<string, ResearchBtn>();
    private List<ActionBtn> _researchTypeButtons = new List<ActionBtn>();
    private ResearchType _selectedResearchType = ResearchType.unlock_building;
    private readonly List<Action> _researchSpawnActions = new List<Action>();
    private Coroutine _researchTypeButtonCoroutine;
    private Coroutine _researchScrollCoroutine;
    private Coroutine _researchEffectCoroutine;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Research.OnResearchPointsChanged -= ResearchChanged;
        _dataManager.Research.OnResearchPointsChanged += ResearchChanged;

        _dataManager.Time.OnDayChanged -= ResearchChanged;
        _dataManager.Time.OnDayChanged += ResearchChanged;

        _dataManager.OnResearchCompleted -= HandleResearchCompleted;
        _dataManager.OnResearchCompleted += HandleResearchCompleted;

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
        RefreshResearchButtons();
        UpdateResearchEffectStatus();
    }

    private void HandleResearchCompleted(string researchId)
    {
        if (!gameObject.activeInHierarchy || string.IsNullOrEmpty(researchId))
            return;

        if (!_researchBtnMap.TryGetValue(researchId, out ResearchBtn researchBtn))
            return;

        Transform target = researchBtn.GetCompleteAnimationTarget();
        if (ResearchInfoPopup.ConsumeCanvasCompleteSfxSkip())
            RequirementCompleteFeedbackUtils.PlayButtonAnimation(target);
        else
            RequirementCompleteFeedbackUtils.Play(target, _researchCompleteSfx);
    }

    private void RefreshResearchButtons()
    {
        if (_researchBtnMap.Count == 0)
            return;

        foreach (ResearchEntry entry in _dataManager.Research.GetAllResearchEntries())
        {
            if (entry?.data == null)
                continue;

            if (_researchBtnMap.TryGetValue(entry.data.id, out ResearchBtn researchBtn))
                researchBtn.Refresh();
        }
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

        List<ResearchType> researchTypes = EnumUtils.GetAllEnumValues<ResearchType>();
        StaggeredSpawnUtils.Restart(this, ref _researchTypeButtonCoroutine, CreateResearchTypeButtonsRoutine(researchTypes));
        UpdateResearchTypeButtonHighlight();
    }

    private IEnumerator CreateResearchTypeButtonsRoutine(List<ResearchType> researchTypes)
    {
        yield return StaggeredSpawnUtils.ForEachFrame(researchTypes.Count, i =>
        {
            ResearchType researchType = researchTypes[i];
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_panelUIManager.ActionBtnPrefab);
            btnObj.transform.SetParent(_researchActionBtnContent, false);
            ActionBtn btn = btnObj.GetComponent<ActionBtn>();
            if (btn != null)
            {
                ResearchType capturedType = researchType;
                string localizedName = capturedType.Localize(LocalizationUtils.TABLE_RESEARCH);
                btn.Init(localizedName, () => OnResearchTypeClick(capturedType));
                _researchTypeButtons.Add(btn);
            }
        });
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
        StaggeredSpawnUtils.Restart(this, ref _researchScrollCoroutine, UpdateResearchScrollViewRoutine());
    }

    private IEnumerator UpdateResearchScrollViewRoutine()
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
        _researchBtnMap.Clear();
        _researchSpawnActions.Clear();

        Dictionary<string, int> depths = new Dictionary<string, int>();
        HashSet<string> plannedButtonIds = new HashSet<string>();
        List<ResearchEntry> allEntries = _dataManager.Research.GetAllResearchEntries();

        foreach (ResearchEntry entry in allEntries)
        {
            if (entry.data == null)
                continue;

            if (entry.data.isDefaultUnlocked && entry.data.researchType == _selectedResearchType)
                QueueDepthRecursive(entry.data, 0, depths, plannedButtonIds);
        }

        yield return StaggeredSpawnUtils.ForEachFrame(_researchSpawnActions.Count, i => _researchSpawnActions[i]());

        yield return DrawLinesStepByStep(allEntries);
    }

    private void QueueDepthRecursive(ResearchData data, int currentDepth, Dictionary<string, int> depths, HashSet<string> plannedButtonIds)
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

        int depth = currentDepth;
        _researchSpawnActions.Add(() =>
        {
            for (int i = _researchBtnContainerList.Count; i <= depth; i++)
            {
                GameObject containerObj = _gameManager.PoolingManager.GetPooledObject(_researchBtnContainerPrefab);
                containerObj.transform.SetParent(_researchBtnContainerContentTransform, false);
                containerObj.name = $"Layer_{i}";
                _researchBtnContainerList.Add(containerObj.transform);
            }
        });

        if (!plannedButtonIds.Contains(data.id))
        {
            plannedButtonIds.Add(data.id);
            string researchId = data.id;
            ResearchEntry capturedEntry = entry;
            _researchSpawnActions.Add(() =>
            {
                if (_researchBtnMap.ContainsKey(researchId))
                    return;

                Transform parentLayer = _researchBtnContainerList[depth];
                GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_researchBtnPrefab);
                btnObj.transform.SetParent(parentLayer, false);

                ResearchBtn script = btnObj.GetComponent<ResearchBtn>();
                _researchBtnMap.Add(researchId, script);
                script.Init(capturedEntry, this);
            });
        }

        if (data.unlockResearchList == null)
            return;

        foreach (ResearchData childData in data.unlockResearchList)
        {
            if (childData == null || childData.researchType != _selectedResearchType)
                continue;

            QueueDepthRecursive(childData, currentDepth + 1, depths, plannedButtonIds);
        }
    }

    private IEnumerator DrawLinesStepByStep(List<ResearchEntry> allEntries)
    {
        yield return new WaitForEndOfFrame();

        foreach (ResearchEntry entry in allEntries)
        {
            if (entry.data == null || !_researchBtnMap.ContainsKey(entry.data.id))
                continue;

            if (entry.data.unlockResearchList == null)
                continue;

            RectTransform start = _researchBtnMap[entry.data.id].GetComponent<RectTransform>();
            foreach (ResearchData childData in entry.data.unlockResearchList)
            {
                if (childData != null && _researchBtnMap.ContainsKey(childData.id))
                {
                    RectTransform end = _researchBtnMap[childData.id].GetComponent<RectTransform>();
                    ConnectButtons(start, end);
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
        StaggeredSpawnUtils.Restart(this, ref _researchEffectCoroutine, UpdateResearchEffectStatusRoutine());
    }

    private IEnumerator UpdateResearchEffectStatusRoutine()
    {
        if (_researchEffectScrollViewContentTransform == null)
            yield break;

        _gameManager.PoolingManager.ClearChildrenToPool(_researchEffectScrollViewContentTransform);

        if (_dataManager?.Effect == null)
            yield break;

        List<EffectState> effects = _dataManager.Effect.GetEffectStatEffects(
            EffectTargetType.Research,
            EffectStatType.Research_RPProduction
        );

        yield return StaggeredSpawnUtils.ForEachFrame(effects.Count, i =>
        {
            EffectState effectState = effects[i];
            if (effectState == null)
                return;

            _panelUIManager.CreateEffectTextPairPanel(_researchEffectScrollViewContentTransform, effectState);
        });
    }

    protected override void OnDisable()
    {
        StaggeredSpawnUtils.Stop(this, ref _researchTypeButtonCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _researchScrollCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _researchEffectCoroutine);
        base.OnDisable();

        if (_dataManager != null)
        {
            _dataManager.Research.OnResearchPointsChanged -= ResearchChanged;
            _dataManager.Time.OnDayChanged -= ResearchChanged;
            _dataManager.OnResearchCompleted -= HandleResearchCompleted;
        }
    }
}
