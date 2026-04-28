using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Evo.UI;

public partial class MainCanvas
{
    [Header("Build UI Prefabs & Contents")]
    [SerializeField] private GameObject _buildingTypeBtnPrefab;
    [SerializeField] private Transform _buildingTypeBtnContent;
    [SerializeField] private GameObject _buildingBtnPrefab;
    [SerializeField] private Transform _buildingBtnContent;
    
    [FormerlySerializedAs("_blueprintBtnPrefab")]
    [SerializeField] private GameObject _blueprintTypeBtnPrefab;
    [SerializeField] private GameObject _blueprintAddBtnPrefab;
    [SerializeField] private GameObject _blueprintSavedEntryPrefab;
    [SerializeField] private Switch _autoEmployeePlacementSwitch;
    [SerializeField] private Switch _removalModeSwitch;

    private BuildingType _selectedBuildingType = BuildingType.Distribution;
    private BuildingData _selectedBuilding;

    private readonly List<MainBuildingTypeBtn> _buildingTypeBtns = new List<MainBuildingTypeBtn>();
    private readonly List<MainBuildingBtn> _buildingBtns = new List<MainBuildingBtn>();

    private MainBlueprintAddBtn _blueprintAddBtn;

    private void InitBuildUi()
    {
        _buildingTypeBtns.Clear();
        GameManager.PoolingManager.ClearChildrenToPool(_buildingTypeBtnContent);

        foreach (BuildingType t in EnumUtils.GetAllEnumValues<BuildingType>())
        {
            GameObject btnObj = GameManager.PoolingManager.GetPooledObject(_buildingTypeBtnPrefab);
            btnObj.transform.SetParent(_buildingTypeBtnContent, false);
            MainBuildingTypeBtn btn = btnObj.GetComponent<MainBuildingTypeBtn>();
            btn.Initialize(this, t);
            _buildingTypeBtns.Add(btn);
        }

        SelectBuildingType(BuildingType.Distribution);
    }

    private void ApplyRemovalMode(bool isOn)
    {
        if (isOn)
        {
            _selectedBuilding = null;
            _mainRunner.PlacementHandler.CancelPlacement();
            _mainRunner.PlacementHandler.StartRemoval();
        }
        else
        {
            _mainRunner.PlacementHandler.CancelRemoval();
        }

        UpdateBuildingButtonStates();
    }

    public void SelectBuilding(BuildingData buildingData, bool isSelected, bool isUnlocked)
    {
        if (!isUnlocked)
        {
            UIManager.ShowWarningPopup(WarningMessage.UnresearchedBuildingBlocksThreadPlacement);
            DeselectBuilding();
            return;
        }

        if (buildingData != null &&
            buildingData.usePlacedCountLimit &&
            !_mainRunner.GridHandler.CanPlaceMoreInstances(buildingData))
        {
            UIManager.ShowWarningPopup(WarningMessage.BuildingPlacedCountLimitReached);
            RefreshBuildingPlacedCountDisplays();
            DeselectBuilding();
            return;
        }

        _selectedBuilding = buildingData;
        _removalModeSwitch.SetValue(false);

        if (isSelected)
        {
            DeselectBuilding();
        }
        else
        {
            _mainRunner.StartPlacementMode(buildingData);
        }

        UpdateBuildingButtonStates();
    }

    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _mainRunner.PlacementHandler.CancelPlacement();
        UpdateBuildingButtonStates();
    }

    public void AutoEmployeePlacementToggle()
    {
        _mainRunner.PlacementHandler.ToggleAutoEmployeePlacement(_autoEmployeePlacementSwitch.IsOn);
    }

    public void RemovalModeToggle()
    {
        ApplyRemovalMode(_removalModeSwitch.IsOn);
    }

    public void SelectBuildingType(BuildingType buildingType)
    {
        GameManager.PoolingManager.ClearChildrenToPool(_buildingBtnContent);
        _buildingBtns.Clear();
        _blueprintAddBtn = null;

        _selectedBuildingType = buildingType;
        List<BuildingData> list = DataManager.Building.GetBuildingDataList(buildingType);
        foreach (BuildingData data in list)
        {
            bool isUnlocked = data.requiredResearch == null
                ? true
                : (DataManager.Research.IsResearchCompleted(data.requiredResearch.id) || data.isUnlockedByDefault);

            GameObject btnObj = GameManager.PoolingManager.GetPooledObject(_buildingBtnPrefab);
            btnObj.transform.SetParent(_buildingBtnContent, false);
            MainBuildingBtn btn = btnObj.GetComponent<MainBuildingBtn>();
            btn.Initialize(this, data, isUnlocked, _mainRunner);
            _buildingBtns.Add(btn);
        }

        UpdateBuildingTypeButtonStates();
        UpdateBuildingButtonStates();
        RefreshBuildingPlacedCountDisplays();

        EnsureMainBlueprintButtonInContent();
    }

    private void EnsureMainBlueprintButtonInContent()
    {
        GameObject blueprintBtnObj = Instantiate(_blueprintTypeBtnPrefab, _buildingBtnContent);
        MainBlueprintBtn blueprintBtn = blueprintBtnObj.GetComponent<MainBlueprintBtn>();
        blueprintBtn.Init(this);
    }

    public void SyncBlueprintAddButtonSelected(bool isBlueprintMode)
    {
        if (_blueprintAddBtn != null)
            _blueprintAddBtn.SetSelected(isBlueprintMode);
    }

    public void AddBlueprintSavedEntryBeforeAddButton(List<PlacedBuildingSaveData> buildings)
    {
        if (buildings == null || buildings.Count == 0 || _blueprintSavedEntryPrefab == null)
            return;

        GameObject entryObj = Instantiate(_blueprintSavedEntryPrefab, _buildingBtnContent);
        MainBlueprintSavedBtn btn = entryObj.GetComponent<MainBlueprintSavedBtn>();
        if (btn != null)
            btn.Initialize(buildings);

        if (_blueprintAddBtn != null)
            entryObj.transform.SetSiblingIndex(_blueprintAddBtn.transform.GetSiblingIndex());
    }

    public void OnMainBlueprintBtnClicked()
    {
        if (_blueprintAddBtn == null)
        {
            GameObject addObj = Instantiate(_blueprintAddBtnPrefab, _buildingBtnContent);
            _blueprintAddBtn = addObj.GetComponent<MainBlueprintAddBtn>();
            _blueprintAddBtn.Init(this);
            _blueprintAddBtn.SetSelected(_mainRunner.BlueprintHandler.IsBlueprintMode);
        }
        else
        {
            bool nextActive = !_blueprintAddBtn.gameObject.activeSelf;
            _blueprintAddBtn.gameObject.SetActive(nextActive);
        }
    }

    public void ToggleBlueprintMode(bool addBtnShowsAsSelected)
    {
        if (addBtnShowsAsSelected)
            _mainRunner.SetBlueprintMode(false);
        else
        {
            DeselectBuilding();
            _removalModeSwitch.SetValue(false);
            _mainRunner.SetBlueprintMode(true);
        }

        UpdateBuildingButtonStates();
    }

    public void RotateBuilding(bool clockwise)
    {
        if (_mainRunner.PlacementHandler.IsPlacementMode)
            _mainRunner.PlacementHandler.Rotate(clockwise);
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
            _buildingBtns[i].RefreshPlacedCount(_mainRunner);
    }

    private void HandleBuildingInstanceLayoutChanged()
    {
        RefreshBuildingPlacedCountDisplays();
    }
}
