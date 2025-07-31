using UnityEngine;

public class ControlPanel : MonoBehaviour
{
    public DoorController connectedDoor;
    public GameObject interactIcon;      // Ícono para indicar "Pulsa E para interactuar"
    public GameObject accessDeniedIcon;  // Ícono que indica "No tienes la llave"

    private bool isPlayerNearby = false;
    private PlayerInventory playerInventory;

    private void Update()
    {
        if (!isPlayerNearby || playerInventory == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (playerInventory.HasKey)
            {
                connectedDoor.OpenDoor();
                
                accessDeniedIcon.SetActive(false);
            }
            else
            {
                ShowAccessDenied();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            interactIcon.SetActive(true);
            
        }
    }

    

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            interactIcon.SetActive(false);
            accessDeniedIcon.SetActive(false);
        }
    }

    private void ShowAccessDenied()
    {
        interactIcon.SetActive(false);
        accessDeniedIcon.SetActive(true);
        Invoke(nameof(HideAccessDeniedIcon), 2f);
    }

    private void HideAccessDeniedIcon()
    {
        accessDeniedIcon.SetActive(false);
       
    }
}
