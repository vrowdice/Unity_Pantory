using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchBtn : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _text;

    public void OnInitialize(ResearchEntry researchEntry)
    {
        _text.text = researchEntry.data.displayName;
    }

    public void OnClick()
    {

    }
}
