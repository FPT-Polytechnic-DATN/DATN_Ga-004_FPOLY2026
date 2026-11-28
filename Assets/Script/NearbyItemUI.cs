
using UnityEngine;
using TMPro;

public class NearbyItemUI : MonoBehaviour
{
    [Header("Item Information")]
    public string itemName = "Flashlight";

    [Header("UI References")]
    public GameObject itemPrompt;
    public TMP_Text itemNameText;

    [Header("Settings")]
    public string playerTag = "Player";

    private bool playerNearby = false;
    private bool isCollected = false;

    private void Start()
    {
        itemPrompt.SetActive(false);
        itemNameText.text = itemName;
    }

    private void Update()
    {
        if (playerNearby && !isCollected)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                PickUpItem();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isCollected)
        {
            playerNearby = true;
            itemPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerNearby = false;
            itemPrompt.SetActive(false);
        }
    }

    private void PickUpItem()
    {
        isCollected = true;
        playerNearby = false;

        // Ẩn UI
        itemPrompt.SetActive(false);

        // Tắt Collider để không kích hoạt lại
        GetComponent<Collider>().enabled = false;

        // Ẩn vật phẩm
        gameObject.SetActive(false);

        Debug.Log("Picked up: " + itemName);
    }
}
