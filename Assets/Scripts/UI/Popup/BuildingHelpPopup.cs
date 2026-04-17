using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingHelpPopup : PopupBase
{
    [SerializeField] private TextMeshProUGUI _buildingNameText;
    [SerializeField] private TextMeshProUGUI _buildingTypeText;
    [SerializeField] private TextMeshProUGUI _buildingDescriptionText;
    [SerializeField] private Image _buildingImage;
    [SerializeField] private Toggle _IsNeededExpertToggle;

    [SerializeField] private TextMeshProUGUI _buildCostText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _requiredEmployeeText;

    [SerializeField] private Transform _outputResourceContent;

    public void Init(BuildingData buildingData)
    {
        base.Init();
        if (buildingData == null)
        {
            Close();
            return;
        }

        _buildingNameText.text = buildingData.id.Localize(LocalizationUtils.TABLE_BUILDING);
        _buildingTypeText.text = buildingData.buildingType.ToString().Localize(LocalizationUtils.TABLE_BUILDING);
        _buildingDescriptionText.text = (buildingData.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_BUILDING);
        _buildingImage.sprite = buildingData.icon != null ? buildingData.icon : buildingData.buildingSprite;
        _IsNeededExpertToggle.isOn = buildingData.isProfessional;

        _buildCostText.text = ReplaceUtils.FormatNumberWithCommas(buildingData.buildCost);
        _maintenanceText.text = ReplaceUtils.FormatNumberWithCommas(buildingData.maintenanceCost);
        _requiredEmployeeText.text = buildingData.requiredEmployees.ToString();

        PopulateOutputResources(buildingData);
        Show();
    }

    private void PopulateOutputResources(BuildingData buildingData)
    {
        Dictionary<string, int> outputCounts = new Dictionary<string, int>();
        if (buildingData is ProductionBuildingData productionBuildingData &&
            productionBuildingData.ProducibleResources != null)
        {
            for (int i = 0; i < productionBuildingData.ProducibleResources.Count; i++)
            {
                ResourceData resourceData = productionBuildingData.ProducibleResources[i];
                if (resourceData == null) continue;

                Dictionary<string, int> batchOutputs = resourceData.GetBatchOutputCounts();
                foreach (KeyValuePair<string, int> kvp in batchOutputs)
                {
                    if (string.IsNullOrEmpty(kvp.Key)) continue;
                    if (outputCounts.TryGetValue(kvp.Key, out int existingCount))
                    {
                        outputCounts[kvp.Key] = existingCount + kvp.Value;
                    }
                    else
                    {
                        outputCounts[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        UIManager.Instance.RepopulateProductionInfoImages(_outputResourceContent, outputCounts);
    }
}
