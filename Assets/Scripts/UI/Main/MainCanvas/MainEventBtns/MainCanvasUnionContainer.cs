using UnityEngine;
using TMPro;

public class MainCanvasUnionContainer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _unionMoodText;

    private DataManager _dataManager;

    public void Init(MainCanvas mainCanvas)
    {
        _dataManager = mainCanvas.DataManager;
        RefreshFromModule();
    }

    public void RefreshFromModule()
    {
        UnionStateModule module = _dataManager?.MainEvent?.UnionModule;
        if (module == null)
        {
            RefreshMoodText(0);
            return;
        }

        RefreshMoodText(Mathf.RoundToInt(module.UnionMood));
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
