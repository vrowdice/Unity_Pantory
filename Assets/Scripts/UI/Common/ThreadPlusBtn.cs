using UnityEngine;

public class ThreadPlusBtn : MonoBehaviour
{
    public void OnClick()
    {
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[ThreadPlusBtn] SceneLoadManager.Instance is null. Cannot load scene.");
            return;
        }

        SceneLoadManager.Instance.LoadScene("Design");
    }
}
