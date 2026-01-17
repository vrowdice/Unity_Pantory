using UnityEngine;

public class ScenMoveBtn : MonoBehaviour
{
    [SerializeField] string _scenName;

    public void OnClick()
    {
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[ScenMoveBtn] SceneLoadManager.Instance is null. Cannot load scene.");
            return;
        }

        SceneLoadManager.Instance.LoadScene(_scenName);
    }
}
