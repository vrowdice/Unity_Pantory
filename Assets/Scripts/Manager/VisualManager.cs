using UnityEngine;

public class VisualManager : MonoBehaviour
{
    public static VisualManager Instance { get; private set; }

    public Color ValidColor => _validColor;
    public Color InvalidColor => _invalidColor;

    [SerializeField]private Color _validColor = new Color(0, 1, 0, 0.2f);
    [SerializeField]private Color _invalidColor = new Color(1, 0, 0, 0.2f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
    }


}
