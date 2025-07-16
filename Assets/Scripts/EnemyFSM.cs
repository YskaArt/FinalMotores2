using UnityEngine;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    [Header("Referencias")]
    public Transform[] patrolPoints;
    public Transform player;
    public LayerMask playerLayer;

    [Header("Configuración")]
    public float viewDistance = 10f;
    public float viewAngle = 60f;
    public float losePlayerTime = 3f;

    [Header("Estado del jugador")]
    public bool playerInStealth = false;

    [HideInInspector] public Vector3 lastKnownPlayerPosition;

    private NavMeshAgent agent;
    private int currentPatrolIndex;
    private IState currentState;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SwitchState(new PatrolState());
    }

    private void Update()
    {
        currentState?.UpdateState(this);
    }

    public void SwitchState(IState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public bool CanSeePlayer()
    {
        if (playerInStealth) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > viewAngle / 2f) return false;

        if (Physics.Raycast(transform.position, dirToPlayer, out RaycastHit hit, viewDistance, playerLayer))
        {
            return hit.transform.CompareTag("Player");
        }

        return false;
    }

    public interface IState
    {
        void EnterState(EnemyFSM enemy);
        void UpdateState(EnemyFSM enemy);
        void ExitState(EnemyFSM enemy);
    }

    // ------------------------
    // 🟦 Estado: Patrullaje
    // ------------------------
    private class PatrolState : IState
    {
        public void EnterState(EnemyFSM enemy)
        {
            Debug.Log("Estado: PATRULLA");
            enemy.agent.SetDestination(enemy.patrolPoints[enemy.currentPatrolIndex].position);

            // Mejora posible: reproducir animación de caminar relajado o idle
            // enemy.animator.SetBool("IsWalking", true);
        }

        public void UpdateState(EnemyFSM enemy)
        {
            if (enemy.CanSeePlayer())
            {
                enemy.SwitchState(new AttackState());
                return;
            }

            if (!enemy.agent.pathPending && enemy.agent.remainingDistance < 0.5f)
            {
                enemy.currentPatrolIndex = (enemy.currentPatrolIndex + 1) % enemy.patrolPoints.Length;
                enemy.agent.SetDestination(enemy.patrolPoints[enemy.currentPatrolIndex].position);
            }
        }

        public void ExitState(EnemyFSM enemy)
        {
            // enemy.animator.SetBool("IsWalking", false); // Ejemplo de transición de animación
        }
    }

    // ------------------------
    // 🟨 Estado: Alerta
    // ------------------------
    private class AlertState : IState
    {
        private float timer;
        private const float alertDuration = 5f;

        public void EnterState(EnemyFSM enemy)
        {
            Debug.Log("Estado: ALERTA");
            timer = 0f;
            enemy.agent.SetDestination(enemy.lastKnownPlayerPosition);

            // Posibles mejoras:
            // - Activar animación de escaneo/sospecha.
            // - Sonido de “alerta detectada”.
            // - Encender luz roja o icono de signo de pregunta (UI).
        }

        public void UpdateState(EnemyFSM enemy)
        {
            timer += Time.deltaTime;

            if (enemy.CanSeePlayer())
            {
                enemy.SwitchState(new AttackState());
                return;
            }

            // Espera un tiempo en el último punto donde lo vio antes de regresar a patrulla
            if (timer >= alertDuration)
            {
                enemy.SwitchState(new PatrolState());
            }
        }

        public void ExitState(EnemyFSM enemy)
        {
            // enemy.animator.SetTrigger("Relax"); // Transición visual del estado de alerta a patrulla
        }
    }

    // ------------------------
    // 🔴 Estado: Ataque
    // ------------------------
    private class AttackState : IState
    {
        private float lostTimer;

        public void EnterState(EnemyFSM enemy)
        {
            Debug.Log("Estado: ATAQUE");
            lostTimer = 0f;

            // Posibles mejoras:
            // - Activar animación de correr o disparar
            // - Activar alarma (Audio Source / luz)
            // - Cambiar música del juego (tensión)
        }

        public void UpdateState(EnemyFSM enemy)
        {
            if (enemy.CanSeePlayer())
            {
                enemy.lastKnownPlayerPosition = enemy.player.position; // Guarda la última posición conocida
                enemy.agent.SetDestination(enemy.player.position);
                lostTimer = 0f;
            }
            else
            {
                lostTimer += Time.deltaTime;
                if (lostTimer >= enemy.losePlayerTime)
                {
                    enemy.SwitchState(new AlertState());
                }
            }
        }

        public void ExitState(EnemyFSM enemy)
        {
            // enemy.animator.SetBool("IsRunning", false);
        }
    }

    // Método público para que el Player pueda cambiar el estado de sigilo
    public void SetPlayerStealth(bool value)
    {
        playerInStealth = value;
    }
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, viewDistance); // Dibuja el rango de visión

        Vector3 forward = transform.forward;
        Vector3 leftLimit = Quaternion.Euler(0, -viewAngle / 2f, 0) * forward;
        Vector3 rightLimit = Quaternion.Euler(0, viewAngle / 2f, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + leftLimit * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightLimit * viewDistance);
    }
}
