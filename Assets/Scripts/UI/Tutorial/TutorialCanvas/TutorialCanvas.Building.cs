using System.Collections.Generic;
using UnityEngine;
using Evo.UI;

public partial class TutorialCanvas
{
    private static readonly BuildingType[] TutorialBuildingTypes =
    {
        BuildingType.Distribution,
        BuildingType.Production,
        BuildingType.RawProduction,
    };

    private static readonly HashSet<string> TutorialBuildingIds = new HashSet<string>
    {
        "load",
        "logging_camp",
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

    private void SyncAutoEmployeePlacementSwitch(bool? forceValue = null)
    {
        if (_sceneRunner == null || _sceneRunner.PlacementHandler == null)
            return;

        bool isOn = forceValue ?? _sceneRunner.PlacementHandler.IsAutoEmployeePlacement;

        if (_autoEmployeePlacementSwitch != null)
            _autoEmployeePlacementSwitch.SetValue(isOn);

        _sceneRunner.PlacementHandler.ToggleAutoEmployeePlacement(isOn);
    }

    public void PrepareAutoEmployeePlacementStep()
    {
        SyncAutoEmployeePlacementSwitch(false);
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
        if (buildingData != null && !TutorialInputGate.CanSelectBuilding(buildingData.id))
        {
            DeselectBuilding();
            return;
        }

        if (buildingData != null &&
            !TutorialInputGate.IsActive &&
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

    public void AutoEmployeePlacementToggle()
    {
        if (!TutorialInputGate.CanToggleAutoEmployeePlacement())
        {
            SyncAutoEmployeePlacementSwitch();
            return;
        }

        bool isOn = _autoEmployeePlacementSwitch.IsOn;
        _sceneRunner.PlacementHandler.ToggleAutoEmployeePlacement(isOn);
        _tutorialFlow?.NotifyAutoEmployeePlacementChanged(isOn);
    }

    public void RemovalModeToggle()
    {
        if (!TutorialInputGate.CanUseRemovalMode())
        {
            _removalModeSwitch.SetValue(false);
            return;
        }

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
        if (isOn && !TutorialInputGate.CanUseRemovalMode())
        {
            _removalModeSwitch.SetValue(false);
            return;
        }

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

    public bool TryCancelActiveBuildMode()
    {
        MainBuildingPlacementHandler placementHandler = _sceneRunner.PlacementHandler;
        if (placementHandler == null)
            return false;

        if (placementHandler.IsRemovalMode)
        {
            _removalModeSwitch.SetValue(false);
            ApplyRemovalMode(false);
            return true;
        }

        if (placementHandler.IsPlacementMode)
        {
            DeselectBuilding();
            return true;
        }

        return false;
    }

    public void CancelRemovalModeIfActive()
    {
        if (_sceneRunner?.PlacementHandler != null && _sceneRunner.PlacementHandler.IsRemovalMode)
        {
            _removalModeSwitch.SetValue(false);
            ApplyRemovalMode(false);
        }
    }

    public Switch FindRemovalModeSwitch()
    {
        return _removalModeSwitch;
    }

    public Switch FindAutoEmployeePlacementSwitch()
    {
        return _autoEmployeePlacementSwitch;
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

    public GameObject FindRotateBtnContainer()
    {
        return _rotateBtnContainer;
    }
}
