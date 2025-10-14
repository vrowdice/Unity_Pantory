using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 건물을 관리하는 서비스 클래스
/// BuildingData ScriptableObject를 기반으로 건물을 동적으로 관리합니다.
/// </summary>
public class BuildingService
{
    // 건물을 저장하는 딕셔너리 (건물 ID -> BuildingEntry)
    private Dictionary<string, BuildingEntry> _buildings;

    // 건물 변경 이벤트
    public event Action OnBuildingChanged;

    /// <summary>
    /// BuildingService 생성자
    /// </summary>
    public BuildingService()
    {
        _buildings = new Dictionary<string, BuildingEntry>();
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

        _buildings[buildingData.id] = new BuildingEntry(buildingData);
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
    /// 특정 건물의 레벨을 반환합니다.
    /// </summary>
    /// <param name="buildingId">건물 ID</param>
    /// <returns>건물 레벨</returns>
    public int GetBuildingLevel(string buildingId)
    {
        if (_buildings.TryGetValue(buildingId, out var entry))
        {
            return entry.buildingState.level;
        }
        
        Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
        return 0;
    }

    /// <summary>
    /// 특정 건물의 작업 효율을 반환합니다.
    /// </summary>
    /// <param name="buildingId">건물 ID</param>
    /// <returns>작업 효율 (0.0 ~ 1.0+)</returns>
    public float GetBuildingEfficiency(string buildingId)
    {
        if (_buildings.TryGetValue(buildingId, out var entry))
        {
            return entry.buildingState.workingEfficiency;
        }
        
        Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
        return 0f;
    }

    /// <summary>
    /// 특정 건물이 건설되어 있는지 확인합니다.
    /// </summary>
    /// <param name="buildingId">건물 ID</param>
    /// <returns>건설되어 있으면 true</returns>
    public bool IsBuildingConstructed(string buildingId)
    {
        if (_buildings.TryGetValue(buildingId, out var entry))
        {
            return entry.buildingState.isConstructed;
        }
        
        Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
        return false;
    }

    /// <summary>
    /// 특정 건물의 BuildingEntry를 반환합니다.
    /// </summary>
    /// <param name="buildingId">건물 ID</param>
    /// <returns>BuildingEntry 또는 null</returns>
    public BuildingEntry GetBuildingEntry(string buildingId)
    {
        if (_buildings.TryGetValue(buildingId, out var entry))
        {
            return entry;
        }
        
        Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
        return null;
    }

    /// <summary>
    /// 모든 건물 정보를 딕셔너리로 반환합니다 (읽기 전용).
    /// </summary>
    /// <returns>건물 딕셔너리의 복사본</returns>
    public Dictionary<string, BuildingEntry> GetAllBuildings()
    {
        return new Dictionary<string, BuildingEntry>(_buildings);
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
    /// 특정 건물 타입의 BuildingEntry 리스트를 반환합니다.
    /// </summary>
    /// <param name="buildingType">건물 타입</param>
    /// <returns>해당 타입의 BuildingEntry 리스트</returns>
    public List<BuildingEntry> GetBuildingEntryList(BuildingType buildingType)
    {
        List<BuildingEntry> result = new List<BuildingEntry>();
        
        foreach (var entry in _buildings.Values)
        {
            if (entry.buildingData.buildingType == buildingType)
            {
                result.Add(entry);
            }
        }
        
        return result;
    }

    /// <summary>
    /// 건설된 모든 건물의 총 유지비를 반환합니다.
    /// </summary>
    /// <returns>총 유지비</returns>
    public int GetTotalMaintenanceCost()
    {
        int total = 0;
        foreach (var entry in _buildings.Values)
        {
            if (entry.buildingState.isConstructed)
            {
                total += CalculateMaintenanceCost(entry);
            }
        }
        return total;
    }

    // ----------------- Public Methods (건물 건설/업그레이드/철거) -----------------

    /// <summary>
    /// 건물을 건설합니다.
    /// </summary>
    /// <param name="buildingId">건설할 건물 ID</param>
    /// <returns>성공 시 true</returns>
    public bool ConstructBuilding(string buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var entry))
        {
            Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
            return false;
        }

        if (entry.buildingState.isConstructed)
        {
            Debug.LogWarning($"[BuildingService] Building already constructed: {entry.buildingData.displayName}");
            return false;
        }

        entry.buildingState.isConstructed = true;
        entry.buildingState.level = 1;
        entry.buildingState.workingEfficiency = 1.0f;
        
        Debug.Log($"[BuildingService] {entry.buildingData.displayName} construction completed");
        
        OnBuildingChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 건물을 철거합니다.
    /// </summary>
    /// <param name="buildingId">철거할 건물 ID</param>
    /// <returns>성공 시 true</returns>
    public bool DemolishBuilding(string buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var entry))
        {
            Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
            return false;
        }

        if (!entry.buildingState.isConstructed)
        {
            Debug.LogWarning($"[BuildingService] Building not constructed: {entry.buildingData.displayName}");
            return false;
        }

        entry.buildingState.isConstructed = false;
        entry.buildingState.level = 0;
        entry.buildingState.workingEfficiency = 0f;
        
        Debug.Log($"[BuildingService] {entry.buildingData.displayName} demolition completed");
        
        OnBuildingChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 건물을 업그레이드합니다.
    /// </summary>
    /// <param name="buildingId">업그레이드할 건물 ID</param>
    /// <returns>성공 시 true</returns>
    public bool UpgradeBuilding(string buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var entry))
        {
            Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
            return false;
        }

        if (!entry.buildingState.isConstructed)
        {
            Debug.LogWarning($"[BuildingService] Building not constructed: {entry.buildingData.displayName}");
            return false;
        }

        entry.buildingState.level++;
        Debug.Log($"[BuildingService] {entry.buildingData.displayName} upgrade completed (level {entry.buildingState.level})");
        
        OnBuildingChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 건물의 레벨을 직접 설정합니다.
    /// </summary>
    /// <param name="buildingId">건물 ID</param>
    /// <param name="level">설정할 레벨</param>
    public void SetBuildingLevel(string buildingId, int level)
    {
        if (level < 0)
        {
            Debug.LogWarning($"[BuildingService] Building level must be 0 or greater. (input: {level})");
            return;
        }

        if (!_buildings.TryGetValue(buildingId, out var entry))
        {
            Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
            return;
        }

        entry.buildingState.level = level;
        
        // 레벨이 0이면 건설되지 않은 것으로 처리
        if (level == 0)
        {
            entry.buildingState.isConstructed = false;
            entry.buildingState.workingEfficiency = 0f;
        }
        else if (!entry.buildingState.isConstructed)
        {
            entry.buildingState.isConstructed = true;
            entry.buildingState.workingEfficiency = 1.0f;
        }
        
        Debug.Log($"[BuildingService] {entry.buildingData.displayName} level = {level}");
        
        OnBuildingChanged?.Invoke();
    }

    // ----------------- Public Methods (효율성 관리) -----------------

    /// <summary>
    /// 건물의 작업 효율을 설정합니다.
    /// </summary>
    /// <param name="buildingId">건물 ID</param>
    /// <param name="efficiency">작업 효율 (0.0 ~ 무한대, 일반적으로 0.0 ~ 2.0)</param>
    public void SetBuildingEfficiency(string buildingId, float efficiency)
    {
        if (!_buildings.TryGetValue(buildingId, out var entry))
        {
            Debug.LogWarning($"[BuildingService] Unregistered building: {buildingId}");
            return;
        }

        entry.buildingState.workingEfficiency = Mathf.Max(0f, efficiency);
        Debug.Log($"[BuildingService] {entry.buildingData.displayName} efficiency = {entry.buildingState.workingEfficiency:F2}");
        
        OnBuildingChanged?.Invoke();
    }

    // ----------------- Private Helper Methods -----------------

    /// <summary>
    /// 건물의 유지비를 계산합니다.
    /// </summary>
    private int CalculateMaintenanceCost(BuildingEntry entry)
    {
        // 기본 유지비 * 레벨
        return entry.buildingData.baseMaintenanceCost * entry.buildingState.level;
    }

    // ----------------- Utility Methods -----------------

    /// <summary>
    /// 모든 건물을 초기화합니다.
    /// </summary>
    public void ResetAllBuildings()
    {
        foreach (var entry in _buildings.Values)
        {
            entry.buildingState.level = 0;
            entry.buildingState.workingEfficiency = 0f;
            entry.buildingState.isConstructed = false;
        }
        Debug.Log("[BuildingService] All buildings have been reset.");
        
        OnBuildingChanged?.Invoke();
    }

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
    /// 건설된 건물 수를 반환합니다.
    /// </summary>
    /// <returns>건설된 건물 수</returns>
    public int GetConstructedBuildingCount()
    {
        int count = 0;
        foreach (var entry in _buildings.Values)
        {
            if (entry.buildingState.isConstructed)
            {
                count++;
            }
        }
        return count;
    }
}


