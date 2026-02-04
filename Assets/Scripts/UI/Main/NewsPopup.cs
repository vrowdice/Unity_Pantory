using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewsPopup : BasePopup
{
    [SerializeField] TextMeshProUGUI _titleText;
    [SerializeField] Image _iconImage;
    [SerializeField] Transform _effectScrollViewContextTransform;
    [SerializeField] TextMeshProUGUI _descriptionText;

    DataManager _dataManager;

    public void Init(NewsState newsState, MainCanvas mainCanvas)
    {
        base.Init();

        _dataManager = DataManager.Instance;

        NewsData newsData = _dataManager.News.GetNewsData(newsState.id);

        _titleText.text = newsData.displayName;
        _iconImage.sprite = newsData.icon;
        _descriptionText.text = newsData.description;

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
        foreach (EffectData effectState in newsData.effects)
        {
            string effectDescription = effectState.displayName ?? effectState.id;
            string changeValue = _dataManager.Effect.FormatEffectValue(effectState.value, effectState.type);
            mainCanvas.GameManager.CreateEffectTextPairPanel(_effectScrollViewContextTransform, effectDescription, changeValue, effectState.value);
        }

        Show();
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
