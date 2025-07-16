using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Alerta")]
    public int alertLevel = 0;
    public int maxAlertLevel = 5;
    private float alertTimer;
    public float timeToIncrease = 2f; // cada 2 segundos

    [Header("UI")]
    public Text alertText;
    public GameObject missionCompleteUI;
    public GameObject gameOverUI;

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (isGameOver) return;

        if (EnemyFSM.AlertActiveCount > 0)
        {
            alertTimer += Time.deltaTime;
            if (alertTimer >= timeToIncrease)
            {
                alertTimer = 0;
                IncreaseAlertLevel();
            }
        }

        alertText.text = "ALERTA: " + alertLevel + "/" + maxAlertLevel;
    }

    public void IncreaseAlertLevel()
    {
        if (alertLevel < maxAlertLevel)
        {
            alertLevel++;
            if (alertLevel >= maxAlertLevel)
                TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TriggerMissionComplete()
    {
        isGameOver = true;
        missionCompleteUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
