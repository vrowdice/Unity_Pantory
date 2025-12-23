using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class DesignUiManager : MonoBehaviour, IUIManager
{
    [SerializeField] private ThreadSaveInfoPanel _saveInfoPanel;
    [SerializeField] private BuildingTileManager _buildingTileManager;

    private GameManager _gameManager;
    private DataManager _dataManager;
    private GameObject _productionInfoImage;

    public Transform CanvasTrans => transform;
    public BuildingTileManager BuildingTileManager => _buildingTileManager;
    public GameManager GameManager => _gameManager;
    public DataManager GameDataManager => _dataManager;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public bool IsRemovalMode => _isRemovalMode;
    public GameObject ProductionInfoImage => _productionInfoImage;

    public void OnInitialize(GameManager argGameManager, DataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;
        _productionInfoImage = argGameManager.ProductionInfoImage;

        foreach (var buildingType in EnumUtils.GetAllEnumValues<BuildingType>())
        {
            var btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            var buildingTypeBtn = btn.GetComponent<BuildingTypeBtn>();
            if (buildingTypeBtn != null)
            {
                buildingTypeBtn.Initialize(this, buildingType);
                _buildingTypeBtns.Add(buildingTypeBtn);
            }
        }

        _saveInfoPanel.OnInitialize(_dataManager);
        InitializeThreadTitle();

        SelectBuildingType(BuildingType.Distribution);
    }

    public void UpdateAllMainText()
    {
        Debug.Log("[DesignUiManager] UpdateAllMainText called (placeholder)");
    }

    public void UpdateModeBtnImages(bool isPlacementMode, bool isRemovalMode)
    {
        _deselectBuildingBtnImage.color = isPlacementMode ? VisualManager.Instance.ValidColor : Color.white;
        _removalModeBtnImage.color = isRemovalMode ? VisualManager.Instance.InvalidColor : Color.white;
    }
}
