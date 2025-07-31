using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
 

    [Header("Nombre de escenas")]
    public string tutorialScene = "Tutorial";
    public string level1Scene = "Level1";
    public string mainMenuScene = "MainMenu";
    public string mainVictory = "Victory";

 

    // Llama este desde el botón "Start Game"
    public void StartTutorial()
    {
        SceneManager.LoadScene(tutorialScene);
    }

    // Llama este desde el botón "Salir"
    public void QuitGame()
    {
        Debug.Log("Cerrando Juego...");
        Application.Quit();

    }

    // Llama este desde el botón "Reiniciar Nivel 1"
    public void RestartLevel1()
    {
        SceneManager.LoadScene(level1Scene);
    }

    // Llama este desde el botón "Volver al Menú Principal"
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
    public void Victory()
    {
        SceneManager.LoadScene(mainVictory);
    }

  
   
}
