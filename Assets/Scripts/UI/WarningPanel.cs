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

    // Start is called before the first frame update
    void Start()
    {
        if (m_ani != null)
        {
            m_ani.SetTrigger("Open");
        }
    }

    /// <summary>
    /// 경고 메시지를 설정합니다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    public void SetMessage(string message)
    {
        if (_messageText != null)
        {
            _messageText.text = message;
        }
        else
        {
            Debug.LogWarning("[WarningPanel] Message text component is not assigned.");
        }
    }

    /// <summary>
    /// 오브젝트 파괴
    /// </summary>
    void DestroyObj()
    {
        Destroy(gameObject);
    }
}
