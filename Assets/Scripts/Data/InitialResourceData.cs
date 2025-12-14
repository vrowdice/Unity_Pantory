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
    /// Validates values in the Editor (prevents negative values).
    /// </summary>
    private void OnValidate()
    {
        // Prevent negative values
        if (initialCredit < 0) initialCredit = 0;
    }
}

