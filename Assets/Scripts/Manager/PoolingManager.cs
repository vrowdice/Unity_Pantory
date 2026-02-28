using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 게임 오브젝트 풀링을 관리하는 매니저 클래스입니다.
/// 오브젝트의 생성과 파괴를 최적화하여 성능을 향상시킵니다.
/// </summary>
public class PoolingManager : Singleton<PoolingManager>
{
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    [SerializeField] private Transform poolContainer;
    [SerializeField] private Transform _canvasTransform;

    const int maxPoolSize = 20;

    /// <summary>
    /// PoolingManager를 초기화합니다.
    /// </summary>
    /// <param name="gameManager">GameManager 참조</param>
    public void Init(GameManager gameManager)
    {
        ClearAllPools();

        if (poolContainer == null)
        {
            GameObject containerObj = new GameObject("PoolContainer");
            containerObj.transform.SetParent(transform);
            poolContainer = containerObj.transform;
        }
    }

    /// <summary>
    /// 특정 프리팹에 대한 오브젝트 풀 생성
    /// </summary>
    /// <param name="_prefab">풀링할 프리팹</param>
    /// <param name="_count">초기 생성 개수</param>
    public void CreatePool(GameObject _prefab, int _count)
    {
        string prefabName = _prefab.name;
        if (poolDictionary.ContainsKey(prefabName))
        {
            Debug.LogWarning($"Pool for {prefabName} already exists!");
            return;
        }

        poolDictionary[prefabName] = new Queue<GameObject>();
        for (int i = 0; i < _count; i++)
        {
            GameObject obj = InstantiatePoolObject(_prefab, poolContainer);

            ReturnPoolOnDisable returnComp = obj.GetComponent<ReturnPoolOnDisable>();
            if (returnComp != null)
            {
                returnComp.isManuallyReturning = true;
            }

            obj.SetActive(false);

            if (returnComp != null)
            {
                returnComp.isManuallyReturning = false;
            }

            poolDictionary[prefabName].Enqueue(obj);
        }
    }

    /// <summary>
    /// 풀링 오브젝트 생성 시 ReturnPoolOnDisable 컴포넌트 추가
    /// </summary>
    /// <param name="_prefab">생성할 프리팹</param>
    /// <param name="_parent">부모 트랜스폼</param>
    /// <returns>생성된 게임오브젝트</returns>
    public GameObject InstantiatePoolObject(GameObject _prefab, Transform _parent = null, int _count = maxPoolSize )
    {
        string prefabName = _prefab.name;
        if (!poolDictionary.ContainsKey(prefabName))
        {
            CreatePool(_prefab, _count);
        }

        GameObject obj = _parent
            ? Instantiate(_prefab, _parent)
            : Instantiate(_prefab, poolContainer);

        if (obj.GetComponent<ReturnPoolOnDisable>() == null && !(_prefab.transform is RectTransform))
        {
            obj.AddComponent<ReturnPoolOnDisable>();
        }

        return obj;
    }

    /// <summary>
    /// 오브젝트 풀에서 오브젝트 가져오기
    /// </summary>
    /// <param name="_prefab">가져올 프리팹</param>
    /// <returns>활성화된 오브젝트</returns>
    public GameObject GetPooledObject(GameObject _prefab)
    {
        string prefabName = _prefab.name;
        if (!poolDictionary.ContainsKey(prefabName))
        {
            CreatePool(_prefab, maxPoolSize);
        }

        Queue<GameObject> pool = poolDictionary[prefabName];
        if (pool.Count == 0)
        {
            GameObject newObj = InstantiatePoolObject(_prefab, poolContainer);
            return newObj;
        }

        GameObject obj = pool.Dequeue();
        obj.transform.localScale = Vector3.one;
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 위치, Z축 회전, 부모 트랜스폼을 설정하여 풀에서 오브젝트 가져오기
    /// </summary>
    /// <param name="_prefab">프리팹</param>
    /// <param name="_position">위치</param>
    /// <param name="_zRotation">Z축 회전 각도 (degree)</param>
    /// <param name="_parent">부모 트랜스폼</param>
    /// <returns>활성화된 오브젝트</returns>
    public GameObject GetPooledObject(GameObject _prefab, Vector2 _position, float _zRotation = 0f, Transform _parent = null)
    {
        GameObject obj = GetPooledObject(_prefab);

        obj.transform.position = _position;
        obj.transform.eulerAngles = new Vector3(0, 0, _zRotation);
        obj.transform.SetParent((_parent != null) ? _parent : poolContainer, true);
        obj.transform.localScale = Vector3.one;

        return obj;
    }

    public GameObject GetPooledObjectFromCanvas(GameObject _prefab, Vector2 _position = default)
    {
        GameObject obj = GetPooledObject(_prefab);
        obj.transform.SetParent(_canvasTransform, true);
        obj.transform.GetComponent<RectTransform>().anchoredPosition = _position;
        obj.transform.localScale = Vector3.one;
        return obj;
    }

    /// <summary>
    /// 오브젝트를 풀에 반환
    /// </summary>
    /// <param name="_obj">반환할 오브젝트</param>
    public void ReturnToPool(GameObject _obj)
    {
        string prefabName = _obj.name.Replace("(Clone)", "");

        if (!poolDictionary.ContainsKey(prefabName))
        {
            Destroy(_obj);
            return;
        }

        ReturnPoolOnDisable returnComp = _obj.GetComponent<ReturnPoolOnDisable>();
        if (returnComp != null)
        {
            returnComp.isManuallyReturning = true;
        }

        _obj.SetActive(false);

        if (_obj.transform is RectTransform)
        {
            _obj.transform.SetParent(_canvasTransform);
        }
        else
        {
            _obj.transform.SetParent(poolContainer);
        }

        poolDictionary[prefabName].Enqueue(_obj);

        if (returnComp != null)
        {
            returnComp.isManuallyReturning = false;
        }
    }

    /// <summary>
    /// 특정 타입의 모든 오브젝트 풀 초기화
    /// </summary>
    /// <param name="_prefab">초기화할 프리팹</param>
    public void ClearPool(GameObject _prefab)
    {
        string prefabName = _prefab.name;

        if (poolDictionary.ContainsKey(prefabName))
        {
            foreach (GameObject obj in poolDictionary[prefabName])
            {
                Destroy(obj);
            }
            poolDictionary[prefabName].Clear();
        }
    }

    /// <summary>
    /// 모든 오브젝트 풀 초기화
    /// </summary>
    public void ClearAllPools()
    {
        foreach (Queue<GameObject> pool in poolDictionary.Values)
        {
            foreach (GameObject obj in pool)
            {
                Destroy(obj);
            }
        }
        poolDictionary.Clear();
    }
    
    /// <summary>
    /// 부모 Transform 하위의 모든 자식 오브젝트를 풀로 반환합니다.
    /// 풀에 없는 오브젝트는 파괴됩니다.
    /// </summary>
    public void ClearChildrenToPool(Transform parent)
    {
        if (parent == null) return;

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in parent)
        {
            children.Add(child.gameObject);
        }

        foreach (GameObject childObj in children)
        {
            ReturnToPool(childObj);
        }
    }
}
