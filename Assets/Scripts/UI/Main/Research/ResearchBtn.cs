using UnityEngine;
using UnityEngine.UI;
using Evo.UI;
using TMPro;

public class ResearchBtn : MonoBehaviour
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

        if(researchEntry.state.isUnlocked)
            GetComponent<Evo.UI.Button>().interactable = true;
        else
            GetComponent<Evo.UI.Button>().interactable = false;
    }

    public void OnClick()
    {
        _researchCanvas.PanelUIManager.ShowResearchInfoPopup(_researchEntry);
    }
}
