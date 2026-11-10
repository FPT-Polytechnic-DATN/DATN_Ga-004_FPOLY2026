using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : MonoBehaviour
{
    [Header("Tầm nhìn & Tấn công")]
    public float visionRange = 10f;                 // Khoảng cách nhìn thấy Player
    [Range(0f, 360f)] public float visionAngle = 120f; // Góc nhìn (360 = nhìn mọi hướng)
    public float attackRange = 2f;                  // Tầm với để tấn công
    public Transform player;                        // Tham chiếu đến Player
    public LayerMask obstacleMask;                  // Layer vật cản che tầm nhìn (tường...). Để Nothing nếu không cần
    public float eyeHeight = 1.5f;                  // Độ cao "mắt" của quái để bắn tia kiểm tra

    [Header("Đi tuần")]
    public Transform[] waypoints;                   // Có điểm tuần -> đi lần lượt. Để trống -> đi ngẫu nhiên quanh vị trí spawn
    public float patrolRadius = 15f;                // Bán kính đi ngẫu nhiên (khi không có waypoints)
    public float patrolSpeed = 2f;                  // Tốc độ khi đi tuần
    public float waitAtPoint = 2f;                  // Thời gian đứng nghỉ tại mỗi điểm
    public float arriveThreshold = 0.5f;            // Cách điểm đến bao nhiêu thì tính là "đã tới"

    [Header("Đuổi theo")]
    public float chaseSpeed = 4.5f;                 // Tốc độ khi đuổi Player
    public float loseSightTime = 3f;                // Mất dấu bao lâu thì bỏ cuộc và quay lại đi tuần
    public float destinationUpdateRate = 0.15f;     // Chu kỳ cập nhật đường đi (giây)

    protected NavMeshAgent agent;
    protected Animator animator;

    public enum State { Idle, Patrol, Chase, Atk }
    public State currentState = State.Idle;

    protected bool isDead = false;
    protected bool isAtking = false;
    protected bool isBlocking = false;

    // Nội bộ
    private Vector3 spawnPosition;
    private int waypointIndex = -1;
    private bool hasPatrolTarget;
    private bool isWaiting;
    private float waitTimer;
    private float nextDestinationTime;
    private float lastSeenTime = -999f;
    private Vector3 lastKnownPlayerPos;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        ChangeState(State.Patrol);
    }

    protected virtual void Update()
    {
        if (isDead)
        {
            StopAgent();
            return;
        }

        CheckVisionAndStates();
        HandleAnimations();
    }

    // --- KIỂM TRA TẦM NHÌN VÀ CHUYỂN TRẠNG THÁI ---
    protected virtual void CheckVisionAndStates()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Đang vung đòn thì khóa trạng thái, không bị hủy giữa chừng
        if (isAtking)
        {
            StopAgent();
            LookAtPlayer();
            return;
        }

        bool canSee = CanSeePlayer();
        if (canSee)
        {
            lastSeenTime = Time.time;
            lastKnownPlayerPos = player.position;
        }

        // Còn "nhớ" Player trong thời gian loseSightTime sau khi mất dấu
        bool remembers = Time.time - lastSeenTime <= loseSightTime;

        // 1. Trong tầm tấn công
        if (canSee && FlatDistanceToPlayer() <= attackRange)
        {
            ChangeState(State.Atk);
            StopAgent();
            LookAtPlayer();
        }
        // 2. Đang thấy Player, hoặc vừa mất dấu -> đuổi theo
        else if (canSee || remembers)
        {
            ChangeState(State.Chase);

            if (isBlocking)
            {
                StopAgent();
                return;
            }

            if (Time.time >= nextDestinationTime)
            {
                nextDestinationTime = Time.time + destinationUpdateRate;
                // Thấy thì đuổi theo Player, mất dấu thì chạy tới chỗ cuối cùng thấy
                agent.SetDestination(canSee ? player.position : lastKnownPlayerPos);
            }
        }
        // 3. Không thấy gì -> đi tuần
        else
        {
            PatrolLogic();
        }
    }

    // --- ĐI TUẦN ---
    protected virtual void PatrolLogic()
    {
        if (currentState != State.Patrol && currentState != State.Idle)
            ChangeState(State.Patrol);

        // Đang đứng nghỉ tại điểm tuần
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

        // Chưa có điểm đến -> chọn điểm mới
        if (!hasPatrolTarget)
        {
            if (!PickNextPatrolPoint()) return;
        }

        // Đã tới điểm -> đứng nghỉ
        if (!agent.pathPending && agent.remainingDistance <= arriveThreshold)
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
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            target = waypoints[waypointIndex].position;
        }
        else if (!TryGetRandomPoint(out target))
        {
            return false;
        }

        agent.isStopped = false;
        hasPatrolTarget = agent.SetDestination(target);
        return hasPatrolTarget;
    }

    // Lấy điểm ngẫu nhiên nằm trên NavMesh quanh vị trí spawn
    protected bool TryGetRandomPoint(out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 random = spawnPosition + Random.insideUnitSphere * patrolRadius;
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = spawnPosition;
        return false;
    }

    // --- ĐỔI TRẠNG THÁI (đặt tốc độ, khoảng dừng cho từng trạng thái) ---
    protected virtual void ChangeState(State newState)
    {
        if (currentState == newState && agent != null && agent.speed > 0f && newState != State.Patrol) return;

        State old = currentState;
        currentState = newState;

        if (agent == null) return;

        switch (newState)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0f;
                if (old == State.Chase || old == State.Atk)
                {
                    hasPatrolTarget = false;   // vừa hết đuổi -> chọn điểm tuần mới
                    isWaiting = false;
                }
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.2f);
                agent.isStopped = false;
                isWaiting = false;
                hasPatrolTarget = false;
                break;
        }
    }

    // --- TẦM NHÌN ---
    // Player có nằm trong khoảng cách + góc nhìn + không bị vật cản che không
    protected virtual bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > visionRange) return false;

        // Kiểm tra góc nhìn (bỏ qua nếu Player đang sát bên cạnh)
        if (dist > attackRange && visionAngle < 360f)
        {
            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > visionAngle * 0.5f) return false;
        }

        // Kiểm tra vật cản
        if (obstacleMask.value != 0)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.position + Vector3.up * 1f;
            if (Physics.Linecast(origin, target, obstacleMask)) return false;
        }

        return true;
    }

    // Khoảng cách phẳng (bỏ qua chênh lệch độ cao)
    protected float FlatDistanceToPlayer()
    {
        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = 0; b.y = 0;
        return Vector3.Distance(a, b);
    }

    protected void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    public virtual void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    public virtual void IdleLogic()
    {
        // Thêm hành vi khi đứng nghỉ tại điểm tuần (nhìn quanh, animation nhàn rỗi...)
    }

    // --- ĐỒNG BỘ ANIMATION DI CHUYỂN ---
    protected virtual void HandleAnimations()
    {
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
    }

    // --- VẼ GIZMOS ĐỂ DỄ CHỈNH TRONG SCENE VIEW ---
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Hai cạnh của góc nhìn
        Vector3 left = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, left * visionRange);
        Gizmos.DrawRay(transform.position, right * visionRange);

        // Vùng đi tuần ngẫu nhiên
        if (waypoints == null || waypoints.Length == 0)
        {
            Gizmos.color = Color.green;
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            Gizmos.DrawWireSphere(center, patrolRadius);
        }
    }
}