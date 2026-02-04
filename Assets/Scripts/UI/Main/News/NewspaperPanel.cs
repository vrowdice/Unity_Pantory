using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewspaperPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _titleText;
    [SerializeField] Image _iconImage;
    [SerializeField] Transform _effectScrollViewContextTransform;
    [SerializeField] TextMeshProUGUI _descriptionText;

    DataManager _dataManager;
    NewsPanel _newsPanel;

    public void Init(NewsState newsState, NewsPanel newsPanel)
    {
        _dataManager = DataManager.Instance;
        _newsPanel = newsPanel;

        NewsData newsData = _dataManager.News.GetNewsData(newsState.id);

        _titleText.text = newsData.displayName;
        _iconImage.sprite = newsData.icon;
        _descriptionText.text = newsData.description;

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
        foreach (EffectData effectState in newsData.effects)
        {
            string effectDescription = effectState.displayName;
            string changeValue = _dataManager.Effect.FormatEffectValue(effectState.value, effectState.type);
            GameManager.Instance.CreateEffectTextPairPanel(_effectScrollViewContextTransform, effectDescription, changeValue, effectState.value);
        }
    }
}
