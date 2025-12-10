using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
public class ResourceItem : MonoBehaviour
{
    private List<Vector3> _pathPoints;
    private int _targetIndex = 0;
    private float _speed = 2.0f; // 이동 속도
    private IObjectPool<ResourceItem> _managedPool;
    public void Initialize(List<Vector3> pathPoints, Sprite resourceSprite, float speed, IObjectPool<ResourceItem> pool)
    {
        _pathPoints = pathPoints;
        _speed = speed;
        _targetIndex = 0;
        _managedPool = pool;

        // 스프라이트 설정
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) 
        {
            renderer.sprite = resourceSprite;
            renderer.sortingOrder = 5; // 도로(보통 0)보다 높게 설정
        }

        // 시작 위치로 이동
        if (_pathPoints != null && _pathPoints.Count > 0)
        {
            transform.position = _pathPoints[0];
        }
    }

    void Update()
    {
        if (_pathPoints == null || _targetIndex >= _pathPoints.Count)
        {
            ReturnToPool();
            return;
        }

        Vector3 targetPos = _pathPoints[_targetIndex];
        
        transform.position = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            _targetIndex++;
        }
    }
    private void ReturnToPool()
    {
        if (_managedPool != null)
        {
            _managedPool.Release(this); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
}