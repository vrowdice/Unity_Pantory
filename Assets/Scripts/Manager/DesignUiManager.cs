using System.Collections.Generic;
using UnityEngine;

public class DesignUiManager : MonoBehaviour, IUIManager
{
    [SerializeField] private GameObject _buildingTypeBtnPrefab = null;
    [SerializeField] private Transform _buildingTypeBtnContent = null;
    [SerializeField] private GameObject _buildingBtnPrefab = null;
    [SerializeField] private Transform _buildingBtnContent = null;

    private GameDataManager _dataManager = null;
    private List<BuildingData> _buildingDataList = null;
    private BuildingData _selectedBuilding = null;  // 현재 선택된 건물
    private BuildingTileManager _buildingTileManager = null;
    private bool _isRemovalMode = false;  // 현재 제거 모드 활성화 여부

    public Transform CanvasTrans => transform;
    public GameDataManager DataManager => _dataManager;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public bool IsRemovalMode => _isRemovalMode;

    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _dataManager = argGameDataManager;
        _buildingTileManager = FindFirstObjectByType<BuildingTileManager>();

        // BuildingType 버튼 생성
        EnumUtils.GetAllEnumValues<BuildingType>().ForEach(buildingType =>
        {
            var btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            btn.GetComponent<BuildingTypeBtn>().Initialize(this, buildingType);
        });

        // 기본 타입 선택
        SelectBuildingType(BuildingType.Load);
    }

    public void UpdateAllMainText()
    {
        Debug.Log("UpdateAllMainText");
    }

    /// <summary>
    /// 건물 타입을 선택하고 해당 타입의 건물 버튼들을 생성합니다.
    /// </summary>
    /// <param name="buildingType">선택할 건물 타입</param>
    public void SelectBuildingType(BuildingType buildingType)
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[DesignUiManager] DataManager is null. Cannot select building type.");
            return;
        }

        // 해당 타입의 건물 리스트 가져오기
        _buildingDataList = _dataManager.GetBuildingDataList(buildingType);

        // 기존 건물 버튼 제거
        var existingButtons = _buildingBtnContent.GetComponentsInChildren<BuildingBtn>();
        foreach (var btn in existingButtons)
        {
            Destroy(btn.gameObject);
        }

        // 새 건물 버튼 생성
        foreach (var buildingData in _buildingDataList)
        {
            var btn = Instantiate(_buildingBtnPrefab, _buildingBtnContent);
            var buildingBtn = btn.GetComponent<BuildingBtn>();
            
            if (buildingBtn != null )
            {
                buildingBtn.Initialize(this, buildingData);
            }
            else
            {
                Debug.LogError("[DesignUiManager] BuildingBtn component not found on prefab.");
                Destroy(btn);
            }
        }
    }

    /// <summary>
    /// 건물을 선택합니다. (배치 모드로 진입)
    /// </summary>
    public void SelectBuilding(BuildingData buildingData)
    {
        _selectedBuilding = buildingData;
        _isRemovalMode = false;  // 배치 모드로 전환
        Debug.Log($"[DesignUiManager] Building selected: {buildingData.displayName}");
        
        // 제거 모드 취소
        if (_buildingTileManager != null && _buildingTileManager.IsRemovalMode)
        {
            _buildingTileManager.CancelRemovalMode();
        }
        
        // BuildingTileManager에 선택된 건물 전달
        if (_buildingTileManager != null)
        {
            _buildingTileManager.StartPlacementMode(buildingData);
        }
    }

    /// <summary>
    /// 건물 선택을 취소합니다.
    /// </summary>
    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _isRemovalMode = false;
        Debug.Log("[DesignUiManager] Building deselected");
        
        if (_buildingTileManager != null)
        {
            _buildingTileManager.CancelPlacementMode();
        }
    }

    /// <summary>
    /// 건물 제거 모드를 시작합니다.
    /// </summary>
    public void StartRemovalMode()
    {
        _isRemovalMode = true;
        _selectedBuilding = null;
        
        // 기존 배치 모드 취소
        if (_buildingTileManager != null && _buildingTileManager.IsPlacementMode)
        {
            _buildingTileManager.CancelPlacementMode();
        }
        
        if (_buildingTileManager != null)
        {
            _buildingTileManager.StartRemovalMode();
        }
    }

    /// <summary>
    /// 건물 제거 모드를 취소합니다.
    /// </summary>
    public void CancelRemovalMode()
    {
        _isRemovalMode = false;
        
        if (_buildingTileManager != null)
        {
            _buildingTileManager.CancelRemovalMode();
        }
    }

    /// <summary>
    /// 건물 제거 모드를 토글합니다.
    /// </summary>
    public void RemovalModeToggle()
    {
        if (_isRemovalMode)
        {
            CancelRemovalMode();
            Debug.Log("[DesignUiManager] Removal mode toggled OFF");
        }
        else
        {
            StartRemovalMode();
            Debug.Log("[DesignUiManager] Removal mode toggled ON");
        }
    }
}
