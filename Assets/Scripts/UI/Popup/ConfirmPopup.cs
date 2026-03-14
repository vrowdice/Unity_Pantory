using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 확인 팝업을 표시하는 클래스입니다.
/// 확인/거부 버튼을 통해 사용자의 선택을 처리합니다.
/// </summary>
public class ConfirmPopup : PopupBase
{
    [SerializeField] private TextMeshProUGUI _text;

    private Action _onConfirm;

    /// <summary>
    /// ConfirmPopup을 초기화합니다.
    /// </summary>
    /// <param name="messageKey">ConfirmMessage 테이블의 로컬라이즈 키</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 실행할 함수</param>
    public void Init(string messageKey, Action onConfirm)
    {
        base.Init();

        _text.text = messageKey.Localize(LocalizationUtils.TABLE_COMMON);
        _onConfirm = onConfirm;

        Show();
    }

    /// <summary>
    /// 확인 버튼 클릭 시 호출됩니다.
    /// 등록된 함수를 실행하고 팝업을 닫습니다.
    /// </summary>
    public void OnClickConfirm()
    {
        _onConfirm?.Invoke();
        ClosePopup();
    }

    /// <summary>
    /// 거부 버튼 클릭 시 호출됩니다.
    /// 팝업을 닫습니다.
    /// </summary>
    public void OnClickRefuse()
    {
        ClosePopup();
    }

    /// <summary>
    /// 팝업을 닫고 삭제합니다.
    /// </summary>
    private void ClosePopup()
    {
        Destroy(gameObject);
    }
}
