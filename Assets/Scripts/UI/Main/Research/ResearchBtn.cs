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
        Refresh(researchEntry);
        EnsureClickBound();
    }

    public void Refresh(ResearchEntry researchEntry)
    {
        _researchEntry = researchEntry;
        _image.sprite = researchEntry.data.icon;
        _text.text = researchEntry.data.id.Localize(LocalizationUtils.TABLE_RESEARCH);
        _focusedImage.SetActive(researchEntry.state.isCompleted);

        Evo.UI.Button button = ResolveButton();
        if (button != null)
            button.interactable = researchEntry.state.isUnlocked;
    }

    protected override void HandleClick()
    {
        _researchCanvas.PanelUIManager.ShowResearchInfoPopup(_researchEntry);
    }
}
