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

    public void Init(NewsState newsState, MainCanvas mainCanvas)
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
    /// 메인 이벤트 시작 등 뉴스 데이터 없이 제목·본문(키 + _Desc)과 아이콘만 표시합니다. 효과 목록은 비웁니다.
    /// </summary>
    public void InitMainEventAnnouncement(InitialMainEventModuleData moduleData, MainCanvas mainCanvas)
    {
        base.Init();

        _dataManager = DataManager.Instance;

        string table = moduleData != null
            ? moduleData.ResolveAnnouncementLocalizationTable()
            : LocalizationUtils.TABLE_MAIN_EVENT;

        if (moduleData == null || string.IsNullOrWhiteSpace(moduleData.announcementLocalizationKey))
        {
            _titleText.text = string.Empty;
            _descriptionText.text = string.Empty;
        }
        else
        {
            string key = moduleData.announcementLocalizationKey;
            _titleText.text = key.Localize(table);
            _descriptionText.text = (key + LocalizationUtils.KEY_SUFFIX_DESC).Localize(table);
        }

        _iconImage.sprite = moduleData != null ? moduleData.announcementIcon : null;

        PoolingManager.Instance.ClearChildrenToPool(_effectScrollViewContextTransform);

        Show();
    }

    public void OnClickCloseBtn()
    {
        Close();
    }
}
