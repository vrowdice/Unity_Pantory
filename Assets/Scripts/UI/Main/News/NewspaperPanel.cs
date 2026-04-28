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
    NewsCanvas _newsPanel;

    public void Init(NewsState newsState, NewsCanvas newsPanel)
    {
        _dataManager = newsPanel.Host.DataManager;
        _newsPanel = newsPanel;

        NewsData newsData = _dataManager.News.GetNewsData(newsState.id);

        _titleText.text = newsState.id.Localize(LocalizationUtils.TABLE_NEWS);
        _iconImage.sprite = newsData.icon;
        _descriptionText.text = (newsState.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_NEWS);

/*        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);

        foreach (EffectData effectData in newsData.effects)
        {
            EffectState liveEffect = _dataManager.Effect.GetEffect(effectData, effectData.targetId);
            UIManager.Instance.CreateEffectTextPairPanel(_effectScrollViewContextTransform, liveEffect, Color.black);
        }*/
    }
}
