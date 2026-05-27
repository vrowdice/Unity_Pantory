using UnityEngine;
using UnityEngine.UI;
using Evo.UI;
using TMPro;

public class ResearchBtn : BtnBase
{
    [SerializeField] private Image _image;
    [SerializeField] private GameObject _focusedImage;
    [SerializeField] private TextMeshProUGUI _text;

    private ResearchEntry _researchEntry;
    private ResearchCanvas _researchCanvas;

    public void Init(ResearchEntry researchEntry, ResearchCanvas researchCanvas)
    {
        _researchEntry = researchEntry;
        _researchCanvas = researchCanvas;
        _image.sprite = researchEntry.data.icon;
        _text.text = researchEntry.data.id.Localize(LocalizationUtils.TABLE_RESEARCH);
        if(researchEntry.state.isCompleted)
            _focusedImage.SetActive(true);
        else
            _focusedImage.SetActive(false);

        Evo.UI.Button button = ResolveButton();
        if (button != null)
        {
            button.interactable = researchEntry.state.isUnlocked;
        }
    }

    protected override void HandleClick()
    {
        _researchCanvas.PanelUIManager.ShowResearchInfoPopup(_researchEntry);
    }
}
