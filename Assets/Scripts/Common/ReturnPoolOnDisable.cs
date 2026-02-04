using UnityEngine;

public class ReturnPoolOnDisable : MonoBehaviour
{
    public bool isManuallyReturning = false;

    private void OnDisable()
    {
        if (isManuallyReturning) return;
        PoolingManager.Instance?.ReturnToPool(gameObject);
    }
}