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

        _titleText.text = newsState.id.Localize(LocalizationUtils.TABLE_NEWS);
        _iconImage.sprite = newsData.icon;
        _descriptionText.text = newsState.id.Localize(LocalizationUtils.TABLE_NEWS_DESCRIPTION);

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);

        foreach (EffectData effectData in newsData.effects)
        {
            EffectState liveEffect = _dataManager.Effect.GetEffect(effectData, effectData.targetId);
            GameManager.Instance.CreateEffectTextPairPanel(_effectScrollViewContextTransform, liveEffect, Color.black);
        }
    }
}
