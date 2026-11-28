
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float fallbackSpeed = 25f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 15f;

    [Header("Hit Settings")]
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private bool logHits = true;

    private Rigidbody rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
        }
    }

    private void Start()
    {
        Debug.Log("[Bullet] Viên đạn được tạo: " + name);

        if (rb != null && !rb.isKinematic)
        {
            if (rb.linearVelocity.sqrMagnitude < 0.01f)
            {
                rb.linearVelocity =
                    transform.forward * fallbackSpeed;
            }
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider other)
    {
        if (other == null || hasHit)
            return;

        // Bỏ qua Player
        if (other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player"))
        {
            return;
        }

        // Bỏ qua súng
        if (other.GetComponentInParent<GunPickup>() != null)
            return;

        // Đánh dấu đã va chạm để tránh gây sát thương nhiều lần
        hasHit = true;

        // Tìm EnemyHealth ở object hiện tại hoặc object cha
        EnemyHealth enemy =
            other.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            Debug.Log(
                "[Bullet] Đã bắn trúng quái: " + enemy.name
            );

            enemy.TakeDamage(damage);

            Debug.Log(
                "[Bullet] Đã gây " + damage +
                " sát thương cho " + enemy.name
            );
        }
        else
        {
            if (logHits)
            {
                Debug.Log(
                    "[Bullet] Va chạm với vật thể: " + other.name +
                    " nhưng không tìm thấy EnemyHealth."
                );
            }
        }

        // Hủy viên đạn sau va chạm
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
