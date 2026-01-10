using UnityEngine;
using TMPro;
using System;

public class EnterNamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nameInputField = null;

    private Action<string> _onConfirm;

    /// <summary>
    /// 패널을 초기화합니다.
    /// </summary>
    /// <param name="message">안내 메시지</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 호출될 콜백</param>
    public void Init(Action<string> onConfirm)
    {
        _onConfirm = onConfirm;

        // 입력 필드 초기화
        if (_nameInputField != null)
        {
            _nameInputField.text = string.Empty;
            _nameInputField.ActivateInputField();
        }
    }

    /// <summary>
    /// 확인 버튼 클릭 시 호출됩니다.
    /// 입력된 이름을 콜백으로 전달하고 패널을 닫습니다.
    /// </summary>
    public void OnClickOk()
    {
        if (_nameInputField == null)
        {
            Debug.LogWarning("[EnterNamePanel] Name input field is null.");
            Destroy(gameObject);
            return;
        }

        string enteredName = _nameInputField.text;

        // 콜백 호출
        _onConfirm?.Invoke(enteredName);

        // 패널 닫기
        Destroy(gameObject);
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void OnClickClose()
    {
        Destroy(gameObject);
    }
}
