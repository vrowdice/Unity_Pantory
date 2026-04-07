using System.Collections.Generic;
using UnityEngine;
using Evo.UI;

public partial class MainCanvas
{
    [Header("Build UI Prefabs & Contents")]
    [SerializeField] private GameObject _buildingTypeBtnPrefab;
    [SerializeField] private Transform _buildingTypeBtnContent;
    [SerializeField] private GameObject _buildingBtnPrefab;
    [SerializeField] private Transform _buildingBtnContent;
    [SerializeField] private Switch _removalModeSwitch;

    private bool _ignoreRemovalSwitchCallback;
    private BuildingType _selectedBuildingType = BuildingType.Distribution;
    private BuildingData _selectedBuilding;

    private readonly List<MainBuildingTypeBtn> _buildingTypeBtns = new List<MainBuildingTypeBtn>();
    private readonly List<MainBuildingBtn> _buildingBtns = new List<MainBuildingBtn>();

    private void InitBuildUi()
    {
        BindRemovalModeSwitch();

        _buildingTypeBtns.Clear();
        if (_buildingTypeBtnPrefab != null && _buildingTypeBtnContent != null)
        {
            GameManager.Instance.PoolingManager.ClearChildrenToPool(_buildingTypeBtnContent);

            foreach (BuildingType t in EnumUtils.GetAllEnumValues<BuildingType>())
            {
                GameObject btnObj = GameManager.Instance.PoolingManager.GetPooledObject(_buildingTypeBtnPrefab);
                btnObj.transform.SetParent(_buildingTypeBtnContent, false);
                MainBuildingTypeBtn btn = btnObj.GetComponent<MainBuildingTypeBtn>();
                if (btn != null)
                {
                    btn.Initialize(this, t);
                    _buildingTypeBtns.Add(btn);
                }
            }
        }

        SelectBuildingType(BuildingType.Distribution);
    }

    private void BindRemovalModeSwitch()
    {
        if (_removalModeSwitch == null) return;
        
        _removalModeSwitch.onValueChanged.RemoveListener(HandleRemovalSwitchChanged);
        _removalModeSwitch.onValueChanged.AddListener(HandleRemovalSwitchChanged);
        SetRemovalSwitch(false);
    }

    private void HandleRemovalSwitchChanged(bool isOn)
    {
        if (_ignoreRemovalSwitchCallback) return;

        ApplyRemovalMode(isOn);
    }

    private void SetRemovalSwitch(bool isOn)
    {
        if (_removalModeSwitch == null) return;
        _ignoreRemovalSwitchCallback = true;
        _removalModeSwitch.IsOn = isOn;
        _ignoreRemovalSwitchCallback = false;
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
            UIManager.Instance.ShowWarningPopup(WarningMessage.UnresearchedBuildingBlocksThreadPlacement);
            return;
        }

        if (buildingData != null &&
            buildingData.usePlacedCountLimit &&
            _mainRunner != null &&
            _mainRunner.GridHandler != null &&
            !_mainRunner.GridHandler.CanPlaceMoreInstances(buildingData))
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.BuildingPlacedCountLimitReached);
            RefreshBuildingPlacedCountDisplays();
            return;
        }

        _selectedBuilding = buildingData;
        SetRemovalSwitch(false);

        if (_mainRunner != null && _mainRunner.IsRemovalMode)
        {
            _mainRunner.PlacementHandler.CancelRemoval();
        }

        if (isSelected)
        {
            DeselectBuilding();
        }
        else
        {
            _mainRunner?.StartPlacementMode(buildingData);
        }

        UpdateBuildingButtonStates();
    }

    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _mainRunner.PlacementHandler.CancelPlacement();
        UpdateBuildingButtonStates();
    }

    public void StartRemovalMode()
    {
        SetRemovalSwitch(true);
        ApplyRemovalMode(true);
    }

    public void CancelRemovalMode()
    {
        SetRemovalSwitch(false);
        ApplyRemovalMode(false);
    }

    public void RemovalModeToggle()
    {
        if (_removalModeSwitch != null) SetRemovalSwitch(!_removalModeSwitch.IsOn);
    }

    public void SelectBuildingType(BuildingType buildingType)
    {
        if (_buildingBtnContent != null) GameManager.Instance.PoolingManager.ClearChildrenToPool(_buildingBtnContent);
        _buildingBtns.Clear();

        _selectedBuildingType = buildingType;
        List<BuildingData> list = DataManager.Building.GetBuildingDataList(buildingType);
        if (list != null && _buildingBtnPrefab != null && _buildingBtnContent != null)
        {
            foreach (BuildingData data in list)
            {
                bool isUnlocked = data.requiredResearch == null
                    ? true
                    : (DataManager.Research.IsResearchCompleted(data.requiredResearch.id) || data.isUnlockedByDefault);

                GameObject btnObj = GameManager.Instance.PoolingManager.GetPooledObject(_buildingBtnPrefab);
                btnObj.transform.SetParent(_buildingBtnContent, false);
                MainBuildingBtn btn = btnObj.GetComponent<MainBuildingBtn>();
                if (btn != null)
                {
                    btn.Initialize(this, data, isUnlocked, _mainRunner);
                    _buildingBtns.Add(btn);
                }
            }
        }

        UpdateBuildingTypeButtonStates();
        UpdateBuildingButtonStates();
        RefreshBuildingPlacedCountDisplays();
    }

    private void UpdateBuildingTypeButtonStates()
    {
        foreach (MainBuildingTypeBtn btn in _buildingTypeBtns)
        {
            if (btn == null) continue;
            btn.SetFocused(btn.BuildingType == _selectedBuildingType);
        }
    }

    private void UpdateBuildingButtonStates()
    {
        foreach (MainBuildingBtn btn in _buildingBtns)
        {
            if (btn == null) continue;
            btn.SetSelected(_selectedBuilding == btn.BuildingData);
        }
    }

    public void RefreshBuildingPlacedCountDisplays()
    {
        for (int i = 0; i < _buildingBtns.Count; i++)
        {
            MainBuildingBtn btn = _buildingBtns[i];
            if (btn != null)
                btn.RefreshPlacedCount(_mainRunner);
        }
    }

    private void HandleBuildingInstanceLayoutChanged()
    {
        RefreshBuildingPlacedCountDisplays();
    }
}
