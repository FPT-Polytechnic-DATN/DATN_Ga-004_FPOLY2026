
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    [Header("Sound Effects")]
    public AudioSource audioSource;

    public AudioClip footstepSound;
    public AudioClip openDoorSound;
    public AudioClip pickUpSound;

    // Tiếng bước chân
    public void PlayFootstep()
    {
        audioSource.PlayOneShot(footstepSound);
    }

    // Tiếng mở cửa
    public void PlayOpenDoor()
    {
        audioSource.PlayOneShot(openDoorSound);
    }

    // Tiếng nhặt đồ
    public void PlayPickUp()
    {
        audioSource.PlayOneShot(pickUpSound);
    }
}
