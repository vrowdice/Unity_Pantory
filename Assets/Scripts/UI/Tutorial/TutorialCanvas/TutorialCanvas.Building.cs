using System.Collections.Generic;
using UnityEngine;
using Evo.UI;

public partial class TutorialCanvas
{
    private static readonly BuildingType[] TutorialBuildingTypes =
    {
        BuildingType.Distribution,
        BuildingType.Production,
    };

    private static readonly HashSet<string> TutorialBuildingIds = new HashSet<string>
    {
        "load",
        "road",
        "sawmill",
        "unload"
    };

    [Header("Build UI Prefabs & Contents")]
    [SerializeField] private GameObject _buildingTypeBtnPrefab;
    [SerializeField] private Transform _buildingTypeBtnContent;
    [SerializeField] private GameObject _buildingBtnPrefab;
    [SerializeField] private Transform _buildingBtnContent;
    [SerializeField] private Switch _autoEmployeePlacementSwitch;
    [SerializeField] private Switch _removalModeSwitch;
    [SerializeField] private GameObject _rotateBtnContainer;

    private BuildingType _selectedBuildingType = BuildingType.Production;
    private BuildingData _selectedBuilding;

    private readonly List<MainBuildingTypeBtn> _buildingTypeBtns = new List<MainBuildingTypeBtn>();
    private readonly List<MainBuildingBtn> _buildingBtns = new List<MainBuildingBtn>();

    public Switch AutoEmployeePlacementSwitch => _autoEmployeePlacementSwitch;
    public Switch RemovalModeSwitch => _removalModeSwitch;
    public GameObject RotateBtnContainer => _rotateBtnContainer;

    private void InitBuildUi()
    {
        _buildingTypeBtns.Clear();
        GameManager.PoolingManager.ClearChildrenToPool(_buildingTypeBtnContent);

        for (int i = 0; i < TutorialBuildingTypes.Length; i++)
        {
            BuildingType buildingType = TutorialBuildingTypes[i];
            GameObject btnObj = GameManager.PoolingManager.GetPooledObject(_buildingTypeBtnPrefab);
            btnObj.transform.SetParent(_buildingTypeBtnContent, false);
            MainBuildingTypeBtn btn = btnObj.GetComponent<MainBuildingTypeBtn>();
            btn.Init(this, buildingType);
            _buildingTypeBtns.Add(btn);
        }

        SelectBuildingType(BuildingType.Distribution);
        SyncAutoEmployeePlacementSwitch(false);
    }

    public void SyncAutoEmployeePlacementSwitch(bool forceValue)
    {
        if (_sceneRunner == null || _sceneRunner.PlacementHandler == null)
            return;

        bool isOn = forceValue;

        if (_autoEmployeePlacementSwitch != null)
            _autoEmployeePlacementSwitch.SetValue(isOn);

        _sceneRunner.PlacementHandler.ToggleAutoEmployeePlacement(isOn);
        _tutorialFlow?.NotifyAutoEmployeePlacementChanged(isOn);
    }

    public void SelectBuildingTypeForBuilding(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return;

        BuildingData buildingData = DataManager.Building.GetBuildingData(buildingId);
        if (buildingData == null)
            return;

        SelectBuildingType(buildingData.buildingType);
    }

    public void SelectBuilding(BuildingData buildingData, bool isSelected)
    {
        if (buildingData != null &&
            buildingData.usePlacedCountLimit &&
            !_sceneRunner.GridHandler.CanPlaceMoreInstances(buildingData))
        {
            UIManager.ShowWarningPopup(WarningMessage.BuildingPlacedCountLimitReached);
            RefreshBuildingPlacedCountDisplays();
            DeselectBuilding();
            return;
        }

        _selectedBuilding = buildingData;
        _removalModeSwitch.SetValue(false);

        if (isSelected)
            DeselectBuilding();
        else
            _sceneRunner.StartPlacementMode(buildingData);

        UpdateBuildingButtonStates();
        _tutorialFlow?.NotifyBuildingSelected(buildingData?.id);
    }

    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _sceneRunner.PlacementHandler.CancelPlacement();
        UpdateBuildingButtonStates();
    }

    public void RemovalModeToggle()
    {
        ApplyRemovalMode(_removalModeSwitch.IsOn);
    }

    public void SelectBuildingType(BuildingType buildingType)
    {
        GameManager.PoolingManager.ClearChildrenToPool(_buildingBtnContent);
        _buildingBtns.Clear();

        _selectedBuildingType = buildingType;
        PopulateBuildingButtons();
        UpdateBuildingTypeButtonStates();
    }

    private void PopulateBuildingButtons()
    {
        List<BuildingData> list = DataManager.Building.GetBuildingDataList(_selectedBuildingType);
        foreach (BuildingData data in list)
        {
            if (!TutorialBuildingIds.Contains(data.id))
                continue;

            GameObject btnObj = GameManager.PoolingManager.GetPooledObject(_buildingBtnPrefab);
            btnObj.transform.SetParent(_buildingBtnContent, false);
            MainBuildingBtn btn = btnObj.GetComponent<MainBuildingBtn>();
            btn.Init(this, data, _sceneRunner);
            _buildingBtns.Add(btn);
        }

        UpdateBuildingButtonStates();
        RefreshBuildingPlacedCountDisplays();
    }

    private void ApplyRemovalMode(bool isOn)
    {
        if (isOn)
        {
            _selectedBuilding = null;
            _sceneRunner.PlacementHandler.CancelPlacement();
            _sceneRunner.PlacementHandler.StartRemoval();
        }
        else
        {
            _sceneRunner.PlacementHandler.CancelRemoval();
        }

        UpdateBuildingButtonStates();
        _tutorialFlow?.NotifyRemovalModeChanged(isOn);
    }

    public void SyncBlueprintAddButtonSelected(bool isBlueprintMode)
    {
    }

    public void RequestSaveBlueprintEntry(List<PlacedBuildingSaveData> captured, List<PlacedRoadSaveData> capturedRoads)
    {
    }

    public void RotateBuilding(bool clockwise)
    {
        if (_sceneRunner.PlacementHandler.IsPlacementMode)
            _sceneRunner.PlacementHandler.Rotate(clockwise);
    }

    private void UpdateBuildingTypeButtonStates()
    {
        foreach (MainBuildingTypeBtn btn in _buildingTypeBtns)
            btn.SetFocused(btn.BuildingType == _selectedBuildingType);
    }

    private void UpdateBuildingButtonStates()
    {
        foreach (MainBuildingBtn btn in _buildingBtns)
            btn.SetSelected(_selectedBuilding == btn.BuildingData);
    }

    public void RefreshBuildingPlacedCountDisplays()
    {
        for (int i = 0; i < _buildingBtns.Count; i++)
            _buildingBtns[i].RefreshPlacedCount(_sceneRunner);
    }

    public MainBuildingBtn FindBuildingButton(string buildingId)
    {
        for (int i = 0; i < _buildingBtns.Count; i++)
        {
            MainBuildingBtn btn = _buildingBtns[i];
            if (btn.BuildingData != null && btn.BuildingData.id == buildingId)
                return btn;
        }

        return null;
    }
}
