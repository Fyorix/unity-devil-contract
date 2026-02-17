using UnityEngine;
using UnityEngine.SceneManagement;

public class playMenu : MonoBehaviour
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.None; // Débloque la souris
        Cursor.visible = true;                  // Rend la souris visible
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playGame()
    {
        SceneManager.LoadSceneAsync(2);
    }

    public void quitGame()
    {
        Application.Quit();
    }
}
