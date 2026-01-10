using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WarningPanel : MonoBehaviour
{
    /// <summary>
    /// 애니메이터
    /// </summary>
    public Animator m_ani = null;

    /// <summary>
    /// 경고 메시지를 표시할 텍스트
    /// </summary>
    [SerializeField] private TextMeshProUGUI _messageText;

    public void Init(string message)
    {
        _messageText.text = message;
        m_ani.SetTrigger("Open");
    }

    /// <summary>
    /// 오브젝트 파괴
    /// </summary>
    void DestroyObj()
    {
        Destroy(gameObject);
    }
}
