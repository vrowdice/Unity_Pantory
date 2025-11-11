using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenMoveBtn : MonoBehaviour
{
    [SerializeField] string _scenName;

    public void OnClick()
    {
        SceneManager.LoadScene(_scenName);
    }
}
