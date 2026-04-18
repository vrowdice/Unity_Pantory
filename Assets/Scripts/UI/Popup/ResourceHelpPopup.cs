using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceHelpPopup : PopupBase
{
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceTypeText;
    [SerializeField] private TextMeshProUGUI _resourceDescriptionText;
    [SerializeField] private Image _resourceImage;

    [SerializeField] private Transform _neededResourceContent;

    public void Init(ResourceData resourceData)
    {
        base.Init();
        if (resourceData == null)
        {
            Close();
            return;
        }

        _resourceNameText.text = resourceData.id.Localize(LocalizationUtils.TABLE_RESOURCE);
        _resourceTypeText.text = resourceData.type.ToString().Localize(LocalizationUtils.TABLE_RESOURCE);
        _resourceDescriptionText.text = (resourceData.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_RESOURCE);
        _resourceImage.sprite = resourceData.icon;

        PopulateNeededResources(resourceData);
        Show();
    }

    private void PopulateNeededResources(ResourceData resourceData)
    {
        Dictionary<string, int> neededCounts = new Dictionary<string, int>();
        if (resourceData.requirements != null)
        {
            for (int i = 0; i < resourceData.requirements.Count; i++)
            {
                ResourceRequirement req = resourceData.requirements[i];
                if (req == null || req.resource == null || string.IsNullOrEmpty(req.resource.id))
                    continue;

                int amount = Mathf.Max(1, req.count);
                string id = req.resource.id;
                if (neededCounts.TryGetValue(id, out int existing))
                    neededCounts[id] = existing + amount;
                else
                    neededCounts[id] = amount;
            }
        }

        UIManager.Instance.RepopulateProductionInfoImages(_neededResourceContent, neededCounts);
    }
}
