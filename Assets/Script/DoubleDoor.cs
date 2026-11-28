using UnityEngine;
using UnityEngine.InputSystem;

public class DoubleDoor : MonoBehaviour
{
    [Header("Hai trục bản lề cửa")]
    public Transform leftDoorPivot;
    public Transform rightDoorPivot;

    [Header("Cài đặt góc mở & tốc độ")]
    public float openAngle = 90f;
    public float openSpeed = 5f;
    public float interactDistance = 3f;

    private bool isOpen = false;
    private Transform player;
    private Quaternion leftClosedRot, rightClosedRot;
    private Quaternion leftOpenRot, rightOpenRot;

    void Start()
    {
        // Tự động tìm kiếm nếu chưa kéo vào Inspector
        if (leftDoorPivot == null) leftDoorPivot = transform.Find("DoorDL_Pivot");
        if (rightDoorPivot == null) rightDoorPivot = transform.Find("DoorDR_Pivot");

        // Tìm Player theo Tag
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy đối tượng nào mang Tag 'Player' trong Scene!");
        }

        // Lưu góc đóng ban đầu
        if (leftDoorPivot != null) leftClosedRot = leftDoorPivot.localRotation;
        if (rightDoorPivot != null) rightClosedRot = rightDoorPivot.localRotation;

        // Tính góc mở (đổi dấu nếu cửa quay ngược hướng)
        leftOpenRot = leftClosedRot * Quaternion.Euler(0, -openAngle, 0);
        rightOpenRot = rightClosedRot * Quaternion.Euler(0, openAngle, 0);
    }

    void Update()
    {
        if (player == null || leftDoorPivot == null || rightDoorPivot == null) return;

        // Kiểm tra khoảng cách tới cửa
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactDistance)
        {
            // Nhấn phím E để bật/tắt trạng thái mở cửa
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
                Debug.Log("Trạng thái cửa đổi thành: " + (isOpen ? "Mở" : "Đóng"));
            }
        }

        // Nội suy góc quay mượt mà
        Quaternion targetLeft = isOpen ? leftOpenRot : leftClosedRot;
        Quaternion targetRight = isOpen ? rightOpenRot : rightClosedRot;

        leftDoorPivot.localRotation = Quaternion.Slerp(leftDoorPivot.localRotation, targetLeft, Time.deltaTime * openSpeed);
        rightDoorPivot.localRotation = Quaternion.Slerp(rightDoorPivot.localRotation, targetRight, Time.deltaTime * openSpeed);
    }
}