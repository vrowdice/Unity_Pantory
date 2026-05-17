using UnityEngine;
using TMPro;

public class MainCanvasUnionContainer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _unionMoodText;

    private DataManager _dataManager;

    public void Init(MainCanvas mainCanvas)
    {
        _dataManager = mainCanvas.DataManager;
        RefreshMoodText(0);
    }

    public void RefreshMoodText(int mood)
    {
        if (_unionMoodText == null)
        {
            return;
        }

        _unionMoodText.text = $"{mood}%";
    }

    public void OnClick()
    {
        UIManager.Instance?.ShowUnionPopup();
    }
}
