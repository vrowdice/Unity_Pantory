using System;
using UnityEngine;

public class RunnerBase : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClip _bgmSource;

    public GameManager GameManager { get; private set; }
    public DataManager DataManager { get; private set; }
    public SoundManager SoundManager { get; private set; }

    public Camera MainCamera { get; private set; }
    public Transform CanvasTrans { get; private set; }
    public GameObject ProductionInfoImage { get; private set; }

    CanvasBase _canvasBase;

    public virtual void Init()
    {
        GameManager = GameManager.Instance;
        if (GameManager == null)
        {
            Debug.LogError("[RunnerBase] GameManager.Instance is null.");
            throw new InvalidOperationException("GameManager.Instance is null.");
        }

        DataManager = DataManager.Instance;
        if (DataManager == null)
        {
            Debug.LogError("[RunnerBase] DataManager.Instance is null.");
            throw new InvalidOperationException("DataManager.Instance is null.");
        }

        SoundManager = SoundManager.Instance;
        if (SoundManager == null)
        {
            Debug.LogError("[RunnerBase] SoundManager.Instance is null.");
            throw new InvalidOperationException("SoundManager.Instance is null.");
        }

        MainCamera = Camera.main;

        if (_canvasBase == null)
        {
            _canvasBase = FindAnyObjectByType<CanvasBase>();
        }

        if (_bgmSource != null)
        {
            SoundManager.PlayBGM(_bgmSource);
        }
    }
}
