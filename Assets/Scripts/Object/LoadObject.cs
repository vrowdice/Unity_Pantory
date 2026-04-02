using UnityEngine;

/// <summary>
/// 로드(운반물) 오브젝트의 뷰/콜라이더 관리.
/// 실제 상태/데이터는 PlacedLoadState 에서 관리합니다.
/// </summary>
public class LoadObject : MonoBehaviour
{
    [SerializeField] private PlacedLoadState _state;

    public PlacedLoadState State => _state;

    private void Awake()
    {
        EnsureStateComponent();
    }

    public void Init(PlacedLoadState state)
    {
        if (state == null)
        {
            return;
        }

        _state = state;
    }

    private void EnsureStateComponent()
    {
        if (_state == null)
        {
            _state = GetComponent<PlacedLoadState>();
            if (_state == null)
            {
                _state = gameObject.AddComponent<PlacedLoadState>();
            }
        }
    }
}
