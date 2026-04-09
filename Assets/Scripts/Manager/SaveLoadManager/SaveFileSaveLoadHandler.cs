using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 세이브 파일 저장/로드를 처리하는 핸들러 클래스
/// </summary>
public class SaveFileSaveLoadHandler
{
    private readonly SaveLoadManager _saveLoadManager;
    private const string SAVE_FILE_DIRECTORY = "SaveFiles";
    private const string SAVE_FILE_EXTENSION = ".json";

    public SaveFileSaveLoadHandler(SaveLoadManager saveLoadManager)
    {
        _saveLoadManager = saveLoadManager;
    }

    /// <summary>
    /// 세이브 파일 디렉토리의 전체 경로를 반환합니다.
    /// </summary>
    private string GetSaveFileDirectory()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FILE_DIRECTORY);
    }

    /// <summary>
    /// 세이브 파일의 전체 경로를 반환합니다.
    /// </summary>
    private string GetSaveFilePath(string fileName)
    {
        if (!fileName.EndsWith(SAVE_FILE_EXTENSION))
        {
            fileName += SAVE_FILE_EXTENSION;
        }
        return Path.Combine(GetSaveFileDirectory(), fileName);
    }

    /// <summary>
    /// 게임 데이터를 세이브 파일로 저장합니다.
    /// </summary>
    /// <param name="fileName">세이브 파일 이름 (확장자 없이)</param>
    /// <param name="dataManager">DataManager 인스턴스</param>
    /// <returns>성공 시 true</returns>
    public bool SaveSavefile(string fileName, DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[SaveFileSaveLoadHandler] DataManager is null.");
            return false;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("[SaveFileSaveLoadHandler] File name is null or empty.");
            return false;
        }

        try
        {
            GameSaveData saveData = CollectGameState(dataManager);

            string json = JsonUtility.ToJson(saveData, true);
            string filePath = GetSaveFilePath(fileName);

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
            Debug.Log($"[SaveFileSaveLoadHandler] Game data saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveFileSaveLoadHandler] Failed to save game data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 세이브 파일에서 게임 데이터를 로드합니다.
    /// </summary>
    /// <param name="fileName">세이브 파일 이름 (확장자 없이)</param>
    /// <param name="dataManager">DataManager 인스턴스</param>
    /// <returns>성공 시 true</returns>
    public bool LoadSaveFile(string fileName, DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[SaveFileSaveLoadHandler] DataManager is null.");
            return false;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("[SaveFileSaveLoadHandler] File name is null or empty.");
            return false;
        }

        string filePath = GetSaveFilePath(fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveFileSaveLoadHandler] Save file not found: {filePath}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("[SaveFileSaveLoadHandler] Failed to parse save data.");
                return false;
            }

            ApplyGameState(saveData, dataManager);

            Debug.Log($"[SaveFileSaveLoadHandler] Game data loaded from: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveFileSaveLoadHandler] Failed to load game data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 모든 핸들러의 State 정보를 수집합니다.
    /// </summary>
    private GameSaveData CollectGameState(DataManager dataManager)
    {
        GameSaveData saveData = new GameSaveData();

        if (dataManager.Time != null)
        {
            saveData.year = dataManager.Time.Year;
            saveData.month = dataManager.Time.Month;
            saveData.day = dataManager.Time.Day;
            saveData.currentHour = dataManager.Time.CurrentHour;
            saveData.dayProgress = dataManager.Time.DayProgress;
            saveData.isPaused = dataManager.Time.IsPaused;
            saveData.timeSpeed = dataManager.Time.TimeSpeed;
        }

        if (dataManager.Employee != null)
        {
            Dictionary<EmployeeType, EmployeeEntry> employees = dataManager.Employee.GetAllEmployees();
            foreach (KeyValuePair<EmployeeType, EmployeeEntry> kvp in employees)
            {
                EmployeeState stateCopy = CloneEmployeeState(kvp.Value.state);
                saveData.employees.Add(new EmployeeStateSaveData(kvp.Key, stateCopy));
            }
        }

        if (dataManager.Resource != null)
        {
            Dictionary<string, ResourceEntry> resources = dataManager.Resource.GetAllResources();
            foreach (KeyValuePair<string, ResourceEntry> kvp in resources)
            {
                ResourceState stateCopy = CloneResourceState(kvp.Value.state);
                saveData.resources.Add(new ResourceStateSaveData(kvp.Key, stateCopy));
            }
        }

        if (dataManager.MarketActor != null)
        {
            Dictionary<string, MarketActorEntry> marketActors = dataManager.MarketActor.GetAllMarketActors();
            foreach (KeyValuePair<string, MarketActorEntry> kvp in marketActors)
            {
                MarketActorState stateCopy = CloneMarketActorState(kvp.Value.state);
                saveData.marketActors.Add(new MarketActorStateSaveData(kvp.Key, stateCopy));
            }
        }

        if (dataManager.Finances != null)
        {
            saveData.credit = dataManager.Finances.Credit;
            saveData.wealth = dataManager.Finances.Wealth;
            saveData.monthlyCreditHistory = new List<long>(dataManager.Finances.MonthlyCreditHistory);
            saveData.monthlyWealthHistory = new List<long>(dataManager.Finances.MonthlyWealthHistory);
        }

        if (dataManager.Research != null)
        {
            saveData.researchPoint = dataManager.Research.ResearchPoint;
            saveData.isAutoPatentMode = dataManager.Research.IsAutoPatentMode;
            
            List<ResearchEntry> researchEntries = dataManager.Research.GetAllResearchEntries();
            foreach (ResearchEntry entry in researchEntries)
            {
                ResearchState stateCopy = CloneResearchState(entry.state);
                saveData.researches.Add(new ResearchStateSaveData(entry.data.id, stateCopy));
            }
        }

        if (dataManager.Order != null)
        {
            List<OrderState> activeOrders = dataManager.Order.GetActiveOrderList();
            foreach (OrderState order in activeOrders)
            {
                OrderState orderCopy = CloneOrderState(order);
                saveData.activeOrders.Add(orderCopy);
            }
        }

        if (dataManager.News != null)
        {
            List<NewsState> activeNews = dataManager.News.GetActiveNewsList();
            foreach (NewsState news in activeNews)
            {
                NewsState newsCopy = CloneNewsState(news);
                saveData.activeNews.Add(newsCopy);
            }
        }

        // ThreadPlacement 시스템 제거로 저장하지 않음

        if (dataManager.Effect != null)
        {
            saveData.effects = CollectEffectStates(dataManager.Effect);
        }

        MainRunner mainRunner = UnityEngine.Object.FindAnyObjectByType<MainRunner>();
        if (mainRunner != null && mainRunner.GridHandler != null)
        {
            saveData.placedBuildings = mainRunner.GridHandler.ExportPlacedBuildings();
            saveData.placedRoads = mainRunner.GridHandler.ExportPlacedRoads();
        }

        return saveData;
    }

    /// <summary>
    /// EffectDataHandler의 모든 EffectState를 수집합니다.
    /// </summary>
    private EffectStateSaveData CollectEffectStates(EffectDataHandler effectHandler)
    {
        EffectStateSaveData effectSaveData = new EffectStateSaveData();

        Type effectType = typeof(EffectDataHandler);
        System.Reflection.FieldInfo globalEffectsField = effectType.GetField("_effects", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo instanceEffectsField = effectType.GetField("_instanceEffects", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (globalEffectsField != null)
        {
            Dictionary<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>> globalEffects = globalEffectsField.GetValue(effectHandler) as 
                Dictionary<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>>;

            if (globalEffects != null)
            {
                foreach (KeyValuePair<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>> targetTypePair in globalEffects)
                {
                    foreach (KeyValuePair<EffectStatType, List<EffectState>> statTypePair in targetTypePair.Value)
                    {
                        if (statTypePair.Value != null && statTypePair.Value.Count > 0)
                        {
                            List<EffectState> effectsCopy = statTypePair.Value.Select(e => CloneEffectState(e)).ToList();
                            effectSaveData.globalEffects.Add(new GlobalEffectStateSaveData(
                                targetTypePair.Key,
                                statTypePair.Key,
                                effectsCopy
                            ));
                        }
                    }
                }
            }
        }

        if (instanceEffectsField != null)
        {
            Dictionary<string, Dictionary<EffectStatType, List<EffectState>>> instanceEffects = instanceEffectsField.GetValue(effectHandler) as 
                Dictionary<string, Dictionary<EffectStatType, List<EffectState>>>;

            if (instanceEffects != null)
            {
                foreach (KeyValuePair<string, Dictionary<EffectStatType, List<EffectState>>> instancePair in instanceEffects)
                {
                    foreach (KeyValuePair<EffectStatType, List<EffectState>> statTypePair in instancePair.Value)
                    {
                        if (statTypePair.Value != null && statTypePair.Value.Count > 0)
                        {
                            List<EffectState> effectsCopy = statTypePair.Value.Select(e => CloneEffectState(e)).ToList();
                            effectSaveData.instanceEffects.Add(new InstanceEffectStateSaveData(
                                instancePair.Key,
                                statTypePair.Key,
                                effectsCopy
                            ));
                        }
                    }
                }
            }
        }

        return effectSaveData;
    }

    /// <summary>
    /// 세이브 데이터를 각 핸들러에 적용합니다.
    /// </summary>
    private void ApplyGameState(GameSaveData saveData, DataManager dataManager)
    {
        if (dataManager.Time != null)
        {
            dataManager.Time.SetDate(saveData.year, saveData.month, saveData.day);
            dataManager.Time.SetTimeSpeed(saveData.timeSpeed);
            dataManager.Time.PauseTime();
        }

        if (dataManager.Employee != null && saveData.employees != null)
        {
            foreach (EmployeeStateSaveData employeeSave in saveData.employees)
            {
                EmployeeEntry entry = dataManager.Employee.GetEmployeeEntry(employeeSave.type);
                if (entry != null)
                {
                    entry.state = employeeSave.state;
                }
            }
        }

        if (dataManager.Resource != null && saveData.resources != null)
        {
            foreach (ResourceStateSaveData resourceSave in saveData.resources)
            {
                ResourceEntry entry = dataManager.Resource.GetResourceEntry(resourceSave.resourceId);
                if (entry != null)
                {
                    entry.state = resourceSave.state;
                }
            }
        }

        if (dataManager.MarketActor != null && saveData.marketActors != null)
        {
            foreach (MarketActorStateSaveData actorSave in saveData.marketActors)
            {
                MarketActorEntry entry = dataManager.MarketActor.GetMarketActorEntry(actorSave.actorId);
                if (entry != null)
                {
                    entry.state = actorSave.state;
                }
            }
        }

        if (dataManager.Finances != null)
        {
            Type financesType = typeof(FinancesDataHandler);
            System.Reflection.FieldInfo creditField = financesType.GetField("_credit", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo wealthField = financesType.GetField("_wealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo monthlyCreditHistoryField = financesType.GetField("_monthlyCreditHistory", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo monthlyWealthHistoryField = financesType.GetField("_monthlyWealthHistory", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (creditField != null)
            {
                creditField.SetValue(dataManager.Finances, saveData.credit);
            }
            if (wealthField != null)
            {
                wealthField.SetValue(dataManager.Finances, saveData.wealth);
            }
            if (monthlyCreditHistoryField != null && saveData.monthlyCreditHistory != null)
            {
                monthlyCreditHistoryField.SetValue(dataManager.Finances, 
                    new List<long>(saveData.monthlyCreditHistory));
            }
            if (monthlyWealthHistoryField != null && saveData.monthlyWealthHistory != null)
            {
                monthlyWealthHistoryField.SetValue(dataManager.Finances, 
                    new List<long>(saveData.monthlyWealthHistory));
            }
        }

        if (dataManager.Research != null)
        {
            Type researchType = typeof(ResearchDataHandler);
            System.Reflection.FieldInfo researchPointField = researchType.GetField("_researchPoint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo isAutoPatentModeField = researchType.GetField("_isAutoPatentMode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (researchPointField != null)
            {
                researchPointField.SetValue(dataManager.Research, saveData.researchPoint);
            }
            if (isAutoPatentModeField != null)
            {
                isAutoPatentModeField.SetValue(dataManager.Research, saveData.isAutoPatentMode);
            }

            if (saveData.researches != null)
            {
                foreach (ResearchStateSaveData researchSave in saveData.researches)
                {
                    ResearchEntry entry = dataManager.Research.GetResearchEntry(researchSave.researchId);
                    if (entry != null)
                    {
                        entry.state = researchSave.state;
                    }
                }
            }

            // 세이브된 완료 상태를 기준으로 언락 상태 재계산
            dataManager.Research.RefreshUnlockedResearchStates();
        }

        if (dataManager.Order != null && saveData.activeOrders != null)
        {
            Type orderType = typeof(OrderDataHandler);
            System.Reflection.FieldInfo activeOrderListField = orderType.GetField("_activeOrderList", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (activeOrderListField != null)
            {
                List<OrderState> orderList = saveData.activeOrders.Select(o => CloneOrderState(o)).ToList();
                activeOrderListField.SetValue(dataManager.Order, orderList);
            }
        }

        if (dataManager.News != null && saveData.activeNews != null)
        {
            Type newsType = typeof(NewsDataHandler);
            System.Reflection.FieldInfo activeNewsListField = newsType.GetField("_activeNewsList", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (activeNewsListField != null)
            {
                List<NewsState> newsList = saveData.activeNews.Select(n => CloneNewsState(n)).ToList();
                activeNewsListField.SetValue(dataManager.News, newsList);
            }
        }

        // ThreadPlacement 시스템 제거로 로드하지 않음

        if (dataManager.Effect != null && saveData.effects != null)
        {
            ApplyEffectStates(saveData.effects, dataManager.Effect);
        }

        if (dataManager.PlacedLayout != null)
        {
            dataManager.PlacedLayout.SetFromSave(saveData.placedBuildings, saveData.placedRoads);
        }
    }

    /// <summary>
    /// EffectState를 EffectDataHandler에 적용합니다.
    /// </summary>
    private void ApplyEffectStates(EffectStateSaveData effectSaveData, EffectDataHandler effectHandler)
    {
        Type effectType = typeof(EffectDataHandler);
        System.Reflection.FieldInfo globalEffectsField = effectType.GetField("_effects", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo instanceEffectsField = effectType.GetField("_instanceEffects", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (globalEffectsField != null && effectSaveData.globalEffects != null)
        {
            Dictionary<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>> globalEffects = globalEffectsField.GetValue(effectHandler) as 
                Dictionary<EffectTargetType, Dictionary<EffectStatType, List<EffectState>>>;

            if (globalEffects != null)
            {
                foreach (GlobalEffectStateSaveData globalSave in effectSaveData.globalEffects)
                {
                    if (!globalEffects.ContainsKey(globalSave.targetType))
                    {
                        globalEffects[globalSave.targetType] = new Dictionary<EffectStatType, List<EffectState>>();
                    }
                    if (!globalEffects[globalSave.targetType].ContainsKey(globalSave.statType))
                    {
                        globalEffects[globalSave.targetType][globalSave.statType] = new List<EffectState>();
                    }

                    List<EffectState> effectsCopy = globalSave.effects.Select(e => CloneEffectState(e)).ToList();
                    globalEffects[globalSave.targetType][globalSave.statType] = effectsCopy;
                }
            }
        }

        if (instanceEffectsField != null && effectSaveData.instanceEffects != null)
        {
            Dictionary<string, Dictionary<EffectStatType, List<EffectState>>> instanceEffects = instanceEffectsField.GetValue(effectHandler) as 
                Dictionary<string, Dictionary<EffectStatType, List<EffectState>>>;

            if (instanceEffects != null)
            {
                foreach (InstanceEffectStateSaveData instanceSave in effectSaveData.instanceEffects)
                {
                    if (!instanceEffects.ContainsKey(instanceSave.instanceKey))
                    {
                        instanceEffects[instanceSave.instanceKey] = new Dictionary<EffectStatType, List<EffectState>>();
                    }
                    if (!instanceEffects[instanceSave.instanceKey].ContainsKey(instanceSave.statType))
                    {
                        instanceEffects[instanceSave.instanceKey][instanceSave.statType] = new List<EffectState>();
                    }

                    List<EffectState> effectsCopy = instanceSave.effects.Select(e => CloneEffectState(e)).ToList();
                    instanceEffects[instanceSave.instanceKey][instanceSave.statType] = effectsCopy;
                }
            }
        }
    }
    private EmployeeState CloneEmployeeState(EmployeeState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<EmployeeState>(json);
    }

    private ResourceState CloneResourceState(ResourceState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<ResourceState>(json);
    }

    private MarketActorState CloneMarketActorState(MarketActorState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<MarketActorState>(json);
    }

    private ResearchState CloneResearchState(ResearchState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<ResearchState>(json);
    }

    private OrderState CloneOrderState(OrderState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<OrderState>(json);
    }

    private NewsState CloneNewsState(NewsState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<NewsState>(json);
    }

    private EffectState CloneEffectState(EffectState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<EffectState>(json);
    }

    /// <summary>
    /// 저장된 세이브 파일 목록을 반환합니다.
    /// </summary>
    /// <returns>세이브 파일 이름 목록 (확장자 제외)</returns>
    public List<string> GetSaveFileList()
    {
        List<string> saveFiles = new List<string>();
        string directory = GetSaveFileDirectory();

        if (!Directory.Exists(directory))
        {
            return saveFiles;
        }

        try
        {
            string[] files = Directory.GetFiles(directory, $"*{SAVE_FILE_EXTENSION}");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                saveFiles.Add(fileName);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveFileSaveLoadHandler] Failed to get save file list: {e.Message}");
        }

        return saveFiles;
    }

    /// <summary>
    /// 세이브 파일이 존재하는지 확인합니다.
    /// </summary>
    /// <param name="fileName">세이브 파일 이름</param>
    /// <returns>파일이 존재하면 true</returns>
    public bool HasSaveFile(string fileName)
    {
        return File.Exists(GetSaveFilePath(fileName));
    }

    /// <summary>
    /// 세이브 파일을 삭제합니다.
    /// </summary>
    /// <param name="fileName">세이브 파일 이름</param>
    /// <returns>성공 시 true</returns>
    public bool DeleteSaveFile(string fileName)
    {
        string filePath = GetSaveFilePath(fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveFileSaveLoadHandler] Save file not found, nothing to delete: {filePath}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"[SaveFileSaveLoadHandler] Save file deleted: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveFileSaveLoadHandler] Failed to delete save file: {e.Message}");
            return false;
        }
    }
}
