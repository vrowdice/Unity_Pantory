using UnityEngine;

public class ScenMoveBtn : BtnBase
{
    [SerializeField] private string _scenName;

    protected override void HandleClick()
    {
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[ScenMoveBtn] SceneLoadManager.Instance is null. Cannot load scene.");
            return;
        }

        SceneLoadManager.Instance.LoadScene(_scenName);
    }
}
