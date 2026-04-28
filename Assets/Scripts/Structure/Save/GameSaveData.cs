using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체 세이브 데이터를 담는 Wrapper 클래스 (JSON 직렬화용)
/// 각 핸들러의 State 정보를 저장합니다.
/// </summary>
[Serializable]
public class GameSaveData
{
    // Time Data
    public int year;
    public int month;
    public int day;
    public int currentHour;
    public float dayProgress;
    public bool isPaused;
    public float timeSpeed;

    // Employee Data
    public List<EmployeeStateSaveData> employees = new List<EmployeeStateSaveData>();

    // Resource Data
    public List<ResourceStateSaveData> resources = new List<ResourceStateSaveData>();

    // MarketActor Data
    public List<MarketActorStateSaveData> marketActors = new List<MarketActorStateSaveData>();

    // Finances Data
    public long credit;
    public long wealth;
    public List<long> monthlyCreditHistory = new List<long>();
    public List<long> monthlyWealthHistory = new List<long>();

    // Research Data
    public long researchPoint;
    public bool isAutoPatentMode;
    public List<ResearchStateSaveData> researches = new List<ResearchStateSaveData>();

    // Order Data
    public List<OrderState> activeOrders = new List<OrderState>();

    // News Data
    public List<NewsState> activeNews = new List<NewsState>();

    // Effect Data
    public EffectStateSaveData effects = new EffectStateSaveData();

    // Player (tutorial auto-show pending by owner object name, JsonUtility는 Dictionary 미지원)
    public List<TutorialAutoShowPendingSaveData> tutorialAutoShowPending = new List<TutorialAutoShowPendingSaveData>();

    // Placed Objects (Main building/road layout)
    public List<PlacedBuildingSaveData> placedBuildings = new List<PlacedBuildingSaveData>();
    public List<PlacedRoadSaveData> placedRoads = new List<PlacedRoadSaveData>();
}

/// <summary>
/// 튜토리얼 자동 표시 여부 (TutorialBase 오브젝트 이름 기준).
/// pendingAutoShow == true: 아직 끝까지 보지 않았거나 자동 표시 대기, false: 완료 후 자동 표시 안 함.
/// </summary>
[Serializable]
public class TutorialAutoShowPendingSaveData
{
    public string ownerGameObjectName;
    public bool pendingAutoShow;
}

/// <summary>
/// ResourcePacket 저장용 데이터
/// </summary>
[Serializable]
public class ResourcePacketSaveData
{
    public string id;
    public int amount;
    public FlowDirection direction;

    public ResourcePacketSaveData() { }

    public ResourcePacketSaveData(string id, int amount, FlowDirection direction = FlowDirection.None)
    {
        this.id = id;
        this.amount = amount;
        this.direction = direction;
    }
}

/// <summary>
/// 배치된 BuildingObject 저장용 데이터
/// </summary>
[Serializable]
public class PlacedBuildingSaveData
{
    public string buildingDataId;
    public int originX;
    public int originY;
    public int rotation;

    public string selectedResourceId;
    public float workProgress;
    public int assignedWorkers;
    public int assignedTechnicians;

    public List<ResourcePacketSaveData> inputBuffer = new List<ResourcePacketSaveData>();
    public List<ResourcePacketSaveData> outputBuffer = new List<ResourcePacketSaveData>();
}

/// <summary>
/// 배치된 RoadObject 저장용 데이터
/// </summary>
[Serializable]
public class PlacedRoadSaveData
{
    public int x;
    public int y;
    public int rotation;
    public string roadDataId;
    public string sourceBuildingDataId;

    public List<ResourcePacketSaveData> buffer = new List<ResourcePacketSaveData>();
}

/// <summary>
/// Employee State 저장용 데이터
/// </summary>
[Serializable]
public class EmployeeStateSaveData
{
    public EmployeeType type;
    public EmployeeState state;

    public EmployeeStateSaveData() { }

    public EmployeeStateSaveData(EmployeeType type, EmployeeState state)
    {
        this.type = type;
        this.state = state;
    }
}

/// <summary>
/// Resource State 저장용 데이터
/// </summary>
[Serializable]
public class ResourceStateSaveData
{
    public string resourceId;
    public ResourceState state;

    public ResourceStateSaveData() { }

    public ResourceStateSaveData(string resourceId, ResourceState state)
    {
        this.resourceId = resourceId;
        this.state = state;
    }
}

/// <summary>
/// MarketActor State 저장용 데이터
/// </summary>
[Serializable]
public class MarketActorStateSaveData
{
    public string actorId;
    public MarketActorState state;

    public MarketActorStateSaveData() { }

    public MarketActorStateSaveData(string actorId, MarketActorState state)
    {
        this.actorId = actorId;
        this.state = state;
    }
}

/// <summary>
/// Research State 저장용 데이터
/// </summary>
[Serializable]
public class ResearchStateSaveData
{
    public string researchId;
    public ResearchState state;

    public ResearchStateSaveData() { }

    public ResearchStateSaveData(string researchId, ResearchState state)
    {
        this.researchId = researchId;
        this.state = state;
    }
}

/// <summary>
/// Effect State 저장용 데이터
/// </summary>
[Serializable]
public class EffectStateSaveData
{
    public List<GlobalEffectStateSaveData> globalEffects = new List<GlobalEffectStateSaveData>();
    public List<InstanceEffectStateSaveData> instanceEffects = new List<InstanceEffectStateSaveData>();

    public EffectStateSaveData() { }
}

/// <summary>
/// 전역 Effect State 저장용 데이터
/// </summary>
[Serializable]
public class GlobalEffectStateSaveData
{
    public EffectTargetType targetType;
    public EffectStatType statType;
    public List<EffectState> effects = new List<EffectState>();

    public GlobalEffectStateSaveData() { }

    public GlobalEffectStateSaveData(EffectTargetType targetType, EffectStatType statType, List<EffectState> effects)
    {
        this.targetType = targetType;
        this.statType = statType;
        this.effects = effects;
    }
}

/// <summary>
/// 인스턴스별 Effect State 저장용 데이터
/// </summary>
[Serializable]
public class InstanceEffectStateSaveData
{
    public string instanceKey;
    public EffectStatType statType;
    public List<EffectState> effects = new List<EffectState>();

    public InstanceEffectStateSaveData() { }

    public InstanceEffectStateSaveData(string instanceKey, EffectStatType statType, List<EffectState> effects)
    {
        this.instanceKey = instanceKey;
        this.statType = statType;
        this.effects = effects;
    }
}
