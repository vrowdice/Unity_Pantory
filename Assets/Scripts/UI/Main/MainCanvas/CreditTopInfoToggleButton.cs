using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 크레딧 상세 팝업 토글. PointerDown에서 먼저 처리해 바깥 클릭 닫기보다 우선합니다.
/// </summary>
public class CreditTopInfoToggleButton : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.ToggleCreditTopInfoPopup();
    }
}
