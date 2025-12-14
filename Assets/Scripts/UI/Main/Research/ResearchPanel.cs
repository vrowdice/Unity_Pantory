using UnityEngine;
using TMPro;

/// <summary>
/// 연구 관리 패널
/// </summary>
public class ResearchPanel : BasePanel
{
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;

    [SerializeField] private GameObject _researchTierPanelPrefab;

    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[ResearchPanel] DataManager is null.");
            return;
        }
    }
}

