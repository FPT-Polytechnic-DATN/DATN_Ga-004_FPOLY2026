using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BatWeapon : MonoBehaviour
{
    [Header("Pickup & Hold Settings")]
    [SerializeField] private float pickupRange = 3f;
    [Tooltip("Tự reset localPosition về (0, 0, 0) khi nhặt gậy.")]
    [SerializeField] private bool resetLocalPositionOnEquip = true;
    [SerializeField] private Vector3 holdLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 holdLocalEulerAngles = Vector3.zero;
    [SerializeField] private bool preserveWorldScaleOnEquip = true;
    [SerializeField] private Vector3 customHoldLocalScale = Vector3.one;

    [Header("Box Collider & Tag Settings")]
    [Tooltip("Kéo Box Collider vào đây, hoặc để trống để tự dùng/tạo Box Collider trên gậy.")]
    [SerializeField] private BoxCollider hitBoxCollider;
    [SerializeField] private Vector3 defaultBoxCenter = new Vector3(0f, 0f, 0.5f);
    [SerializeField] private Vector3 defaultBoxSize = new Vector3(0.5f, 0.5f, 1.5f);
    [Tooltip("Tag của quái cần đánh trúng.")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Melee Attack Settings")]
    [SerializeField] private int damagePerHit = 20;
    [SerializeField] private float attackCooldown = 0.45f;
    [SerializeField] private float hitStartDelay = 0f;
    [SerializeField] private float hitActiveDuration = 0.65f;
    [SerializeField] private float swingCheckRadius = 1.7f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private bool isEquipped;
    private bool isAttacking;
    private bool isHitWindowOpen;
    private float nextAttackTime;

    private Transform ownerRoot;
    private Vector3 initialWorldScale;
    private readonly HashSet<EnemyHealth> hitEnemiesInSwing = new HashSet<EnemyHealth>();
    private readonly List<Collider> allBatColliders = new List<Collider>();
    private Rigidbody rb;

    public bool IsEquipped => isEquipped;
    public float AttackCooldown => attackCooldown;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialWorldScale = transform.lossyScale;

        EnsureHitBoxCollider();
        allBatColliders.AddRange(GetComponentsInChildren<Collider>(true));
        SetEquippedState(false);
    }

    private void Update()
    {
        if (isEquipped && isHitWindowOpen)
        {
            ScanTaggedEnemies();
        }
    }

    private void EnsureHitBoxCollider()
    {
        if (hitBoxCollider == null)
        {
            hitBoxCollider = GetComponent<BoxCollider>();
        }

        if (hitBoxCollider == null)
        {
            hitBoxCollider = gameObject.AddComponent<BoxCollider>();
            hitBoxCollider.center = defaultBoxCenter;
            hitBoxCollider.size = defaultBoxSize;
        }

        hitBoxCollider.isTrigger = true;
    }

    public bool CanPickup(Transform playerTransform)
    {
        if (isEquipped || playerTransform == null)
            return false;

        float dist = Vector3.Distance(playerTransform.position, transform.position);
        return dist <= pickupRange;
    }

    public void Equip(Transform playerRoot, Transform holdParent)
    {
        if (isEquipped || holdParent == null)
            return;

        isEquipped = true;
        ownerRoot = playerRoot;

        Vector3 targetWorldScale = initialWorldScale;

        transform.SetParent(holdParent, worldPositionStays: false);
        transform.localPosition = resetLocalPositionOnEquip ? Vector3.zero : holdLocalPosition;
        transform.localRotation = Quaternion.Euler(holdLocalEulerAngles);

        if (preserveWorldScaleOnEquip)
        {
            Vector3 parentScale = holdParent.lossyScale;
            transform.localScale = new Vector3(
                Mathf.Abs(parentScale.x) > 0.0001f ? targetWorldScale.x / parentScale.x : targetWorldScale.x,
                Mathf.Abs(parentScale.y) > 0.0001f ? targetWorldScale.y / parentScale.y : targetWorldScale.y,
                Mathf.Abs(parentScale.z) > 0.0001f ? targetWorldScale.z / parentScale.z : targetWorldScale.z
            );
        }
        else
        {
            transform.localScale = customHoldLocalScale;
        }

        SetEquippedState(true);

        if (debugLogs)
        {
            Debug.Log($"[BatWeapon] Đã cầm gậy vào {holdParent.name} | Damage={damagePerHit} | Enemy Tag='{enemyTag}'");
        }
    }

    private void SetEquippedState(bool equipped)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        EnsureHitBoxCollider();

        foreach (Collider col in allBatColliders)
        {
            if (col == null)
                continue;

            if (!equipped)
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            else
            {
                col.isTrigger = true;
                col.enabled = false;
            }
        }
    }

    public bool TryStartAttack()
    {
        if (!isEquipped || isAttacking || Time.time < nextAttackTime)
            return false;

        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackWindowRoutine());
        return true;
    }

    private IEnumerator AttackWindowRoutine()
    {
        isAttacking = true;
        hitEnemiesInSwing.Clear();

        if (hitStartDelay > 0f)
        {
            yield return new WaitForSeconds(hitStartDelay);
        }

        SetHitBoxActive(true);
        ScanTaggedEnemies();

        yield return new WaitForSeconds(Mathf.Max(0.05f, hitActiveDuration));

        SetHitBoxActive(false);
        isAttacking = false;
    }

    public void AnimEvent_EnableHitWindow()
    {
        if (!isEquipped)
            return;

        hitEnemiesInSwing.Clear();
        SetHitBoxActive(true);
        ScanTaggedEnemies();
    }

    public void AnimEvent_DisableHitWindow()
    {
        SetHitBoxActive(false);
    }

    private void SetHitBoxActive(bool active)
    {
        isHitWindowOpen = active;
        EnsureHitBoxCollider();

        if (hitBoxCollider != null)
        {
            hitBoxCollider.isTrigger = true;
            hitBoxCollider.enabled = active && isEquipped;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitTarget(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHitTarget(other.gameObject);
    }

    private void ScanTaggedEnemies()
    {
        if (!isEquipped || !isHitWindowOpen)
            return;

        Vector3 origin = ownerRoot != null
            ? ownerRoot.position + ownerRoot.forward * 1.1f + Vector3.up * 1.0f
            : transform.position;

        Collider[] hits = Physics.OverlapSphere(origin, swingCheckRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider col in hits)
        {
            if (col != null)
            {
                TryHitTarget(col.gameObject);
            }
        }
    }

    private bool HasEnemyTag(GameObject obj)
    {
        if (string.IsNullOrEmpty(enemyTag))
            return true;

        Transform current = obj.transform;
        while (current != null)
        {
            if (current.CompareTag(enemyTag))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void TryHitTarget(GameObject target)
    {
        if (!isEquipped || !isHitWindowOpen || target == null)
            return;

        if (ownerRoot != null && target.transform.root == ownerRoot)
            return;

        if (!HasEnemyTag(target))
            return;

        EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.IsDead)
            return;

        if (hitEnemiesInSwing.Contains(enemy))
            return;

        hitEnemiesInSwing.Add(enemy);

        if (debugLogs)
        {
            Debug.Log($"[BatWeapon] Trúng {enemy.name} | dmg={damagePerHit}");
        }

        enemy.TakeDamage(damagePerHit);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = hitBoxCollider != null ? hitBoxCollider : GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = isHitWindowOpen ? Color.red : Color.green;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}