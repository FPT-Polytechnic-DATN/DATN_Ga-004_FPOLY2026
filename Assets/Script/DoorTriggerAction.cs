
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DoorTriggerAction : MonoBehaviour
{
    public Animator doorAnimator;

    [Header("Door Sound")]
    public AudioClip doorOpenSound;
    [Range(0f, 1f)]
    public float doorSoundVolume = 1f;

    private bool playerInRange = false;
    private bool hasOpened = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (playerInRange && !hasOpened &&
            Input.GetKeyDown(KeyCode.E))
        {
            hasOpened = true;

            if (doorAnimator != null)
            {
                doorAnimator.SetBool("isOpen", true);
            }

            if (doorOpenSound != null)
            {
                audioSource.PlayOneShot(
                    doorOpenSound, doorSoundVolume);
            }

            Collider col = GetComponent<Collider>();

            if (col != null)
                col.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}
