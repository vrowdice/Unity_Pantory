using UnityEngine;
using UnityEngine.UI;

public class ToggleBtn : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image _targetImage;

    [Header("Panel (Optional)")]
    [SerializeField] private PanelDoAni _targetPanel;   // 여기서 PanelDoAni를 인스펙터에 할당

    [Header("Sprites")]
    [SerializeField] private Sprite _closedSprite; // 닫힌 상태 아이콘
    [SerializeField] private Sprite _openedSprite; // 열린 상태 아이콘

    /// <summary>
    /// 현재 열림/닫힘 상태
    /// </summary>
    public bool IsOpened { get; private set; }

    private void Start()
    {
        // 시작 시 패널 상태와 아이콘을 동기화
        if (_targetPanel != null)
        {
            if (_targetPanel.IsOpen)
            {
                SetOpened();
            }
            else
            {
                SetClosed();
            }
        }
        else
        {
            SetOpened();
        }
    }

    /// <summary>
    /// 현재 상태를 토글합니다.
    /// (버튼 OnClick에 연결해서 사용)
    /// </summary>
    public void OnClickToggleBtn()
    {
        if (_targetImage == null)
        {
            Debug.LogWarning("[ToggleBtn] Target Image is not assigned.");
            return;
        }

        if (IsOpened)
            SetClosed();
        else
            SetOpened();
    }

    /// <summary>
    /// 외부에서 강제로 열린 상태로 설정
    /// </summary>
    public void SetOpened()
    {
        IsOpened = true;
        if (_targetImage != null && _openedSprite != null)
        {
            _targetImage.sprite = _openedSprite;
        }

        // 연결된 패널이 있으면 함께 열기
        if (_targetPanel != null && !_targetPanel.IsOpen)
        {
            _targetPanel.OpenPanel();
        }
    }

    /// <summary>
    /// 외부에서 강제로 닫힌 상태로 설정
    /// </summary>
    public void SetClosed()
    {
        IsOpened = false;
        if (_targetImage != null && _closedSprite != null)
        {
            _targetImage.sprite = _closedSprite;
        }

        // 연결된 패널이 있으면 함께 닫기
        if (_targetPanel != null && _targetPanel.IsOpen)
        {
            _targetPanel.ClosePanel();
        }
    }
}
