using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

/// <summary>
/// 연구 관리 패널
/// </summary>
public class ResearchCanvas : MainCanvasPanelBase
{
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;
    [SerializeField] private TextMeshProUGUI _researcherText;

    [SerializeField] private GameObject _researchBtnPrefab;
    [SerializeField] private GameObject _researchBtnContainerPrefab;
    [SerializeField] private Transform _researchBtnContainerContentTransform;

    [SerializeField] private GameObject _linePrefab;

    private Transform _lineParent;
    private List<Transform> _researchBtnContainerList = new();
    private Dictionary<string, RectTransform> _buttonMap = new();

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Research.OnResearchPointsChanged -= ResearchChanged;
        _dataManager.Research.OnResearchPointsChanged += ResearchChanged;

        _dataManager.Time.OnDayChanged -= ResearchChanged;
        _dataManager.Time.OnDayChanged += ResearchChanged;

        UpdateAllText();
        UpdateResearchScrollView();
    }

    public void ResearchChanged()
    {
        UpdateAllText();
        UpdateResearchScrollView();
    }

    public void UpdateResearchScrollView()
    {
        GameObjectUtils.ClearChildren(_researchBtnContainerContentTransform);
        GameObject lineParentObj = new GameObject("LineParent");
        lineParentObj.transform.SetParent(_researchBtnContainerContentTransform, false);
        _lineParent = lineParentObj.transform;
        _researchBtnContainerList.Clear();
        _buttonMap.Clear();

        Dictionary<string, int> depths = new Dictionary<string, int>();
        List<ResearchEntry> allEntries = _dataManager.Research.GetAllResearchEntries();

        foreach (ResearchEntry entry in allEntries)
        {
            if (entry.data.isDefaultUnlocked)
            {
                SetDepthRecursive(entry.data, 0, depths);
            }
        }

        StartCoroutine(DrawLinesStepByStep(allEntries));
    }

    private void SetDepthRecursive(ResearchData data, int currentDepth, Dictionary<string, int> depths)
    {
        if (depths.ContainsKey(data.id) && depths[data.id] >= currentDepth) return;
        depths[data.id] = currentDepth;

        for (int i = _researchBtnContainerList.Count; i <= currentDepth; i++)
        {
            GameObject containerObj = Instantiate(_researchBtnContainerPrefab, _researchBtnContainerContentTransform);
            containerObj.name = $"Layer_{i}";
            _researchBtnContainerList.Add(containerObj.transform);
        }

        if (!_buttonMap.ContainsKey(data.id))
        {
            ResearchEntry entry = _dataManager.Research.GetResearchEntry(data.id);
            Transform parentLayer = _researchBtnContainerList[currentDepth];
            GameObject btnObj = Instantiate(_researchBtnPrefab, parentLayer);

            _buttonMap.Add(data.id, btnObj.GetComponent<RectTransform>());

            ResearchBtn script = btnObj.GetComponent<ResearchBtn>();
            script.Init(entry);
        }

        foreach (ResearchData childData in data.unlockResearchList)
        {
            SetDepthRecursive(childData, currentDepth + 1, depths);
        }
    }

    private IEnumerator DrawLinesStepByStep(List<ResearchEntry> allEntries)
    {
        yield return new WaitForEndOfFrame();

        foreach (ResearchEntry entry in allEntries)
        {
            if (!_buttonMap.ContainsKey(entry.data.id)) continue;

            foreach (ResearchData childData in entry.data.unlockResearchList)
            {
                if (_buttonMap.ContainsKey(childData.id))
                {
                    ConnectButtons(_buttonMap[entry.data.id], _buttonMap[childData.id]);
                }
            }
        }
    }

    private void ConnectButtons(RectTransform start, RectTransform end)
    {
        GameObject lineObj = Instantiate(_linePrefab, _lineParent);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();

        Vector3 startPos = start.position;
        Vector3 endPos = end.position;
        Vector3 diff = endPos - startPos;

        lineRect.position = startPos + (diff * 0.5f);
        lineRect.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        lineRect.sizeDelta = new Vector2(diff.magnitude, 5f);
    }

    private void UpdateAllText()
    {
        long researchPoints = _dataManager.Research.ResearchPoint;
        _researchText.text = ReplaceUtils.FormatNumberWithCommas(researchPoints);
        long deltaResearch = _dataManager.Research.CalculateDailyRPProduction();
        string sign = deltaResearch > 0 ? " + " : " ";
        _deltaResearchText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaResearch)}";
        VisualManager visualManager = VisualManager.Instance;
        _deltaResearchText.color = visualManager.GetDeltaColor(deltaResearch);
        _researcherText.text = _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Researcher).ToString();
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