using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFlashlight : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 3f;
    public KeyCode pickupKey = KeyCode.R;
    public KeyCode turnOnKey = KeyCode.F;

    [Header("References")]
    public Transform flashlightHolder;
    public Light flashlightLight;

    [Header("Pickup Sound")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    private Transform cameraTransform;
    private GameObject worldFlashlight;
    private bool hasFlashlight = false;
    private bool isTurnedOn = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Tự động tìm đèn pin dưới đất
        GameObject foundLight = GameObject.FindWithTag("Flashlight");
        if (foundLight != null)
            worldFlashlight = foundLight;

        // Tự động tìm Camera của Player
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
        else
            cameraTransform = GetComponentInChildren<Camera>()?.transform;

        // Ban đầu tắt đèn
        if (flashlightLight != null)
            flashlightLight.enabled = false;
    }

    void Update()
    {
        // Nhấn R để nhặt đèn
        if (!hasFlashlight && worldFlashlight != null)
        {
            float distance = Vector3.Distance(
                transform.position, worldFlashlight.transform.position);

            if (distance <= pickupRange && Input.GetKeyDown(pickupKey))
                PickUpFlashlight();
        }

        // Nhấn F để bật đèn
        else if (hasFlashlight && !isTurnedOn && Input.GetKeyDown(turnOnKey))
        {
            TurnOnLight();
        }

        // Đèn đi theo hướng nhìn của Camera
        if (isTurnedOn && flashlightLight != null && cameraTransform != null)
        {
            flashlightLight.transform.position = cameraTransform.position;
            flashlightLight.transform.rotation = cameraTransform.rotation;
        }
    }

    void PickUpFlashlight()
    {
        hasFlashlight = true;

        // Phát tiếng nhặt đồ đúng thời điểm nhặt thành công
        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound, pickupVolume);

        Collider col = worldFlashlight.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = worldFlashlight.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Gắn mô hình đèn vào tay nhân vật
        if (flashlightHolder != null)
        {
            worldFlashlight.transform.SetParent(flashlightHolder);
            worldFlashlight.transform.localPosition = Vector3.zero;
            worldFlashlight.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("Chưa gán Flashlight Holder trong Inspector.");
        }

        if (flashlightLight != null)
            flashlightLight.enabled = false;

        Debug.Log("Đã nhặt đèn pin cầm trên tay!");
    }

    void TurnOnLight()
    {
        isTurnedOn = true;

        if (flashlightLight != null)
            flashlightLight.enabled = true;

        Debug.Log("Đã bật sáng đèn!");
    }
}
