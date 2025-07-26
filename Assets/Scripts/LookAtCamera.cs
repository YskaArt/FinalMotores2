using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform mainCamera;

    void Start()
    {
       
        mainCamera = Camera.main.transform;

       
        if (mainCamera != null)
        {
           
            transform.forward = mainCamera.forward;
            transform.Rotate(0, -90, 0, Space.Self);
        }
    }

    void LateUpdate()
    {
      
        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
            if (mainCamera == null) return; 
        }

        
        transform.rotation = Quaternion.LookRotation(mainCamera.forward);
        transform.Rotate(0, -90, 0, Space.Self);
    }
}