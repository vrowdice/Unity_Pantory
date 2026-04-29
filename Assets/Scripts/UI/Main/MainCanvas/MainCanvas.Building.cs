using System.Collections.Generic;
using System.Text;
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
    private string _activeBlueprintLayoutKey;

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

        _mainBlueprintTypeBtn.SetFocused(false);
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

    public void AddBlueprintSavedEntryBeforeAddButton(string blueprintName, List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        bool hasBuildings = buildings != null && buildings.Count > 0;
        bool hasRoads = roads != null && roads.Count > 0;
        if ((!hasBuildings && !hasRoads) || _blueprintSavedEntryPrefab == null)
            return;

        if (!_isBlueprintPanelOpen)
            return;

        if (DataManager != null && DataManager.BlueprintLayout != null)
            DataManager.BlueprintLayout.Add(blueprintName, buildings, roads);

        EnsureBlueprintAddButton();
        CreateBlueprintSavedEntryUi(blueprintName, buildings, roads);
    }

    public void RequestSaveBlueprintEntry(List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        UIManager.Instance.ShowEnterNamePopup((string enteredName) =>
        {
            string blueprintName = string.IsNullOrWhiteSpace(enteredName) ? "Blueprint" : enteredName.Trim();
            AddBlueprintSavedEntryBeforeAddButton(blueprintName, buildings, roads);
        });
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
    }

    private void EnsureBlueprintAddButton()
    {
        if (_blueprintAddBtn != null) return;
        GameObject addObj = Instantiate(_blueprintAddBtnPrefab, _buildingBtnContent);
        _blueprintAddBtn = addObj.GetComponent<MainBlueprintAddBtn>();
        _blueprintAddBtn.Init(this);
    }

    private static string BuildBlueprintLayoutKey(string blueprintName, List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(blueprintName).Append('|');
        if (buildings != null)
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                PlacedBuildingSaveData b = buildings[i];
                if (b == null) continue;
                sb.Append("b:")
                    .Append(b.buildingDataId).Append(':')
                    .Append(b.originX).Append(':')
                    .Append(b.originY).Append(':')
                    .Append(b.rotation).Append(';');
            }
        }

        sb.Append('|');
        if (roads != null)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                PlacedRoadSaveData r = roads[i];
                if (r == null) continue;
                sb.Append("r:")
                    .Append(r.sourceBuildingDataId).Append(':')
                    .Append(r.roadDataId).Append(':')
                    .Append(r.x).Append(':')
                    .Append(r.y).Append(':')
                    .Append(r.rotation).Append(';');
            }
        }

        return sb.ToString();
    }

    private void CreateBlueprintSavedEntryUi(string blueprintName, List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads)
    {
        GameObject entryObj = Instantiate(_blueprintSavedEntryPrefab, _buildingBtnContent);
        MainBlueprintSavedBtn btn = entryObj.GetComponent<MainBlueprintSavedBtn>();
        string layoutKey = BuildBlueprintLayoutKey(blueprintName, buildings, roads);
        bool isSelected = _mainRunner != null &&
                          _mainRunner.PlacementHandler != null &&
                          _mainRunner.PlacementHandler.IsBlueprintPlacementMode &&
                          _activeBlueprintLayoutKey == layoutKey;
        if (btn != null)
            btn.Initialize(this, layoutKey, blueprintName, buildings, roads, isSelected);

        if (_blueprintAddBtn != null)
            entryObj.transform.SetSiblingIndex(_blueprintAddBtn.transform.GetSiblingIndex());
    }

    private void RebuildBlueprintSavedEntriesFromData()
    {
        GameManager.PoolingManager.ClearChildrenToPool(_buildingBtnContent);

        _buildingBtns.Clear();
        _blueprintAddBtn = null;
        EnsureBlueprintAddButton();

        if (DataManager == null || DataManager.BlueprintLayout == null)
            return;

        IReadOnlyList<BlueprintLayoutSaveData> layouts = DataManager.BlueprintLayout.GetAll();
        for (int i = 0; i < layouts.Count; i++)
        {
            BlueprintLayoutSaveData layout = layouts[i];
            bool hasBuildings = layout != null && layout.buildings != null && layout.buildings.Count > 0;
            bool hasRoads = layout != null && layout.roads != null && layout.roads.Count > 0;
            if (!hasBuildings && !hasRoads)
                continue;

            CreateBlueprintSavedEntryUi(layout.blueprintName, layout.buildings, layout.roads);
        }
    }

    private void RefreshBlueprintUi()
    {
        bool isBlueprintMode = _mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode;
        bool isBlueprintPlacementMode = _mainRunner.PlacementHandler != null && _mainRunner.PlacementHandler.IsBlueprintPlacementMode;
        if (!isBlueprintPlacementMode)
            _activeBlueprintLayoutKey = null;

        if (_isBlueprintPanelOpen)
        {
            foreach (MainBuildingTypeBtn btn in _buildingTypeBtns)
                btn.SetFocused(false);
            _mainBlueprintTypeBtn.SetFocused(true);

            RebuildBlueprintSavedEntriesFromData();
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

    public void ToggleSavedBlueprintPlacement(string layoutKey, string blueprintName, List<PlacedBuildingSaveData> blueprintBuildings, List<PlacedRoadSaveData> blueprintRoads)
    {
        bool hasBuildings = blueprintBuildings != null && blueprintBuildings.Count > 0;
        bool hasRoads = blueprintRoads != null && blueprintRoads.Count > 0;
        if (!hasBuildings && !hasRoads)
            return;

        bool isAlreadyActive = _mainRunner != null &&
                               _mainRunner.PlacementHandler != null &&
                               _mainRunner.PlacementHandler.IsBlueprintPlacementMode &&
                               _activeBlueprintLayoutKey == layoutKey;
        if (isAlreadyActive)
        {
            _mainRunner.PlacementHandler.CancelBlueprintPlacement();
            _activeBlueprintLayoutKey = null;
            RefreshBlueprintUi();
            return;
        }

        if (_mainRunner != null && _mainRunner.PlacementHandler != null && _mainRunner.PlacementHandler.IsRemovalMode)
        {
            UIManager.Instance.ShowConfirmPopup(ConfirmMessage.DeleteConfirm, () =>
            {
                RemoveSavedBlueprintLayout(layoutKey);
            });
            return;
        }

        StartSavedBlueprintPlacement(layoutKey, blueprintName, blueprintBuildings, blueprintRoads);
    }

    private void StartSavedBlueprintPlacement(string layoutKey, string blueprintName, List<PlacedBuildingSaveData> blueprintBuildings, List<PlacedRoadSaveData> blueprintRoads)
    {
        if (_mainRunner == null || _mainRunner.PlacementHandler == null)
            return;

        if (_mainRunner != null && _mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode)
            _mainRunner.SetBlueprintMode(false);

        _selectedBuilding = null;
        _mainRunner.PlacementHandler.CancelRemoval();
        _mainRunner.StartBlueprintPlacementMode(blueprintName, blueprintBuildings, blueprintRoads);
        _activeBlueprintLayoutKey = layoutKey;
        RefreshBlueprintUi();
        UpdateBuildingButtonStates();
    }

    private void RemoveSavedBlueprintLayout(string layoutKey)
    {
        if (DataManager == null || DataManager.BlueprintLayout == null)
            return;

        IReadOnlyList<BlueprintLayoutSaveData> layouts = DataManager.BlueprintLayout.GetAll();
        for (int i = 0; i < layouts.Count; i++)
        {
            BlueprintLayoutSaveData layout = layouts[i];
            if (layout == null) continue;

            string key = BuildBlueprintLayoutKey(layout.blueprintName, layout.buildings, layout.roads);
            if (key != layoutKey) continue;

            DataManager.BlueprintLayout.RemoveAt(i);
            if (_activeBlueprintLayoutKey == layoutKey)
            {
                _mainRunner.PlacementHandler.CancelBlueprintPlacement();
                _activeBlueprintLayoutKey = null;
            }

            RebuildBlueprintSavedEntriesFromData();
            return;
        }
    }
}
