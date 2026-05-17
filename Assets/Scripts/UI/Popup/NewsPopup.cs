using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewsPopup : PopupBase
{
    [SerializeField] TextMeshProUGUI _titleText;
    [SerializeField] Image _iconImage;
    [SerializeField] Transform _effectScrollViewContextTransform;
    [SerializeField] TextMeshProUGUI _descriptionText;

    DataManager _dataManager;

    public void Init(NewsState newsState)
    {
        base.Init();

        _dataManager = DataManager.Instance;

        NewsData newsData = _dataManager.News.GetNewsData(newsState.id);

        _titleText.text = newsState.id.Localize(LocalizationUtils.TABLE_NEWS);
        _iconImage.sprite = newsData.icon;
        _descriptionText.text = (newsState.id + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_NEWS);

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);
        foreach (EffectData effectData in newsData.effects)
        {
            UIManager.Instance.CreateEffectTextPairPanel(_effectScrollViewContextTransform, new EffectState(effectData));
        }

        Show();
    }

    /// <summary>
    /// 메인 이벤트 시작 등 뉴스
    /// </summary>
    public void InitMainEventAnnouncement(InitialMainEventModuleData moduleData)
    {
        base.Init();

        string table = LocalizationUtils.TABLE_MAIN_EVENT;
        string key = moduleData.announcementLocalizationKey;
        _titleText.text = key.Localize(table);
        _descriptionText.text = (key + LocalizationUtils.KEY_SUFFIX_DESC).Localize(table);

        _iconImage.sprite = moduleData.announcementIcon;
        SoundManager.Instance.PlaySFX(moduleData.openNewsAudio);

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);

        Show();
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
