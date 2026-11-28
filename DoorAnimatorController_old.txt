using UnityEngine;

public class DoorAnimatorController : MonoBehaviour
{
    private Animator anim;
    private bool isOpen = false;
    private bool canInteract = false;

    void Start()
    {
        // Lấy component Animator trên chính DoorPivot
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Khi người chơi đứng trong vùng trigger và bấm E
        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen; // Đảo trạng thái mở / đóng
            anim.SetBool("isOpen", isOpen);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
        }
    }
}