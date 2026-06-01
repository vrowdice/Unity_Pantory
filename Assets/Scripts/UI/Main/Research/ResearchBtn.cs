using UnityEngine;
using UnityEngine.UI;
using Evo.UI;
using TMPro;

public class ResearchBtn : EntryListBtnBase
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
        Refresh();
        EnsureClickBound();
    }

    public override void Refresh()
    {
        if (_researchEntry?.data == null)
            return;

        _image.sprite = _researchEntry.data.icon;
        _text.text = _researchEntry.data.id.Localize(LocalizationUtils.TABLE_RESEARCH);
        _focusedImage.SetActive(_researchEntry.state.isCompleted);

        Evo.UI.Button button = ResolveButton();
        if (button != null)
            button.interactable = _researchEntry.state.isUnlocked;
    }

    public Transform GetCompleteAnimationTarget()
    {
        if (_focusedImage != null && _focusedImage.activeSelf)
            return _focusedImage.transform;

        return transform;
    }

    protected override void HandleClick()
    {
        _researchCanvas.PanelUIManager.ShowResearchInfoPopup(_researchEntry);
    }
}
