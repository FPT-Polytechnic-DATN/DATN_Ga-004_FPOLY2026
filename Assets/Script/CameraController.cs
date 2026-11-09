using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target; // Kéo nhân vật Player vào đây

    [Header("Camera Settings")]
    public float mouseSensitivity = 3f;
    public float distanceFromTarget = 4f;
    public Vector2 pitchLimits = new Vector2(-20f, 60f); // Giới hạn góc nhìn lên xuống

    private float yaw = 0f;   // Góc xoay ngang
    private float pitch = 15f; // Góc xoay dọc

    void Start()
    {
        // Khóa và ẩn con trỏ chuột ở giữa màn hình (giống game thông thường)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Lấy chuyển động của chuột
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        // Tính toán vị trí và góc quay của camera xoay quanh nhân vật
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 targetPosition = target.position + new Vector3(0, 1.5f, 0) - (rotation * Vector3.forward * distanceFromTarget);

        transform.rotation = rotation;
        transform.position = targetPosition;
    }
}