using UnityEngine;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    public enum State
    {
        Patrol, Alert, Attack
    }

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    [SerializeField] private float waitTime = 2f;

    [Header("Vision")]
    [SerializeField] private Transform eye;
    [SerializeField] private float visionRangeFar = 10f;
    [SerializeField] private float visionRangeClose = 5f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private float visionHeight = 2f;
    [SerializeField] private LayerMask visionMask;
    [SerializeField] private bool showVision = true;

    [Header("Timings")]
    [SerializeField] private float timeToAttack = 3f;
    [SerializeField] private float alertDecayTime = 5f;

    [Header("UI State Indicators")]
    [SerializeField] private GameObject patrolIndicator;
    [SerializeField] private GameObject alertIndicator;
    [SerializeField] private GameObject attackIndicator;

    private State currentState;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;

    private float alertTimer = 0f;
    private float postAttackTimer = 0f;
    private float lostSightTimer = 0f;
    private bool isWaiting = false;
   
    private bool playerInStealth = false;

    public static int AlertActiveCount = 0;

    private bool returningFromAlert = false; // NUEVO: si está volviendo de Alert a Patrol (punto lejano)

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        GoToNextPatrolPoint();
        UpdateStateIndicators();
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Patrol: HandlePatrol(); break;
            case State.Alert: HandleAlert(); break;
            case State.Attack: HandleAttack(); break;
        }

        animator.SetBool("IsWalking", agent.velocity.magnitude > 0.1f);
    }

    private void HandlePatrol()
    {
        // Si está volviendo desde alerta al punto lejano, mantengo alerta visual
        if (returningFromAlert)
        {
            animator.SetBool("AlertWalk", true);
            // Cuando llegue, desactivo este modo y sigo patrullando normal
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                returningFromAlert = false;
                animator.SetBool("AlertWalk", false);
                waitTimer = 0f;
                isWaiting = false;
                GoToNextPatrolPoint();
            }
            return; // Mientras vuelve, no sigue la lógica normal de patrulla
        }
        else
        {
            animator.SetBool("AlertWalk", false);
        }

        if (CanSeePlayer(out float _))
        {
            ChangeState(State.Alert);
            return;
        }

        if (agent.remainingDistance < 0.5f && !isWaiting)
        {
            isWaiting = true;
            waitTimer = 0f;
            animator.SetTrigger("StopWalking");
        }

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                GoToNextPatrolPoint();
            }
        }
    }

    private void HandleAlert()
    {
        animator.SetBool("AlertWalk", true);
        animator.SetBool("IsChasing", false);
        if (CanSeePlayer(out float distance))
        {
            lostSightTimer = 0f;
            agent.SetDestination(player.position);
            alertTimer += Time.deltaTime;

            if (distance <= visionRangeClose || alertTimer >= timeToAttack)
            {
                ChangeState(State.Attack);
            }
        }
        else
        {
            lostSightTimer += Time.deltaTime;

            if (agent.remainingDistance < 0.5f)
                animator.SetBool("IsWalking", false);

            if (lostSightTimer >= alertDecayTime)
            {
                ChangeState(State.Patrol);
            }
        }
    }

    private void HandleAttack()
    {
        animator.SetBool("AlertWalk", false);
        animator.SetBool("IsChasing", true);

        if (!CanSeePlayer(out float _))
        {
            postAttackTimer += Time.deltaTime;
            if (postAttackTimer >= alertDecayTime)
            {
                ChangeState(State.Alert);
            }
            return;
        }

        postAttackTimer = 0f;
        agent.SetDestination(player.position);

      
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState) return;

        if (currentState == State.Attack) AlertActiveCount--;
        if (newState == State.Attack)
        {
            AlertActiveCount++;
            GameManager.Instance.IncreaseAlertLevel();
        }

        // Cuando pasa de Alert a Patrol, activar flag para ir al punto más lejano
        if (currentState == State.Alert && newState == State.Patrol)
        {
            returningFromAlert = true;

            // Busco el punto de patrulla más lejano
            int farthestIndex = 0;
            float maxDistance = 0f;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    farthestIndex = i;
                }
            }

            GoToNextPatrolPoint(farthestIndex);
        }
        else
        {
            returningFromAlert = false;
        }

        currentState = newState;
        alertTimer = 0f;
        postAttackTimer = 0f;
        lostSightTimer = 0f;
        isWaiting = false;
        waitTimer = 0f;

        UpdateStateIndicators();
    }

    private void GoToNextPatrolPoint(int forcedIndex = -1)
    {
        if (patrolPoints.Length == 0) return;

        if (forcedIndex >= 0 && forcedIndex < patrolPoints.Length)
        {
            currentPatrolIndex = forcedIndex;
        }

        agent.destination = patrolPoints[currentPatrolIndex].position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private bool CanSeePlayer(out float distance)
    {
        distance = 0f;
        if (player == null) return false;

        Vector3 dir = player.position - eye.position;
        distance = dir.magnitude;
        if (distance > visionRangeFar) return false;

        float angle = Vector3.Angle(eye.forward, dir);
        if (angle > visionAngle / 2f) return false;

        if (Physics.Raycast(eye.position, dir.normalized, out RaycastHit hit, visionRangeFar, visionMask))
        {
            if (hit.transform.CompareTag("Player"))
                return !(playerInStealth && currentState != State.Attack);
        }

        return false;
    }

    public void SetPlayerStealth(bool isStealth)
    {
        playerInStealth = isStealth;
    }

    private void UpdateStateIndicators()
    {
        if (patrolIndicator) patrolIndicator.SetActive(currentState == State.Patrol);
        if (alertIndicator) alertIndicator.SetActive(currentState == State.Alert);
        if (attackIndicator) attackIndicator.SetActive(currentState == State.Attack);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showVision || eye == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);

        DrawVisionArc(eye.position, visionRangeFar, visionAngle);
        DrawVisionArc(eye.position + Vector3.up * visionHeight, visionRangeFar, visionAngle);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(eye.position, visionRangeClose);
    }

    private void DrawVisionArc(Vector3 center, float radius, float angle)
    {
        int segments = 20;
        float step = angle / segments;
        Quaternion startRot = Quaternion.AngleAxis(-angle / 2, Vector3.up);
        Vector3 lastPoint = center + (startRot * eye.forward) * radius;

        for (int i = 1; i <= segments; i++)
        {
            Quaternion rot = Quaternion.AngleAxis(-angle / 2 + step * i, Vector3.up);
            Vector3 nextPoint = center + (rot * eye.forward) * radius;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
}
