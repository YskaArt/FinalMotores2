using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections;

public class GameIntroManager : MonoBehaviour
{
    [Header("Cámaras Cinemachine")]
    [SerializeField] private CinemachineCamera vcObjetivo;
    [SerializeField] private CinemachineCamera vcRecorrido;
    [SerializeField] private CinemachineCamera vcJugador;

    [Header("Configuración de la Cinemática")]
    [SerializeField] private float duracionObjetivo = 3f;
    [SerializeField] private float duracionRecorrido = 5f;
    [SerializeField] private Transform[] puntosRecorrido; // Waypoints para la cámara de recorrido (solo usaremos su Z)
    [SerializeField] private float velocidadRecorrido = 1f;

    [Header("Referencias del Jugador")]
    [SerializeField] private PlayerMovement playerMovementScript;
    [SerializeField] private PlayerInput playerInput;

    private const int PRIORITY_ACTIVE = 20;
    private const int PRIORITY_INACTIVE = 5;

    // Guardaremos la posición inicial X e Y de la cámara de recorrido
    private float recorridoStartX;
    private float recorridoStartY;

    void Start()
    {
        if (vcObjetivo == null || vcRecorrido == null || vcJugador == null ||
            playerMovementScript == null || playerInput == null)
        {
            Debug.LogError("¡Faltan referencias en GameIntroManager! Asigna todas las cámaras, el script de movimiento y el PlayerInput.");
            enabled = false;
            return;
        }

        vcObjetivo.Priority = PRIORITY_INACTIVE;
        vcRecorrido.Priority = PRIORITY_INACTIVE;
        vcJugador.Priority = PRIORITY_INACTIVE;

        // Captura la posición X e Y inicial de la cámara de recorrido.
        // Asume que esta es la posición Y que quieres mantener para el 2.5D.
        recorridoStartX = vcRecorrido.transform.position.x;
        recorridoStartY = vcRecorrido.transform.position.y;

        // Opcional: Si quieres que la cámara del recorrido siempre mire hacia adelante en Z sin rotar en X/Y.
        // Asegúrate de que la rotación inicial del GameObject 'VCRecorrido' esté configurada correctamente en el Inspector (ej: (0,0,0) o (X,0,0) si hay un tilt).
        // Si no quieres que rote, no añadas un Look At Target en el componente CinemachineCamera del VCRecorrido.

        SetPlayerControls(false); // Deshabilita controles al inicio
        StartCoroutine(PlayIntroCinematic());
    }

    private void SetPlayerControls(bool enable)
    {
        if (playerInput != null) playerInput.enabled = enable;
        if (playerMovementScript != null) playerMovementScript.enabled = enable;
        Debug.Log($"Controles del jugador: {(enable ? "HABILITADOS" : "DESHABILITADOS")}");
    }

    private IEnumerator PlayIntroCinematic()
    {
        // Paso 1: Mostrar el objetivo
        vcObjetivo.Priority = PRIORITY_ACTIVE;
        Debug.Log("Mostrando objetivo...");
        yield return new WaitForSeconds(duracionObjetivo);

        // Paso 2: Recorrido por el mapa (solo eje Z)
        vcObjetivo.Priority = PRIORITY_INACTIVE;
        vcRecorrido.Priority = PRIORITY_ACTIVE;
        Debug.Log("Iniciando recorrido por el mapa (solo eje Z)...");

        if (puntosRecorrido != null && puntosRecorrido.Length > 1)
        {
            for (int i = 0; i < puntosRecorrido.Length - 1; i++)
            {
                // Solo nos interesan las posiciones Z de los puntos de recorrido
                float startZ = puntosRecorrido[i].position.z;
                float endZ = puntosRecorrido[i + 1].position.z;

                // Calculamos la distancia efectiva en Z
                float distanciaSegmentoZ = Mathf.Abs(endZ - startZ);
                if (velocidadRecorrido <= 0) velocidadRecorrido = 0.1f; // Evitar división por cero
                float duracionSegmento = distanciaSegmentoZ / velocidadRecorrido;

                float t = 0f;

                Vector3 startPos = new Vector3(recorridoStartX, recorridoStartY, startZ);
                Vector3 endPos = new Vector3(recorridoStartX, recorridoStartY, endZ);

                while (t < 1.0f)
                {
                    t += Time.deltaTime / duracionSegmento;
                    // Interpola solo la posición Z, manteniendo X e Y fijas
                    float currentZ = Mathf.Lerp(startZ, endZ, t);
                    vcRecorrido.transform.position = new Vector3(recorridoStartX, recorridoStartY, currentZ);

                    // ¡Importante! No uses LookAt() si no quieres que la cámara rote libremente.
                    // Si necesitas que mire a un punto específico pero sin rotar en todos los ejes,
                    // tendrías que calcular la rotación del eje Y manualmente o usar un Aim.
                    // Para un 2.5D simple, la rotación del GameObject 'VCRecorrido' debería ser fija.

                    yield return null;
                }
            }
        }
        else
        {
            Debug.LogWarning("No hay suficientes puntos de recorrido para la cámara. Esperando duración predefinida.");
            yield return new WaitForSeconds(duracionRecorrido);
        }

        // Paso 3: Transición al Jugador y arranque del juego
        vcRecorrido.Priority = PRIORITY_INACTIVE;
        vcJugador.Priority = PRIORITY_ACTIVE;
        Debug.Log("Transición a la cámara del jugador. ¡Juego listo!");

        SetPlayerControls(true); // Habilita los controles del jugador
        this.enabled = false; // Deshabilita este script ya que su trabajo ha terminado
    }
}