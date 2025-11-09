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
    public long initialCredit = 1000;

    [System.Serializable]
    public class ResourceAmount
    {
        public string resourceId;
        public long amount;
    }

    [Tooltip("Initial resource amounts")]
    public ResourceAmount[] initialResources = new ResourceAmount[0];

    /// <summary>
    /// Applies initial resources to ResourceService and FinancesService.
    /// </summary>
    /// <param name="resourceService">ResourceService to apply to</param>
    /// <param name="financesService">FinancesService to apply to</param>
    public void ApplyToServices(ResourceDataHandler resourceService, FinancesDataHandler financesService)
    {
        if (resourceService == null)
        {
            Debug.LogError("[InitialResourceData] ResourceService is null.");
            return;
        }

        if (financesService == null)
        {
            Debug.LogError("[InitialResourceData] FinancesService is null.");
            return;
        }

        // Silver 적용
        financesService.SetCredit(initialCredit);

        // 각 자원 적용
        foreach (var resource in initialResources)
        {
            if (string.IsNullOrEmpty(resource.resourceId))
            {
                Debug.LogWarning("[InitialResourceData] Resource ID is empty.");
                continue;
            }

            resourceService.SetResource(resource.resourceId, resource.amount);
            Debug.Log($"[InitialResourceData] Initial resource applied: {resource.resourceId} = {resource.amount}");
        }
    }

    /// <summary>
    /// Validates values in the Editor (prevents negative values).
    /// </summary>
    private void OnValidate()
    {
        // Prevent negative values
        if (initialCredit < 0) initialCredit = 0;

        foreach (var resource in initialResources)
        {
            if (resource.amount < 0) resource.amount = 0;
        }
    }
}

