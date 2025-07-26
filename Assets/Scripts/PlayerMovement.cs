using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Asegúrate de tener esta línea

public class PlayerMovement : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isJumping;

    private bool isStealth = false;
    private bool isInStealthZone = false;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float depthSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Physics")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("PostProcessing")]
    [SerializeField] private Volume globalVolume;
    private Vignette vignette;

    private Collider playerCollider;
    private Animator animator;

    [Header("UI Stealth Indicators")] // Nuevo encabezado para los indicadores del jugador
    [SerializeField] private GameObject inStealthZoneIndicator; // Imagen para cuando el jugador está en una zona de sigilo
    [SerializeField] private GameObject stealthActiveIndicator;   // Imagen para cuando el sigilo está activado


    private void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider>();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;

        inputActions.Player.Jump.performed += _ => TryJump();
        inputActions.Player.Stealth.performed += _ => ToggleStealth();

        if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
        {
            vignette.intensity.Override(0.1f);
        }

        // Asegurarse de que los indicadores estén desactivados al inicio
        UpdateStealthIndicators();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.z = moveInput.x * moveSpeed;
        velocity.x = -moveInput.y * depthSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Keyboard.current.spaceKey.isPressed)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        Vector3 moveDirection = new Vector3(-moveInput.y, 0, moveInput.x);
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            float targetYRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            targetYRotation -= 180f;
            Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }

        // Actualizar animación de caminar
        animator.SetBool("IsWalking", moveInput.sqrMagnitude > 0.01f);
    }

    private void TryJump()
    {
        if (IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            Debug.Log("Salto ejecutado");

            // Activar animación de salto
            animator.SetTrigger("Jump");
        }
    }

    private bool IsGrounded()
    {
        return Physics.CheckSphere(groundCheck.position, 0.6f, groundLayer);
    }

    private void ToggleStealth()
    {
        if (isInStealthZone)
        {
            isStealth = !isStealth;
            Debug.Log("Sigilo activo: " + isStealth);

            if (vignette != null)
            {
                float intensity = isStealth ? 0.7f : 0.1f;
                vignette.intensity.Override(intensity);
            }

            // Notificar a todos los enemigos sobre el estado de sigilo del jugador
            foreach (EnemyFSM enemy in FindObjectsByType<EnemyFSM>(FindObjectsSortMode.None))
            {
                enemy.SetPlayerStealth(isStealth);
            }
            UpdateStealthIndicators(); // Actualiza los indicadores visuales
        }
        else
        {
            // Si el jugador intenta activar sigilo fuera de una zona, asegurarse de que no esté activo
            isStealth = false;
            if (vignette != null)
            {
                vignette.intensity.Override(0.1f);
            }
            foreach (EnemyFSM enemy in FindObjectsByType<EnemyFSM>(FindObjectsSortMode.None))
            {
                enemy.SetPlayerStealth(false);
            }
            UpdateStealthIndicators(); // Actualiza los indicadores visuales
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StealthZone"))
        {
            isInStealthZone = true;
            Debug.Log("Entró en zona de sigilo.");
            UpdateStealthIndicators(); // Actualiza los indicadores visuales
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StealthZone"))
        {
            isInStealthZone = false;
            isStealth = false; // El sigilo se desactiva al salir de la zona

            Debug.Log("Salió de zona de sigilo. Sigilo desactivado.");

            if (vignette != null)
            {
                vignette.intensity.Override(0.1f);
            }

            foreach (EnemyFSM enemy in FindObjectsByType<EnemyFSM>(FindObjectsSortMode.None))
            {
                enemy.SetPlayerStealth(false);
            }
            UpdateStealthIndicators(); // Actualiza los indicadores visuales
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Objective") && Keyboard.current.eKey.wasPressedThisFrame)
        {
            GameManager.Instance.TriggerMissionComplete();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    // Nuevo método para actualizar las imágenes de estado de sigilo del jugador
    private void UpdateStealthIndicators()
    {
        // El indicador de "en zona de sigilo" solo se activa si estamos en la zona Y el sigilo no está activo (para que no se superpongan)
        // O podrías querer que esté siempre activo si estás en la zona, y el otro encima si activas.
        // Aquí lo haré para que solo uno esté activo a la vez para mayor claridad.
        if (inStealthZoneIndicator != null)
        {
            inStealthZoneIndicator.SetActive(isInStealthZone && !isStealth);
        }

        if (stealthActiveIndicator != null)
        {
            stealthActiveIndicator.SetActive(isStealth);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, 0.6f);
        }
    }
}