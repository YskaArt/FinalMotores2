using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialPanel; // Panel con la información del tutorial

    private bool hasActivated = false;

    private void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false); // Ocultamos el panel al inicio
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasActivated) return;

        if (other.CompareTag("Player"))
        {
            hasActivated = true;
            tutorialPanel.SetActive(true);
            Time.timeScale = 0f; // Pausar el juego
            gameObject.SetActive(false); // Desactivar el trigger para no reutilizarlo
        }
    }

    // Este método se debe conectar al botón de "Cerrar"
    public void CloseTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        Time.timeScale = 1f; // Reanudar el juego
    }
}
