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
    }

    /// <summary>
    /// Validates values in the Editor (prevents negative values).
    /// </summary>
    private void OnValidate()
    {
        // Prevent negative values
        if (initialCredit < 0) initialCredit = 0;
    }
}

