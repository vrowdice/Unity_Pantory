using UnityEngine;

public class ThreadPlusBtn : BtnBase
{
    protected override void HandleClick()
    {
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[ThreadPlusBtn] SceneLoadManager.Instance is null. Cannot load scene.");
            return;
        }

        SceneLoadManager.Instance.LoadScene("Design");
    }
}
