using UnityEngine;

public class RunnerBase : MonoBehaviour
{
    public GameManager GameManager { get; private set; }
    public DataManager DataManager { get; private set; }

    public Camera MainCamera { get; private set; }
    public Transform CanvasTrans { get; private set; }
    public GameObject ProductionInfoImage { get; private set; }

    CanvasBase _canvasBase;

    public virtual void Init()
    {
        GameManager = GameManager.Instance;
        DataManager = DataManager.Instance;
        MainCamera = Camera.main;

        if(_canvasBase == null)
        {
            _canvasBase = FindAnyObjectByType<CanvasBase>();
        }
    }
}
