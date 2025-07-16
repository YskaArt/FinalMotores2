using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMission : MonoBehaviour
{
    private bool canCompleteMission = false;
    [SerializeField] private GameObject missionUI;

    private void Start()
    {
        missionUI = GameObject.Find("MissionCompleteUI");
        if (missionUI != null)
            missionUI.SetActive(false);
    }

    private void Update()
    {
        if (canCompleteMission && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("¡Misión completada!");
            CompleteMission();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MissionGoal"))
        {
            canCompleteMission = true;
            Debug.Log("Zona de objetivo alcanzada. Presiona E para completar.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MissionGoal"))
        {
            canCompleteMission = false;
            Debug.Log("Saliste del objetivo.");
        }
    }

    private void CompleteMission()
    {
        Time.timeScale = 0f; // Pausa el juego
        if (missionUI != null)
            missionUI.SetActive(true);
    }

}
