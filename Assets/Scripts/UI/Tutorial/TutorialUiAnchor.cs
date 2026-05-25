using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 UI 포커스 대상을 문자열 ID로 등록합니다.
/// </summary>
public class TutorialUiAnchor : MonoBehaviour
{
    private static readonly Dictionary<string, TutorialUiAnchor> Registry = new Dictionary<string, TutorialUiAnchor>();

    [SerializeField] private string _anchorId;

    public string AnchorId => _anchorId;
    public GameObject Target => gameObject;

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void Register()
    {
        if (string.IsNullOrEmpty(_anchorId))
            return;

        Registry[_anchorId] = this;
    }

    private void Unregister()
    {
        if (string.IsNullOrEmpty(_anchorId))
            return;

        if (Registry.TryGetValue(_anchorId, out TutorialUiAnchor current) && current == this)
            Registry.Remove(_anchorId);
    }

    public static GameObject Resolve(string anchorId)
    {
        if (string.IsNullOrEmpty(anchorId))
            return null;

        return Registry.TryGetValue(anchorId, out TutorialUiAnchor anchor) ? anchor.Target : null;
    }
}
