using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WarningPanel : MonoBehaviour
{
    /// <summary>
    /// 애니메이션
    /// </summary>
    public Animator m_ani = null;

    // Start is called before the first frame update
    void Start()
    {
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
