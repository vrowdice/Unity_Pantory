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

    private MainBlueprintTypeBtn _mainBlueprintTypeBtn;
    private MainBlueprintAddBtn _blueprintAddBtn;
    private bool _isBlueprintPanelOpen;

    private void InitBuildUi()
    {
        _buildingTypeBtns.Clear();
        GameManager.PoolingManager.ClearChildrenToPool(_buildingTypeBtnContent);

        foreach (BuildingType buildingType in EnumUtils.GetAllEnumValues<BuildingType>())
        {
            GameObject btnObj = GameManager.PoolingManager.GetPooledObject(_buildingTypeBtnPrefab);
            btnObj.transform.SetParent(_buildingTypeBtnContent, false);
            MainBuildingTypeBtn btn = btnObj.GetComponent<MainBuildingTypeBtn>();
            btn.Init(this, buildingType);
            _buildingTypeBtns.Add(btn);
        }

        EnsureMainBlueprintTypeButtonInContent();

        SelectBuildingType(BuildingType.Distribution);
    }

    private void ApplyRemovalMode(bool isOn)
    {
        if (isOn)
        {
            if (_mainRunner != null && _mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode)
                _mainRunner.SetBlueprintMode(false);
            _isBlueprintPanelOpen = false;
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

        if (_mainRunner != null && _mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode)
            _mainRunner.SetBlueprintMode(false);
        _isBlueprintPanelOpen = false;
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
        if (_mainRunner != null && _mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode)
            _mainRunner.SetBlueprintMode(false);
        _isBlueprintPanelOpen = false;
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
    }

    private void EnsureMainBlueprintTypeButtonInContent()
    {
        GameObject blueprintBtnObj = Instantiate(_blueprintTypeBtnPrefab, _buildingTypeBtnContent);
        MainBlueprintTypeBtn blueprintBtn = blueprintBtnObj.GetComponent<MainBlueprintTypeBtn>();
        blueprintBtn.Init(this);

        _mainBlueprintTypeBtn = blueprintBtn;
    }

    public void SyncBlueprintAddButtonSelected(bool isBlueprintMode)
    {
        RefreshBlueprintUi();
    }

    public void AddBlueprintSavedEntryBeforeAddButton(List<PlacedBuildingSaveData> buildings)
    {
        if (buildings == null || buildings.Count == 0 || _blueprintSavedEntryPrefab == null)
            return;

        if (!_isBlueprintPanelOpen)
            return;

        EnsureBlueprintAddButton();
        GameObject entryObj = Instantiate(_blueprintSavedEntryPrefab, _buildingBtnContent);
        MainBlueprintSavedBtn btn = entryObj.GetComponent<MainBlueprintSavedBtn>();
        if (btn != null)
            btn.Initialize(buildings);

        if (_blueprintAddBtn != null)
            entryObj.transform.SetSiblingIndex(_blueprintAddBtn.transform.GetSiblingIndex());
    }

    public void OnMainBlueprintBtnClicked()
    {
        if (_isBlueprintPanelOpen)
            return;

        GameManager.PoolingManager.ClearChildrenToPool(_buildingBtnContent);
        _buildingBtns.Clear();
        _blueprintAddBtn = null;

        _isBlueprintPanelOpen = true;
        _selectedBuilding = null;
        _mainRunner.PlacementHandler.CancelPlacement();

        RefreshBlueprintUi();
        UpdateBuildingButtonStates();
    }

    public void ToggleBlueprintMode()
    {
        bool nextActive = !_mainRunner.BlueprintHandler.IsBlueprintMode;
        if (_mainRunner != null && _mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode != nextActive)
            _mainRunner.SetBlueprintMode(nextActive);
        _isBlueprintPanelOpen = true;
        if (nextActive)
        {
            _selectedBuilding = null;
            _mainRunner.PlacementHandler.CancelPlacement();
        }
        RefreshBlueprintUi();
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

        _mainBlueprintTypeBtn.SetFocused(false);
        _isBlueprintPanelOpen = false;
    }

    private void EnsureBlueprintAddButton()
    {
        if (_blueprintAddBtn != null) return;
        GameObject addObj = Instantiate(_blueprintAddBtnPrefab, _buildingBtnContent);
        _blueprintAddBtn = addObj.GetComponent<MainBlueprintAddBtn>();
        _blueprintAddBtn.Init(this);
    }

    private void RefreshBlueprintUi()
    {
        bool isBlueprintMode = _mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode;

        if (_isBlueprintPanelOpen)
        {
            foreach (MainBuildingTypeBtn btn in _buildingTypeBtns)
                btn.SetFocused(false);
            _mainBlueprintTypeBtn.SetFocused(true);

            EnsureBlueprintAddButton();
            if (_blueprintAddBtn != null)
            {
                _blueprintAddBtn.gameObject.SetActive(true);
                _blueprintAddBtn.SetSelected(isBlueprintMode);
            }
        }
        else
        {
            UpdateBuildingTypeButtonStates();
            _mainBlueprintTypeBtn.SetFocused(false);
            if (_blueprintAddBtn != null)
                _blueprintAddBtn.gameObject.SetActive(false);
        }
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
}
