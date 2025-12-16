using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchBtn : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private GameObject _focusedImage;
    [SerializeField] private TextMeshProUGUI _text;

    public void OnInitialize(ResearchEntry researchEntry)
    {
        _image.sprite = researchEntry.data.icon;
        _text.text = researchEntry.data.displayName;

        if(researchEntry.state.isCompleted)
        {
            _focusedImage.SetActive(true);
        }
        else
        {
            _focusedImage.SetActive(false);
        }
    }

    public void OnClick()
    {

    }
}
