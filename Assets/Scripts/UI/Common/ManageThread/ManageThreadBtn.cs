using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManageThreadBtn : MonoBehaviour
{
    [SerializeField] private Image _threadImage = null;
    [SerializeField] private TextMeshProUGUI _titleText = null;

    private string _threadId = string.Empty;
    private System.Action<string> _onClickCallback = null;
    private System.Action<string> _onEditCallback = null;
    private System.Action<string> _onDeleteCallback = null;

    public void Init(ThreadState thread, Sprite previewSprite, System.Action<string> onClick, System.Action<string> onEdit, System.Action<string> onDelete)
    {
        if (thread == null)
            return;

        _threadId = thread.threadId;
        _onClickCallback = onClick;
        _onEditCallback = onEdit;
        _onDeleteCallback = onDelete;

        // 제목 설정
        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrEmpty(thread.threadName) ? thread.threadId : thread.threadName;
        }

        // 미리보기 이미지 설정
        if (_threadImage != null)
        {
            if (previewSprite != null)
            {
                _threadImage.sprite = previewSprite;
                _threadImage.enabled = true;
            }
            else
            {
                _threadImage.enabled = false;
            }
        }
    }

    public void OnClick()
    {
        if (!string.IsNullOrEmpty(_threadId) && _onClickCallback != null)
        {
            _onClickCallback(_threadId);
        }
    }

    public void OnClickEdit()
    {
        if (!string.IsNullOrEmpty(_threadId) && _onEditCallback != null)
        {
            _onEditCallback(_threadId);
        }
    }

    public void OnClickDelete()
    {
        if (!string.IsNullOrEmpty(_threadId) && _onDeleteCallback != null)
        {
            _onDeleteCallback(_threadId);
        }
    }
}
