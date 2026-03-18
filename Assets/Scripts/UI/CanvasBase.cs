using UnityEngine;

public class CanvasBase : TutorialBase
{
    public GameManager GameManager { get; private set; }
    public DataManager DataManager { get; private set; }
    public Transform CanvasTrans { get; private set; }
    public GameObject ProductionInfoImage { get; private set; }

    public virtual void Init()
    {
        GameManager = GameManager.Instance;
        DataManager = DataManager.Instance;

        CanvasTrans = transform;
        ProductionInfoImage = UIManager.Instance.ProductionInfoImagePrefab;
    }

    public virtual void UpdateAllMainText()
    {

    }
}
