using UnityEngine;

public class DoorOnceController : MonoBehaviour
{
    [Header("Kéo Animator của DoorPivot vào đây")]
    public Animator doorAnimator;

    private bool playerInRange = false;
    private bool hasOpened = false; // Cờ kiểm tra đã mở chưa

    void Update()
    {
        // Chỉ nhận phím E nếu người chơi ở gần VÀ cửa chưa từng mở
        if (playerInRange && !hasOpened && Input.GetKeyDown(KeyCode.E))
        {
            hasOpened = true; // Đánh dấu đã dùng

            if (doorAnimator != null)
            {
                doorAnimator.SetBool("isOpen", true);
            }

            // Tùy chọn: Tắt luôn collider trigger để không còn nhận diện người chơi nữa
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}