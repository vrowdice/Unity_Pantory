using UnityEngine;
using System.Collections.Generic;

public class ResourceItem : MonoBehaviour
{
    private List<Vector3> _pathPoints;
    private int _targetIndex = 0;
    private float _speed = 2.0f; // 이동 속도

    public void Initialize(List<Vector3> pathPoints, Sprite resourceSprite, float speed)
    {
        _pathPoints = pathPoints;
        _speed = speed;
        _targetIndex = 0;

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
            Destroy(gameObject); // 목적지 도착 시 삭제
            return;
        }

        Vector3 targetPos = _pathPoints[_targetIndex];
        
        // 목표 지점을 향해 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);

        // 목표 지점에 거의 도달했으면 다음 지점으로
        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            _targetIndex++;
        }
    }
}