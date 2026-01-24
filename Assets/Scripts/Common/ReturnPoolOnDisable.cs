using UnityEngine;

public class ReturnPoolOnDisable : MonoBehaviour
{
    private void OnDisable()
    {
        PoolingManager.Instance?.ReturnToPool(gameObject);
    }
}