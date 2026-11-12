
using UnityEngine;

public class DoorNearby : MonoBehaviour
{
    public GameObject itemPrompt;

    private void Start()
    {
        itemPrompt.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            itemPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            itemPrompt.SetActive(false);
        }
    }

    public void HidePrompt()
    {
        itemPrompt.SetActive(false);
    }
}
