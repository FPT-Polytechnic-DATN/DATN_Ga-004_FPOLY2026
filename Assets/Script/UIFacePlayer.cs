
using UnityEngine;

public class UIFacePlayer : MonoBehaviour
{
    public Transform player;
    public float rotationOffset = 180f;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;

        // Khóa hướng xoay X và Z
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(
                0,
                angle + rotationOffset,
                0
            );
        }
    }
}
