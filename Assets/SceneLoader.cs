using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Or load by name if you prefer:
    // public void LoadScene(string sceneName)
    // {
    //     SceneManager.LoadScene(sceneName);
    // }
}