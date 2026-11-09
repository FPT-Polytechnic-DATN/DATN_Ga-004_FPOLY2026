using UnityEngine;

public class DoorTriggerAction : MonoBehaviour
{
    [Header("Animator cho 2 cánh cửa")]
    public Animator leftDoorAnimator;  // Kéo Animator của cánh trái vào đây
    public Animator rightDoorAnimator; // Kéo Animator của cánh phải vào đây

    [Header("Cài đặt kích hoạt")]
    public float openDistance = 3f;    // Khoảng cách để mở cửa
    public Transform player;           // Kéo nhân vật Player vào đây (hoặc để trống tự động tìm theo Tag)

    void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Kiểm tra khoảng cách giữa nhân vật và cửa
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= openDistance)
        {
            // Mở cả 2 cánh
            if (leftDoorAnimator != null) leftDoorAnimator.SetBool("isOpen", true);
            if (rightDoorAnimator != null) rightDoorAnimator.SetBool("isOpen", true);
        }
        else
        {
            // Đóng lại khi đi xa
            if (leftDoorAnimator != null) leftDoorAnimator.SetBool("isOpen", false);
            if (rightDoorAnimator != null) rightDoorAnimator.SetBool("isOpen", false);
        }
    }
}