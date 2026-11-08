using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5.5f;
    public float rotationSpeed = 50f;

    [Header("Rotation Offset")]
    [Tooltip("Nếu nhân vật bị lệch hướng đi thẳng, hãy chỉnh số này (ví dụ: 90, -90, 180)")]
    public float rotationOffset = 0f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Jump & Gravity")]
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // 1. Kiểm tra chạm đất
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer, QueryTriggerInteraction.Ignore);
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Nhận input bàn phím (Dùng GetAxisRaw để dừng ngay lập tức, không bị trượt)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 3. Tính hướng di chuyển theo góc nhìn Camera
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = forward * vertical + right * horizontal;
        }
        else
        {
            moveDirection = new Vector3(horizontal, 0f, vertical);
        }

        isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // 4. Di chuyển nhân vật (Tự động bước qua bậc cầu thang nhờ Character Controller)
        Vector3 movement = moveDirection.normalized * currentSpeed;
        controller.Move(movement * Time.deltaTime);

        // 5. Xoay hướng nhân vật kèm góc bù (Rotation Offset) để sửa lỗi đi xéo
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            targetRotation *= Quaternion.Euler(0, rotationOffset, 0); // Cộng góc bù lệch nếu model 3D bị xoay sai hướng

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 6. Nhảy
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 7. Trọng lực
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 8. Cập nhật Animator khớp với các điều kiện trong Animator Controller
        if (animator != null)
        {
            float animSpeed = 0f;
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                animSpeed = isRunning ? runSpeed : walkSpeed;
            }

            animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}