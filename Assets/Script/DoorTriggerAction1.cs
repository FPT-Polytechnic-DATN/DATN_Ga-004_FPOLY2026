using UnityEngine;

public class DoorTriggerAction1 : MonoBehaviour
{
    [Header("Animator cho 2 cÃ¡nh cá»­a")]
    public Animator leftDoorAnimator;  // KÃ©o Animator cá»§a cÃ¡nh trÃ¡i vÃ o Ä‘Ã¢y
    public Animator rightDoorAnimator; // KÃ©o Animator cá»§a cÃ¡nh pháº£i vÃ o Ä‘Ã¢y

    [Header("CÃ i Ä‘áº·t kÃ­ch hoáº¡t")]
    public float openDistance = 3f;    // Khoáº£ng cÃ¡ch Ä‘á»ƒ má»Ÿ cá»­a
    public Transform player;           // KÃ©o nhÃ¢n váº­t Player vÃ o Ä‘Ã¢y (hoáº·c Ä‘á»ƒ trá»‘ng tá»± Ä‘á»™ng tÃ¬m theo Tag)

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

        // Kiá»ƒm tra khoáº£ng cÃ¡ch giá»¯a nhÃ¢n váº­t vÃ  cá»­a
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= openDistance)
        {
            // Má»Ÿ cáº£ 2 cÃ¡nh
            if (leftDoorAnimator != null) leftDoorAnimator.SetBool("isOpen", true);
            if (rightDoorAnimator != null) rightDoorAnimator.SetBool("isOpen", true);
        }
        else
        {
            // ÄÃ³ng láº¡i khi Ä‘i xa
            if (leftDoorAnimator != null) leftDoorAnimator.SetBool("isOpen", false);
            if (rightDoorAnimator != null) rightDoorAnimator.SetBool("isOpen", false);
        }
    }
}