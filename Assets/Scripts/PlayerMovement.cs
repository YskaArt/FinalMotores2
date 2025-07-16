using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    [SerializeField] private Volume globalVolume;
    private Vignette vignette;
    private void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody>();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;

        inputActions.Player.Jump.performed += _ => TryJump();
        inputActions.Player.Stealth.performed += _ => ToggleStealth();

        // Buscar el efecto Vignette dentro del Volume
        if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
        {
            vignette.intensity.Override(0.1f);
        }
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.z = moveInput.x * moveSpeed;
        velocity.x = -moveInput.y * depthSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

      
    }

    private void TryJump()
    {
        if (IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            Debug.Log("Salto ejecutado");
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
            Debug.Log("Sigilo: " + isStealth);
            if (vignette != null)
            {
                float intensity = isStealth ? 0.7f : 0.1f;
                vignette.intensity.Override(intensity);
            }
            foreach (EnemyFSM enemy in FindObjectsByType<EnemyFSM>(FindObjectsSortMode.None))
            {
                enemy.SetPlayerStealth(true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StealthZone"))
        {
            isInStealthZone = true;
           
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StealthZone"))
        {
            isInStealthZone = false;
            isStealth = false;

            if (vignette != null)
            {
                vignette.intensity.Override(0.1f);
            }
            foreach (EnemyFSM enemy in FindObjectsByType<EnemyFSM>(FindObjectsSortMode.None))
            {
                enemy.SetPlayerStealth(false);
            }
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
