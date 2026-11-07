
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float acceleration = 25f;
    public float deceleration = 35f;
    public float rotationSpeed = 540f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Jump")]
    public float jumpForce = 6f;
    public Transform groundCheck;
    public float groundDistance = 0.25f;
    public LayerMask groundLayer;

    [Header("Animation")]
    public Animator animator;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 currentVelocity;

    private bool isGrounded;
    private bool jumpPressed;
    private bool isRunning;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0, vertical);
        input = Vector3.ClampMagnitude(input, 1f);

        // Camera relative movement
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0;
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            moveDirection = forward * input.z + right * input.x;
        }
        else
        {
            moveDirection = input;
        }

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Kiểm tra mặt đất
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }

        // Animation
        if (animator != null)
        {
            float speed = moveDirection.magnitude *
                (isRunning ? runSpeed : walkSpeed);

            animator.SetFloat("Speed", speed, 0.15f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
        }

        // Nhảy
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpPressed = true;
        }
    }

    void FixedUpdate()
    {
        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 targetVelocity = moveDirection * targetSpeed;

        // Tăng tốc và giảm tốc riêng biệt
        float rate = moveDirection.sqrMagnitude > 0.01f
            ? acceleration
            : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(
            currentVelocity.x,
            rb.linearVelocity.y,
            currentVelocity.z
        );

        // Xoay nhân vật mượt theo hướng di chuyển
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            rb.MoveRotation(
                Quaternion.RotateTowards(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                )
            );
        }

        // Thực hiện nhảy
        if (jumpPressed)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            jumpPressed = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundDistance
        );
    }
}
