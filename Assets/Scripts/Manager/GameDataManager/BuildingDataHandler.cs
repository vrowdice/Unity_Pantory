using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 건물 데이터를 관리하는 서비스 클래스
/// BuildingData ScriptableObject를 로드하고 참조를 제공합니다.
/// 건물의 동적 상태(BuildingState)는 ThreadState에서 관리됩니다.
/// </summary>
public class BuildingDataHandler
{
    // 건물 데이터를 저장하는 딕셔너리 (건물 ID -> BuildingData)
    private Dictionary<string, BuildingData> _buildings;

    // 건물 데이터 변경 이벤트 (데이터 로드 완료 등)
    //public event Action OnBuildingChanged;

    /// <summary>
    /// BuildingService 생성자
    /// </summary>
    public BuildingDataHandler(GameDataManager gameDataManager)
    {
        _buildings = new Dictionary<string, BuildingData>();
        AutoLoadAllBuildings(); // 게임 시작 시 자동으로 모든 건물 데이터 로드
    }

    // ----------------- 초기화 -----------------

    /// <summary>
    /// 지정된 경로에서 모든 BuildingData를 자동으로 로드하여 등록합니다.
    /// </summary>
    /// <param name="buildingPaths">검색할 폴더 경로 배열 (예: "Datas/Building")</param>
    public void AutoLoadBuildings(string[] buildingPaths)
    {
#if UNITY_EDITOR
        int loadedCount = 0;
        
        foreach (string path in buildingPaths)
        {
            // AssetDatabase를 사용하여 모든 BuildingData 찾기
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BuildingData", new[] { "Assets/" + path });
            
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                BuildingData buildingData = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>(assetPath);
                
                if (buildingData != null)
                {
                    RegisterBuilding(buildingData);
                    loadedCount++;
                }
            }
        }
        
        Debug.Log($"[BuildingService] Auto load completed: {loadedCount} building types registered");
#else
        Debug.LogWarning("[BuildingService] AutoLoadBuildings is only available in editor mode.");
#endif
    }

    /// <summary>
    /// 모든 BuildingData를 자동으로 검색하여 등록합니다. (전체 Assets 폴더)
    /// </summary>
    public void AutoLoadAllBuildings()
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BuildingData");
        int loadedCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            BuildingData buildingData = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>(assetPath);
            
            if (buildingData != null)
            {
                RegisterBuilding(buildingData);
                loadedCount++;
            }
        }
        
        Debug.Log($"[BuildingService] Full auto load completed: {loadedCount} building types registered");
#else
        Debug.LogWarning("[BuildingService] AutoLoadAllBuildings is only available in editor mode.");
#endif
    }

    /// <summary>
    /// BuildingData를 등록하여 관리 대상에 추가합니다.
    /// </summary>
    /// <param name="buildingData">등록할 BuildingData</param>
    public void RegisterBuilding(BuildingData buildingData)
    {
        if (buildingData == null)
        {
            Debug.LogWarning("[BuildingService] BuildingData is null.");
            return;
        }

        if (string.IsNullOrEmpty(buildingData.id))
        {
            Debug.LogWarning("[BuildingService] BuildingData ID is empty.");
            return;
        }

        if (_buildings.ContainsKey(buildingData.id))
        {
            Debug.LogWarning($"[BuildingService] Building type already registered: {buildingData.id}");
            return;
        }

        _buildings[buildingData.id] = buildingData;
    }

    /// <summary>
    /// 여러 BuildingData를 한 번에 등록합니다.
    /// </summary>
    /// <param name="buildingDataList">등록할 BuildingData 배열</param>
    public void RegisterBuildings(BuildingData[] buildingDataList)
    {
        foreach (var data in buildingDataList)
        {
            RegisterBuilding(data);
        }
    }

    // ----------------- Public Getters (읽기 전용) -----------------

    /// <summary>
    /// 특정 건물의 BuildingData를 반환합니다.
    /// </summary>
    /// <param name="buildingId">건물 ID</param>
    /// <returns>BuildingData 또는 null</returns>
    public BuildingData GetBuildingData(string buildingId)
    {
        if (_buildings.TryGetValue(buildingId, out var data))
        {
            return data;
        }
        
        Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
        return null;
    }

    /// <summary>
    /// 모든 건물 데이터를 딕셔너리로 반환합니다 (읽기 전용).
    /// </summary>
    /// <returns>건물 딕셔너리의 복사본</returns>
    public Dictionary<string, BuildingData> GetAllBuildings()
    {
        return new Dictionary<string, BuildingData>(_buildings);
    }

    /// <summary>
    /// 등록된 모든 건물 ID 목록을 반환합니다.
    /// </summary>
    /// <returns>건물 ID 리스트</returns>
    public List<string> GetAllBuildingIds()
    {
        return new List<string>(_buildings.Keys);
    }

    /// <summary>
    /// 특정 건물 타입의 BuildingData 리스트를 반환합니다.
    /// </summary>
    /// <param name="buildingType">건물 타입</param>
    /// <returns>해당 타입의 BuildingData 리스트</returns>
    public List<BuildingData> GetBuildingDataList(BuildingType buildingType)
    {
        List<BuildingData> result = new List<BuildingData>();
        
        foreach (var data in _buildings.Values)
        {
            if (data.buildingType == buildingType)
            {
                result.Add(data);
            }
        }
        
        return result;
    }

    // ----------------- Utility Methods -----------------

    /// <summary>
    /// 특정 건물이 등록되어 있는지 확인합니다.
    /// </summary>
    /// <param name="buildingId">확인할 건물 ID</param>
    /// <returns>등록되어 있으면 true</returns>
    public bool IsBuildingRegistered(string buildingId)
    {
        return _buildings.ContainsKey(buildingId);
    }

    /// <summary>
    /// 등록된 건물 타입의 개수를 반환합니다.
    /// </summary>
    /// <returns>건물 타입 개수</returns>
    public int GetBuildingTypeCount()
    {
        return _buildings.Count;
    }
}
