using UnityEngine;
using Unity.Cinemachine;

public class ChangeRotationComposerY : MonoBehaviour
{
    public CinemachineCamera cineCamera;
    public float targetY = -0.2f;
    public float smoothTime = 0.6f;

    private CinemachineRotationComposer composer;
    private float velocity = 0f;
    private bool isTransitioning = false;
    private float currentY;

    private void Start()
    {
        if (cineCamera == null)
        {
            Debug.LogError("CinemachineCamera no asignada.");
            enabled = false;
            return;
        }

        composer = cineCamera.GetComponent<CinemachineRotationComposer>();
        if (composer == null)
        {
            Debug.LogError("CinemachineRotationComposer no encontrado.");
            enabled = false;
            return;
        }

        // Obtener Y actual al iniciar
        currentY = composer.Composition.ScreenPosition.y;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Solo iniciar si hay diferencia significativa
            float currentComposerY = composer.Composition.ScreenPosition.y;
            if (Mathf.Abs(currentComposerY - targetY) > 0.01f)
            {
                currentY = currentComposerY; // Actualizar desde valor actual real
                isTransitioning = true;
            }
        }
    }

    private void Update()
    {
        if (!isTransitioning) return;

        // Interpolar suavemente Y hacia targetY
        currentY = Mathf.SmoothDamp(currentY, targetY, ref velocity, smoothTime);

        var comp = composer.Composition;
        comp.ScreenPosition = new Vector2(comp.ScreenPosition.x, currentY);
        composer.Composition = comp;

        // Detener cuando ya está cerca
        if (Mathf.Abs(currentY - targetY) < 0.001f)
        {
            comp.ScreenPosition = new Vector2(comp.ScreenPosition.x, targetY);
            composer.Composition = comp;
            isTransitioning = false;
        }
    }
}
