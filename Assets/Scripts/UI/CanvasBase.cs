using UnityEngine;

public class CanvasBase : TutorialBase
{
    public GameManager GameManager { get; private set; }
    public DataManager DataManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public SoundManager SoundManager { get; private set; }
    public SceneLoadManager SceneLoadManager { get; private set; }
    public VisualManager VisualManager { get; private set; }
    public Transform CanvasTrans { get; private set; }

    public virtual void Init()
    {
        GameManager = GameManager.Instance;
        DataManager = DataManager.Instance;
        UIManager = UIManager.Instance;
        SoundManager = SoundManager.Instance;
        SceneLoadManager = SceneLoadManager.Instance;
        VisualManager = VisualManager.Instance;

        CanvasTrans = transform;
    }

    public virtual void UpdateAllMainText()
    {

    }
}
