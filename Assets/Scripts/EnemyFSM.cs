using UnityEngine;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    public enum State
    {
        Patrol, Alert, Attack
    }
    public State currentState;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;

    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    private float alertTimer = 0f;
    private float timeToAttack = 3f;

    private bool canSeePlayer = false;
    private bool playerInStealth = false;

    public static int AlertActiveCount = 0;

    [Header("Vision Settings")]
    [SerializeField] private Transform eye; // punto de origen de la visión
    [SerializeField] private float visionRange = 10f; // distancia de visión
    [SerializeField] private float visionAngle = 90f; // ángulo en grados
    [SerializeField] private float visionHeight = 2f; // altura del cilindro de visión
    [SerializeField] private LayerMask visionMask;

    [SerializeField] private bool showVision = true; // si se debe mostrar la visión en modo juego

    private bool isWaiting = false;
    [SerializeField] private float waitTime = 2f; // Tiempo que el enemigo espera en Idle al llegar a un punto
    private float waitTimer = 0f;

    [Header("UI State Indicators")] // Nuevo encabezado para las imágenes
    [SerializeField] private GameObject patrolIndicator; // Imagen para el estado Patrulla
    [SerializeField] private GameObject alertIndicator;  // Imagen para el estado Alerta
    [SerializeField] private GameObject attackIndicator; // Imagen para el estado Ataque


    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        GoToNextPatrolPoint(); // Inicia el patrullaje
        UpdateStateIndicators(); // Llama esto al inicio para asegurar el estado visual correcto
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Alert:
                Alert();
                break;
            case State.Attack:
                Attack();
                break;
        }

        // Si el agente está en movimiento, activamos IsWalking, de lo contrario, desactivamos.
        // Esto se aplica globalmente para el movimiento.
        // AlertWalk se maneja en los estados Alert/Attack.
        if (currentState == State.Patrol) // Solo controlamos IsWalking aquí si estamos patrullando
        {
            animator.SetBool("IsWalking", agent.velocity.magnitude > 0.1f);
        }
        else
        { // En alerta o ataque, IsWalking siempre debería ser true si se mueve
            animator.SetBool("IsWalking", agent.velocity.magnitude > 0.1f); // O si el agente está en movimiento
        }
    }

    private void Patrol()
    {
        animator.SetBool("AlertWalk", false); // Aseguramos que no esté en animación de alerta

        if (agent.remainingDistance < 0.5f && !isWaiting)
        {
            // El enemigo ha llegado al punto de patrulla
            isWaiting = true;
            animator.SetBool("IsWalking", false); // Detiene la animación de caminar
            animator.SetTrigger("StopWalking"); // Activa el trigger para la animación de detenerse
            // No necesitamos un temporizador aquí para Idle directamente,
            // la lógica de espera ya se encarga de la duración total.
        }

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                // El tiempo de espera ha terminado, procede al siguiente punto
                isWaiting = false;
                waitTimer = 0f;
                GoToNextPatrolPoint();
                animator.SetBool("IsWalking", true); // Reanuda la animación de caminar
            }
            // Mientras espera, la animación de Idle se encargará por sí misma si "StopWalking" la transiciona
            // El SetBool("IsWalking", false) ya se encargó de la transición a Idle
            return;
        }

        if (CanSeePlayer())
        {
            ChangeState(State.Alert);
        }
    }

    private void Alert()
    {
        animator.SetBool("AlertWalk", true); // Activa la animación de alerta/caminar rápido
        animator.SetBool("IsWalking", true); // Asegura que también esté caminando

        if (!CanSeePlayer() && !playerInStealth) // Si pierde de vista al jugador y no está en sigilo
        {
            ChangeState(State.Patrol);
            return;
        }
        else if (!CanSeePlayer() && playerInStealth) // Si pierde de vista al jugador y está en sigilo
        {
            ChangeState(State.Patrol);
            return;
        }

        agent.SetDestination(player.position);

        alertTimer += Time.deltaTime;
        if (alertTimer >= timeToAttack)
        {
            ChangeState(State.Attack);
        }
    }

    private void Attack()
    {
        animator.SetBool("AlertWalk", false); // El ataque puede tener su propia animación, o solo IsWalking
        animator.SetBool("IsWalking", true); // Asegura que el enemigo se mueva si persigue

        if (!CanSeePlayer()) // Si pierde de vista al jugador, vuelve a Alerta (o Patrulla si la lógica lo permite)
        {
            ChangeState(State.Alert); // O ChangeState(State.Patrol); si quieres que sea menos persistente
            return;
        }

        agent.SetDestination(player.position);
        // Aquí irían otras acciones ofensivas o animaciones específicas (por ejemplo, un trigger de "AttackAnimation")
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        agent.destination = patrolPoints[currentPatrolIndex].position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState) return; // Evita cambiar al mismo estado repetidamente

        Debug.Log($"Enemigo {gameObject.name}: Cambio de estado de {currentState} a {newState}");

        if (currentState == State.Attack && newState != State.Attack)
        {
            AlertActiveCount--;
        }

        if (newState == State.Attack && currentState != State.Attack)
        {
            AlertActiveCount++;
            GameManager.Instance.IncreaseAlertLevel();
        }

        currentState = newState;
        alertTimer = 0f;
        isWaiting = false; // Reset waiting state on state change
        waitTimer = 0f;    // Reset wait timer

        UpdateStateIndicators();
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 direction = player.position - eye.position; // Usar eye.position como origen
        float distanceToPlayer = direction.magnitude;

        if (distanceToPlayer > visionRange)
        {
            canSeePlayer = false;
            return false;
        }

        float angle = Vector3.Angle(eye.forward, direction); // Usar eye.forward para el ángulo de visión
        if (angle > visionAngle / 2f)
        {
            canSeePlayer = false;
            return false;
        }

        // Raycast para comprobar obstáculos y sigilo
        if (Physics.Raycast(eye.position, direction.normalized, out RaycastHit hit, visionRange, visionMask))
        {
            if (hit.transform.CompareTag("Player"))
            {
                // Si el jugador está en sigilo y el enemigo NO está en estado de ataque, no lo "ve".
                canSeePlayer = !(playerInStealth && currentState != State.Attack);
            }
            else
            {
                canSeePlayer = false; // Hay un obstáculo entre el enemigo y el jugador
            }
        }
        else
        {
            canSeePlayer = false; // No hay nada detectado por el raycast
        }

        return canSeePlayer;
    }

    public void SetPlayerStealth(bool isStealth)
    {
        playerInStealth = isStealth;
    }

    private void UpdateStateIndicators()
    {
        if (patrolIndicator != null) patrolIndicator.SetActive(currentState == State.Patrol);
        if (alertIndicator != null) alertIndicator.SetActive(currentState == State.Alert);
        if (attackIndicator != null) attackIndicator.SetActive(currentState == State.Attack);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showVision || eye == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);

        // Se usa transform.forward aquí para dibujar el cono en la dirección del enemigo,
        // no la dirección del eye.forward si el eye es un sub-objeto con diferente rotación local.
        // Si eye siempre está alineado con el forward del enemigo, puedes usar eye.forward.
        Vector3 forwardDir = eye.forward;

        Quaternion leftRayRotation = Quaternion.AngleAxis(-visionAngle / 2, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(visionAngle / 2, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forwardDir;
        Vector3 rightRayDirection = rightRayRotation * forwardDir;

        Vector3 basePosition = eye.position;
        Vector3 topPosition = eye.position + Vector3.up * visionHeight;

        // Dibujo de cono base (como sector de círculo)
        Gizmos.DrawRay(basePosition, leftRayDirection * visionRange);
        Gizmos.DrawRay(basePosition, rightRayDirection * visionRange);

        // Dibujo de líneas de altura
        Gizmos.DrawLine(basePosition + leftRayDirection * visionRange, topPosition + leftRayDirection * visionRange);
        Gizmos.DrawLine(basePosition + rightRayDirection * visionRange, topPosition + rightRayDirection * visionRange);

        // Base y techo del cilindro (aproximado)
        DrawVisionArc(basePosition, visionRange, visionAngle);
        DrawVisionArc(topPosition, visionRange, visionAngle);
    }

    private void DrawVisionArc(Vector3 center, float radius, float angle)
    {
        int segments = 20;
        float step = angle / segments;
        // La rotación inicial para el arco debe considerar la dirección 'forward' del ojo
        Quaternion startRotation = Quaternion.AngleAxis(-angle / 2, Vector3.up);
        Vector3 lastPoint = center + (startRotation * eye.forward) * radius;

        for (int i = 1; i <= segments; i++)
        {
            Quaternion rotation = Quaternion.AngleAxis(-angle / 2 + step * i, Vector3.up);
            Vector3 nextPoint = center + (rotation * eye.forward) * radius;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
}