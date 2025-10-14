using System.Collections.Generic;
using UnityEngine;

public class DesignUiManager : MonoBehaviour, IUIManager
{
    [SerializeField] private GameObject _buildingTypeBtnPrefab = null;
    [SerializeField] private Transform _buildingTypeBtnContent = null;
    [SerializeField] private GameObject _buildingBtnPrefab = null;
    [SerializeField] private Transform _buildingBtnContent = null;

    private GameDataManager _dataManager = null;
    private List<BuildingEntry> _buildingEntryList = null;

    public Transform CanvasTrans => transform;
    public GameDataManager DataManager => _dataManager;

    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _dataManager = argGameDataManager;

        // BuildingType 버튼 생성
        EnumUtils.GetAllEnumValues<BuildingType>().ForEach(buildingType =>
        {
            var btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            btn.GetComponent<BuildingTypeBtn>().Initialize(this, buildingType);
        });

        // 기본 타입 선택
        SelectBuildingType(BuildingType.Load);

        Debug.Log("[DesignUiManager] Initialized.");
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
        _buildingEntryList = _dataManager.GetBuildingEntryList(buildingType);

        // 기존 건물 버튼 제거
        var existingButtons = _buildingBtnContent.GetComponentsInChildren<BuildingBtn>();
        foreach (var btn in existingButtons)
        {
            Destroy(btn.gameObject);
        }

        // 새 건물 버튼 생성
        foreach (var buildingEntry in _buildingEntryList)
        {
            var btn = Instantiate(_buildingBtnPrefab, _buildingBtnContent);
            var buildingBtn = btn.GetComponent<BuildingBtn>();
            
            if (buildingBtn != null )
            {
                buildingBtn.Initialize(this, buildingEntry);
            }
            else
            {
                Debug.LogError("[DesignUiManager] BuildingBtn component not found on prefab.");
                Destroy(btn);
            }
        }

        Debug.Log($"[DesignUiManager] Selected building type: {buildingType}, {_buildingEntryList.Count} buildings loaded.");
    }

    public void SelectBuilding(BuildingEntry buildingEntry)
    {
        Debug.Log("SelectBuilding: " + buildingEntry.buildingData.displayName);
    }
}
