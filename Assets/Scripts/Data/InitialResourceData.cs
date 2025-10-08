using UnityEngine;

/// <summary>
/// ScriptableObject that stores initial resource data.
/// Allows initial resource balancing through the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "InitialResourceData", menuName = "Game Data/Initial Resource Data", order = 1)]
public class InitialResourceData : ScriptableObject
{
    [Header("Initial Resource Settings")]
    [Tooltip("Silver granted at game start")]
    public long initialSilver = 1000;
    
    [Tooltip("Steel granted at game start")]
    public long initialSteel = 100;
    
    [Tooltip("Wood granted at game start")]
    public long initialWood = 100;
    
    [Tooltip("Labor granted at game start")]
    public long initialLabor = 10;

    /// <summary>
    /// Applies initial resources to ResourceService.
    /// </summary>
    /// <param name="resourceService">ResourceService to apply to</param>
    public void ApplyToResourceService(ResourceService resourceService)
    {
        if (resourceService == null)
        {
            Debug.LogError("[InitialResourceData] ResourceService is null.");
            return;
        }

        resourceService.SetResource(ResourceType.Silver, initialSilver);
        resourceService.SetResource(ResourceType.Steel, initialSteel);
        resourceService.SetResource(ResourceType.Wood, initialWood);
        resourceService.SetResource(ResourceType.Labor, initialLabor);

        Debug.Log($"[InitialResourceData] Initial resources applied: " +
                  $"Silver={initialSilver}, Steel={initialSteel}, " +
                  $"Wood={initialWood}, Labor={initialLabor}");
    }

    /// <summary>
    /// Applies initial resources to GameDataManager (convenience method).
    /// </summary>
    /// <param name="dataManager">GameDataManager to apply to</param>
    public void ApplyToDataManager(GameDataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError("[InitialResourceData] GameDataManager is null.");
            return;
        }

        dataManager.AddResource(ResourceType.Silver, initialSilver);
        dataManager.AddResource(ResourceType.Steel, initialSteel);
        dataManager.AddResource(ResourceType.Wood, initialWood);
        dataManager.AddResource(ResourceType.Labor, initialLabor);

        Debug.Log($"[InitialResourceData] Initial resources applied: " +
                  $"Silver={initialSilver}, Steel={initialSteel}, " +
                  $"Wood={initialWood}, Labor={initialLabor}");
    }

    /// <summary>
    /// Validates values in the Editor (prevents negative values).
    /// </summary>
    private void OnValidate()
    {
        // Prevent negative values
        if (initialSilver < 0) initialSilver = 0;
        if (initialSteel < 0) initialSteel = 0;
        if (initialWood < 0) initialWood = 0;
        if (initialLabor < 0) initialLabor = 0;
    }
}

