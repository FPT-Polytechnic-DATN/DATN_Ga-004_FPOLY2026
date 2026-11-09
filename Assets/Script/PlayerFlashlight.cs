using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 3f;
    public KeyCode pickupKey = KeyCode.R;
    public KeyCode turnOnKey = KeyCode.F;

    [Header("References")]
    public Transform flashlightHolder;    // Vẫn giữ mô hình ở tay (kéo FlashlightHolder vào đây)
    public Light flashlightLight;         // Kéo WhiteLight vào đây

    private Transform cameraTransform;    // Code sẽ tự động tìm Camera
    private GameObject worldFlashlight;
    private bool hasFlashlight = false;
    private bool isTurnedOn = false;

    void Start()
    {
        // Tự động tìm đèn pin dưới đất
        GameObject foundLight = GameObject.FindWithTag("Flashlight");
        if (foundLight != null)
        {
            worldFlashlight = foundLight;
        }

        // Tự động tìm Camera của Player mà không cần kéo thả thủ công
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }

        // Ban đầu tắt đèn
        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }
    }

    void Update()
    {
        // 1. Nhấn R để nhặt đèn
        if (!hasFlashlight && worldFlashlight != null)
        {
            float distance = Vector3.Distance(transform.position, worldFlashlight.transform.position);
            if (distance <= pickupRange && Input.GetKeyDown(pickupKey))
            {
                PickUpFlashlight();
            }
        }
        // 2. Nhấn F để bật sáng
        else if (hasFlashlight && !isTurnedOn && Input.GetKeyDown(turnOnKey))
        {
            TurnOnLight();
        }

        // 3. Tự động xoay hướng ánh sáng theo góc nhìn của Camera
        if (isTurnedOn && flashlightLight != null && cameraTransform != null)
        {
            flashlightLight.transform.position = cameraTransform.position; // Đặt đèn đi theo vị trí camera
            flashlightLight.transform.rotation = cameraTransform.rotation; // Chiếu theo hướng nhìn camera
        }
    }

    void PickUpFlashlight()
    {
        hasFlashlight = true;

        // Tắt vật lý đèn dưới đất
        Collider col = worldFlashlight.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = worldFlashlight.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Gắn mô hình đèn vào tay nhân vật
        worldFlashlight.transform.SetParent(flashlightHolder);
        worldFlashlight.transform.localPosition = Vector3.zero;
        worldFlashlight.transform.localRotation = Quaternion.identity;

        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }

        Debug.Log("Đã nhặt đèn pin cầm trên tay!");
    }

    void TurnOnLight()
    {
        isTurnedOn = true;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = true;
        }
        Debug.Log("Đã bật sáng đèn!");
    }
}