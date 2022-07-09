using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReload : MonoBehaviour
{
    public void Reload() {
        Debug.Log("Reloading Scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
