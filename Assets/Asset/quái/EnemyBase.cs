using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : MonoBehaviour
{
    [Header("Tam nhin & Tan cong")]
    public float visionRange = 10f;
    [Range(0f, 360f)] public float visionAngle = 120f;
    public float attackRange = 2f;
    public Transform player;
    public LayerMask obstacleMask;
    public float eyeHeight = 1.5f;

    [Header("Cau hinh sat thuong")]
    public int attackDamage = 20;
    public float attackCooldown = 2f;
    public float damageDistance = 2.5f;

    [Header("Thoi gian tan cong (du phong)")]
    [Tooltip("Bat neu animation Attack co Animation Event DealDamage / EndAttack")]
    public bool useAnimationEvents = true;
    [Tooltip("Thoi diem gay sat thuong neu KHONG dung Animation Event")]
    public float attackHitDelay = 0.5f;
    [Tooltip("Thoi gian toi da 1 don danh. Het thoi gian se tu ket thuc de quai khong bi ket")]
    public float attackMaxDuration = 1.5f;

    [Header("Di tuan")]
    public Transform[] waypoints;
    public float patrolRadius = 15f;
    public float patrolSpeed = 2f;
    public float waitAtPoint = 2f;
    public float arriveThreshold = 0.5f;

    [Header("Duoi theo")]
    public float chaseSpeed = 4.5f;
    public float loseSightTime = 3f;
    public float destinationUpdateRate = 0.15f;
    public float turnSpeed = 5f;

    protected NavMeshAgent agent;
    protected Animator animator;

    public enum State { Idle, Patrol, Chase, Atk }
    public State currentState = State.Idle;

    protected bool isDead = false;
    protected bool isAtking = false;
    protected bool isBlocking = false;

    private Vector3 spawnPosition;
    private int waypointIndex = -1;
    private bool hasPatrolTarget;
    private bool isWaiting;
    private float waitTimer;
    private float nextDestinationTime;
    private float lastSeenTime = -999f;
    private float nextAttackTime;
    private float attackStartTime;

    private bool hasDealtDamage;

    private Vector3 lastKnownPlayerPos;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    public bool IsDead => isDead;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Tim Animator tren object hien tai hoac object con
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Neu Animator nam o object con, Animation Event se goi ham tren object con
        // -> tu dong gan relay de chuyen tiep ve EnemyBase
        if (animator != null && animator.gameObject != gameObject &&
            animator.GetComponent<EnemyAnimationEventRelay>() == null)
        {
            animator.gameObject.AddComponent<EnemyAnimationEventRelay>();
        }
    }

    protected virtual void Start()
    {
        spawnPosition = transform.position;

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
                player = pObj.transform;
        }

        // Ep ap dung cau hinh Patrol du currentState trong Inspector la gi
        currentState = State.Idle;
        ChangeState(State.Patrol);
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        CheckVisionAndStates();
        HandleAnimations();
    }

    // =========================
    // KIEM TRA TAM NHIN
    // =========================

    protected virtual void CheckVisionAndStates()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (isAtking)
        {
            UpdateAttack();
            return;
        }

        bool canSee = CanSeePlayer();

        if (canSee)
        {
            lastSeenTime = Time.time;
            lastKnownPlayerPos = player.position;
        }

        bool remembers = Time.time - lastSeenTime <= loseSightTime;

        if (canSee && FlatDistanceToPlayer() <= attackRange)
        {
            ChangeState(State.Atk);

            StopAgent();
            LookAtPlayer();

            if (Time.time >= nextAttackTime)
                StartAttack();
        }
        else if (canSee || remembers)
        {
            ChangeState(State.Chase);

            if (isBlocking)
            {
                StopAgent();
                return;
            }

            agent.isStopped = false;

            if (Time.time >= nextDestinationTime)
            {
                nextDestinationTime = Time.time + destinationUpdateRate;
                agent.SetDestination(canSee ? player.position : lastKnownPlayerPos);
            }
        }
        else
        {
            PatrolLogic();
        }
    }

    // =========================
    // TAN CONG
    // =========================

    protected virtual void StartAttack()
    {
        if (isAtking || isDead)
            return;

        isAtking = true;
        hasDealtDamage = false;
        attackStartTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;

        StopAgent();
        LookAtPlayer();

        if (animator != null)
        {
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(AttackHash);
        }
    }

    // Chay moi frame trong luc dang danh
    protected virtual void UpdateAttack()
    {
        StopAgent();
        LookAtPlayer();

        float elapsed = Time.time - attackStartTime;

        // Khong dung Animation Event (hoac khong co Animator): tu gay damage theo thoi gian
        if ((!useAnimationEvents || animator == null) &&
            !hasDealtDamage && elapsed >= attackHitDelay)
        {
            DealDamage();
        }

        // Du phong: event EndAttack khong duoc goi (thieu event, animation bi ngat...)
        if (elapsed >= attackMaxDuration)
            FinishAttack();
    }

    // GOI BANG ANIMATION EVENT
    public virtual void DealDamage()
    {
        if (!isAtking || isDead || hasDealtDamage)
            return;

        hasDealtDamage = true;

        if (player == null || FlatDistanceToPlayer() > damageDistance)
            return;

        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();

        if (health != null)
            health.TakeDamage(attackDamage);
        else
            Debug.LogWarning("Player chua co PlayerHealth!", player);
    }

    // GOI BANG ANIMATION EVENT KHI ANIMATION KET THUC
    public virtual void EndAttack()
    {
        if (!isAtking)
            return;

        FinishAttack();
    }

    protected void FinishAttack()
    {
        isAtking = false;
        hasDealtDamage = false;
        nextDestinationTime = 0f; // cap nhat duong di ngay khi duoi tiep
    }

    // =========================
    // CHET
    // =========================

    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isAtking = false;

        StopAgent();
        if (agent != null)
            agent.enabled = false;

        if (animator != null)
            animator.SetFloat(SpeedHash, 0f);
    }

    // =========================
    // DI TUAN
    // =========================

    protected virtual void PatrolLogic()
    {
        if (currentState != State.Patrol && currentState != State.Idle)
            ChangeState(State.Patrol);

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                hasPatrolTarget = false;
                ChangeState(State.Patrol);
            }

            return;
        }

        if (!hasPatrolTarget)
        {
            if (!PickNextPatrolPoint())
                return;
        }

        if (agent.pathPending)
            return;

        // Den noi, hoac duong di bi mat/khong hop le -> dung lai cho
        bool arrived = agent.remainingDistance <= arriveThreshold;
        bool pathLost = !agent.hasPath ||
                        agent.pathStatus == NavMeshPathStatus.PathInvalid;

        if (arrived || pathLost)
        {
            isWaiting = true;
            waitTimer = waitAtPoint;

            StopAgent();
            ChangeState(State.Idle);
            IdleLogic();
        }
    }

    protected bool PickNextPatrolPoint()
    {
        Vector3 target;

        if (waypoints != null && waypoints.Length > 0)
        {
            // Bo qua waypoint bi null
            Transform wp = null;
            for (int i = 0; i < waypoints.Length && wp == null; i++)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                wp = waypoints[waypointIndex];
            }

            if (wp == null)
                return false;

            target = wp.position;
        }
        else if (!TryGetRandomPoint(out target))
        {
            return false;
        }

        agent.isStopped = false;
        hasPatrolTarget = agent.SetDestination(target);

        return hasPatrolTarget;
    }

    protected bool TryGetRandomPoint(out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            // Lay diem tren mat phang ngang (insideUnitSphere hay roi xa mat dat)
            Vector2 circle = Random.insideUnitCircle * patrolRadius;
            Vector3 random = spawnPosition + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = spawnPosition;
        return false;
    }

    // =========================
    // DOI TRANG THAI
    // =========================

    protected virtual void ChangeState(State newState)
    {
        if (currentState == newState)
            return;

        State old = currentState;
        currentState = newState;

        if (agent == null || !agent.enabled)
            return;

        switch (newState)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0f;

                if (old == State.Chase || old == State.Atk)
                {
                    hasPatrolTarget = false;
                    isWaiting = false;
                }
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.2f);
                agent.isStopped = false;
                isWaiting = false;
                hasPatrolTarget = false;
                nextDestinationTime = 0f;
                break;

            case State.Atk:
                if (agent.isOnNavMesh)
                    agent.isStopped = true;
                isWaiting = false;
                hasPatrolTarget = false;
                break;
        }
    }

    // =========================
    // KIEM TRA TAM NHIN PLAYER
    // =========================

    protected virtual bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;

        if (dist > visionRange)
            return false;

        if (dist > attackRange && visionAngle < 360f)
        {
            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > visionAngle * 0.5f)
                return false;
        }

        if (obstacleMask.value != 0)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.position + Vector3.up;

            if (Physics.Linecast(origin, target, obstacleMask, QueryTriggerInteraction.Ignore))
                return false;
        }

        return true;
    }

    protected float FlatDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    // =========================
    // DUNG VA XOAY QUAI
    // =========================

    protected void StopAgent()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            if (agent.hasPath)
                agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    public virtual void LookAtPlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
    }

    public virtual void IdleLogic()
    {
    }

    // =========================
    // ANIMATION
    // =========================

    protected virtual void HandleAnimations()
    {
        if (animator == null || agent == null)
            return;

        float speed = (isAtking || !agent.enabled) ? 0f : agent.velocity.magnitude;
        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
    }

    // =========================
    // GIZMOS
    // =========================

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 left = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, left * visionRange);
        Gizmos.DrawRay(transform.position, right * visionRange);

        if (waypoints == null || waypoints.Length == 0)
        {
            Gizmos.color = Color.green;
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            Gizmos.DrawWireSphere(center, patrolRadius);
        }
    }
}

// Gan tu dong len object con chua Animator.
// Animation Event goi DealDamage / EndAttack tren object con -> chuyen tiep ve EnemyBase o cha.
public class EnemyAnimationEventRelay : MonoBehaviour
{
    private EnemyBase enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<EnemyBase>();
    }

    public void DealDamage()
    {
        if (enemy != null) enemy.DealDamage();
    }

    public void EndAttack()
    {
        if (enemy != null) enemy.EndAttack();
    }
}