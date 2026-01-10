using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResearchTierPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _tierText;
    [SerializeField] private Image _allResearchClearImage;
    [SerializeField] private Image _dividerImage;
    [SerializeField] private Transform _researchButtonContentTransform;
    [SerializeField] private GameObject _researchBtnPrefab;

    public void Init(int tier, List<ResearchEntry> researchEntry, MainCanvas mainUiManager)
    {
        _tierText.text = $"Tier {tier}";

        foreach (ResearchEntry item in researchEntry)
        {
            GameObject btnObj = Instantiate(_researchBtnPrefab, _researchButtonContentTransform);
            ResearchBtn researhBtn = btnObj.GetComponent<ResearchBtn>();

            researhBtn.Init(item, mainUiManager);
        }
    }
}
