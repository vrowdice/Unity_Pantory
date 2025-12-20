using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public partial class DesignUiManager
{
    [Header("Mode References")]
    [SerializeField] private Image _deselectBuildingBtnImage;
    [SerializeField] private Image _removalModeBtnImage;

    [Header("UI Prefabs & Contents")]
    [SerializeField] private GameObject _buildingTypeBtnPrefab;
    [SerializeField] private Transform _buildingTypeBtnContent;
    [SerializeField] private GameObject _buildingBtnPrefab;
    [SerializeField] private Transform _buildingBtnContent;

    [Header("Panel References")]
    [SerializeField] private BuildingInfoPanel _buildingInfoPanel;

    private bool _isRemovalMode;
    private BuildingType _selectedBuildingType = BuildingType.Distribution;

    private BuildingData _selectedBuilding;

    private List<BuildingData> _buildingDataList;
    private List<BuildingTypeBtn> _buildingTypeBtns = new List<BuildingTypeBtn>();
    private List<BuildingBtn> _buildingBtns = new List<BuildingBtn>();

    public void SelectBuilding(BuildingData buildingData, bool isSelected, bool isUnlocked)
    {
        if (!isUnlocked)
        {
            _gameManager.ShowWarningPanel("This building is locked. Complete the required research to unlock it.");
            return;
        }

        _selectedBuilding = buildingData;
        _isRemovalMode = false;

        if (_buildingTileManager.IsRemovalMode)
        {
            _buildingTileManager.RemovalHandler.CancelRemoval();
        }

        if (isSelected)
        {
            DeselectBuilding();
        }
        else
        {
            _buildingTileManager.StartPlacementMode(buildingData);
        }
        
        UpdateBuildingButtonStates();
    }

    public void DeselectBuilding()
    {
        _selectedBuilding = null;

        _buildingTileManager.PlacementHandler.CancelPlacement();

        UpdateModeBtnImages(false, false);
        UpdateBuildingButtonStates();
    }

    public void StartRemovalMode()
    {
        _isRemovalMode = true;
        _selectedBuilding = null;

        _buildingTileManager.PlacementHandler.CancelPlacement();
        _buildingTileManager.RemovalHandler.StartRemoval();

        UpdateModeBtnImages(false, true);
        UpdateBuildingButtonStates();
    }

    public void CancelRemovalMode()
    {
        _isRemovalMode = false;
        _buildingTileManager.RemovalHandler.CancelRemoval();

        UpdateModeBtnImages(false, false);
        UpdateBuildingButtonStates();
    }

    public void RemovalModeToggle()
    {
        if (_isRemovalMode)
        {
            CancelRemovalMode();
        }
        else
        {
            StartRemovalMode();
        }
    }

    public void SelectBuildingType(BuildingType buildingType)
    {
        GameObjectUtils.ClearChildren(_buildingBtnContent);
        _buildingBtns.Clear();

        _selectedBuildingType = buildingType;
        _buildingDataList = _dataManager.Building.GetBuildingDataList(buildingType);

        foreach (BuildingData data in _buildingDataList)
        {
            bool isUnlocked = true;

            if (data.requiredResearch != null)
            {
                isUnlocked = _dataManager.Research.IsResearchCompleted(data.requiredResearch.id) || data.isUnlockedByDefault;
            }
            else
            {
                isUnlocked = true;
            }

            GameObject btn = Instantiate(_buildingBtnPrefab, _buildingBtnContent);
            BuildingBtn buildingBtn = btn.GetComponent<BuildingBtn>();

            buildingBtn.Initialize(this, data, isUnlocked);
            _buildingBtns.Add(buildingBtn);
        }

        UpdateBuildingTypeButtonStates();
        UpdateBuildingButtonStates();
    }

    private void UpdateBuildingTypeButtonStates()
    {
        foreach (BuildingTypeBtn btn in _buildingTypeBtns)
        {
            if (btn.BuildingType == _selectedBuildingType)
            {
                btn.SetFocused(true);
            }
            else
            {
                btn.SetFocused(false);
            }
        }
    }

    private void UpdateBuildingButtonStates()
    {
        foreach (BuildingBtn btn in _buildingBtns)
        {
            if(_selectedBuilding == btn.BuildingData)
            {
                btn.SetSelected(true);
            }
            else
            {
                btn.SetSelected(false);
            }
        }
    }

    public void ShowBuildingInfo(BuildingData buildingData, BuildingState buildingState)
    {
        _buildingInfoPanel.gameObject.SetActive(true);
        _buildingInfoPanel.ShowBuildingInfo(buildingData, buildingState, this);
    }

    public void RotateBuildingLeft()
    {
        _buildingTileManager.PlacementHandler.RotateLeft();
    }

    public void RotateBuildingRight()
    {
        _buildingTileManager.PlacementHandler.RotateRight();
    }
}
