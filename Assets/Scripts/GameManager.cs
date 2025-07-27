using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Alerta")]
    public float alertLevel = 0f;
    public int maxAlertLevel = 5;
    

    [Header("UI")]
    public Text alertText;
    public GameObject missionCompleteUI;
    public GameObject gameOverUI;

    private bool isGameOver = false;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (Instance == null)
        {
            Instance = this;
           
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
        if (missionCompleteUI != null) missionCompleteUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        
        UpdateAlertUI(); // Actualiza el UI al inicio
    }

    private void Update()
    {
        if (isGameOver) return;

        
        UpdateAlertUI();
    }

    // Método modificado para recibir la cantidad a aumentar (float)
    public void IncreaseAlertLevel(float amountToIncrease = 1f) // Valor por defecto de 1 si no se pasa nada
    {
        if (isGameOver) return;

        alertLevel += amountToIncrease;

        // Limita el nivel de alerta al máximo
        if (alertLevel >= maxAlertLevel)
        {
            alertLevel = maxAlertLevel; // Asegura que no se exceda el máximo visualmente
            TriggerGameOver();
        }
        // El UI se actualiza en UpdateAlertUI, que es llamado en Update, o puedes llamarlo aquí explícitamente si quieres una actualización inmediata
        // UpdateAlertUI(); 
    }

    private void UpdateAlertUI()
    {
        // Usar Mathf.CeilToInt para redondear hacia arriba y mostrar un número entero en el UI
        // O puedes castear a int si prefieres truncar.
        alertText.text = "ALERTA: " + Mathf.CeilToInt(alertLevel) + "/" + maxAlertLevel;
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return; // Evita que se dispare múltiples veces

        Debug.Log("Game Over! Nivel de Alerta máximo alcanzado.");
        isGameOver = true;
        if (gameOverUI != null) gameOverUI.SetActive(true);
        Time.timeScale = 0f; // Pausa el juego
        // Desactiva cualquier input de jugador si es necesario
    }

    public void TriggerMissionComplete()
    {
        if (isGameOver) return; // Evita que se dispare si ya es Game Over

        Debug.Log("¡Misión Completada!");
        isGameOver = true;
        if (missionCompleteUI != null) missionCompleteUI.SetActive(true);
        Time.timeScale = 0f; // Pausa el juego
        // Desactiva cualquier input de jugador si es necesario
    }

    public void ResetGame()
    {
        Time.timeScale = 1f; // Reanuda el juego antes de cargar la escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void IncreaseAlertOverTime(float deltaTime)
    {
        if (isGameOver) return;

        // Escalado personalizado: aumenta 0.25 de alerta por segundo
        float alertPerSecond = 0.25f;
        float increaseAmount = deltaTime * alertPerSecond;

        IncreaseAlertLevel(increaseAmount);
    }
}