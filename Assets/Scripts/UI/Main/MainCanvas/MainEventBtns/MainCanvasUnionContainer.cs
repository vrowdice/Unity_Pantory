using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

public class MainCanvasUnionContainer : MonoBehaviour
{
    [FormerlySerializedAs("_unionMoodText")]
    [SerializeField] private TextMeshProUGUI _unionCohesionProgressText;
    [SerializeField] private TextMeshProUGUI _remainDateText;

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
            gameObject.SetActive(false);
            return;
        }
        
        _unionCohesionProgressText.text = $"{module.UnionCohesionProgress}%";
        _remainDateText.text = $"{module.RemainingDays}";
    }

    public void OnClick()
    {
        UIManager.Instance?.ShowUnionPopup();
    }
}
