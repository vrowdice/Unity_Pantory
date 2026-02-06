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

        _titleText.text = newsState.id.Localize(LocalizationUtils.TABLE_NEWS);
        _iconImage.sprite = newsData.icon;
        _descriptionText.text = newsState.id.Localize(LocalizationUtils.TABLE_NEWS_DESCRIPTION);

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
        foreach (EffectData effectData in newsData.effects)
        {
            mainCanvas.GameManager.CreateEffectTextPairPanel(_effectScrollViewContextTransform, new EffectState(effectData));
        }

        Show();
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
