using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 이동 및 로딩 패널 관리를 담당하는 매니저
/// </summary>
public class SceneLoadManager : Singleton<SceneLoadManager>
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private Slider _progressBar;

    [Header("Settings")]
    [SerializeField] private float _fadeDuration = 0.5f;

    private bool _isLoading = false;

    protected override void Awake()
    {
        base.Awake();
        
        if (Instance != this) return;

        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
        }

        if (_progressBar != null)
        {
            _progressBar.value = 0f;
        }
    }

    public void Init()
    {
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
        }

        if (_progressBar != null)
        {
            _progressBar.value = 0f;
        }
    }

    /// <summary>
    /// 씬을 로드합니다. 페이드 효과와 로딩바를 표시합니다.
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    public void LoadScene(string sceneName)
    {
        if (_isLoading)
        {
            Debug.LogWarning($"[SceneLoadManager] Scene is already loading. Ignoring request to load: {sceneName}");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoadManager] Scene name is null or empty.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;

        yield return StartCoroutine(Fade(1f));

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (_progressBar != null)
            {
                _progressBar.value = progress;
            }

            if (op.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.2f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }

        if (_progressBar != null)
        {
            _progressBar.value = 0f;
        }

        yield return StartCoroutine(Fade(0f));

        _isLoading = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (_fadeCanvasGroup == null) yield break;

        _fadeCanvasGroup.blocksRaycasts = true;
        float startAlpha = _fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < _fadeDuration)
        {
            time += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / _fadeDuration);
            yield return null;
        }

        _fadeCanvasGroup.alpha = targetAlpha;
        _fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0f);
    }
}
