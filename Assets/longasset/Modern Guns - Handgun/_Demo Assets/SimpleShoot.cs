using UnityEngine;

[AddComponentMenu("Nokobot/Modern Guns/Simple Shoot")]
public class SimpleShoot : MonoBehaviour
{
    public enum AimDirectionMode
    {
        BarrelForward,
        CameraForward
    }

    [Header("Prefab References")]
    public GameObject bulletPrefab;
    public GameObject casingPrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Location References")]
    public Animator gunAnimator;
    public Transform barrelLocation;
    public Transform casingExitLocation;

    [Header("Settings")]
    [SerializeField] private AimDirectionMode aimMode = AimDirectionMode.BarrelForward;
    [SerializeField] private float destroyTimer = 3f;
    [SerializeField] private float shotPower = 25f;
    [SerializeField] private float ejectPower = 250f;
    [SerializeField] private float fireCooldown = 0.2f;
    [SerializeField] private float bulletScaleMultiplier = 1f;
    [SerializeField] private bool fireDirectlyInTrigger = true;
    [SerializeField] private ForceMode shotForceMode = ForceMode.Impulse;

    [Header("Debug")]
    [SerializeField] private bool showDebugRayAndLog = true;
    [SerializeField] private float debugRayLength = 15f;

    [Header("Animation")]
    [SerializeField] private string fireTrigger = "Fire";

    private bool isEquipped;
    private float nextFireTime;

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (barrelLocation == null)
        {
            Transform foundBarrel = FindChildRecursive(transform, "Barrel_Location");
            if (foundBarrel == null)
            {
                foundBarrel = FindChildRecursive(transform, "BarrelLocation");
            }
            barrelLocation = foundBarrel != null ? foundBarrel : transform;
        }

        if (casingExitLocation == null)
        {
            Transform foundCasing = FindChildRecursive(transform, "CasingExitLocation");
            if (foundCasing == null)
            {
                foundCasing = FindChildRecursive(transform, "CasingExit");
            }
            if (foundCasing != null)
            {
                casingExitLocation = foundCasing;
            }
        }

        if (gunAnimator == null)
        {
            gunAnimator = GetComponentInChildren<Animator>();
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform result = FindChildRecursive(child, targetName);
            if (result != null)
                return result;
        }
        return null;
    }

    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
    }

    public void TriggerFire()
    {
        if (!isEquipped)
            return;

        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireCooldown;
        EnsureReferences();

        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger(fireTrigger);
        }

        if (fireDirectlyInTrigger || gunAnimator == null)
        {
            Shoot();
            CasingRelease();
        }
    }

    private Vector3 GetShootDirection()
    {
        if (aimMode == AimDirectionMode.CameraForward && Camera.main != null)
        {
            return Camera.main.transform.forward.normalized;
        }

        return barrelLocation != null ? barrelLocation.forward.normalized : transform.forward.normalized;
    }

    public void Shoot()
    {
        if (!isEquipped)
            return;

        EnsureReferences();

        if (barrelLocation == null)
        {
            barrelLocation = transform;
        }

        Vector3 shootDir = GetShootDirection();
        Quaternion shootRot = Quaternion.LookRotation(shootDir);

        if (showDebugRayAndLog)
        {
            Debug.DrawRay(barrelLocation.position, shootDir * debugRayLength, Color.yellow, 3f);
            Debug.Log($"[SimpleShoot] Bắn từ '{barrelLocation.name}' tại {barrelLocation.position} | dir={shootDir} | mode={aimMode}");
        }

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(
                muzzleFlashPrefab,
                barrelLocation.position,
                shootRot
            );

            Destroy(flash, destroyTimer);
        }

        if (bulletPrefab == null)
        {
            Debug.LogWarning("[SimpleShoot] Chưa gán Bullet Prefab trên Inspector của súng!");
            return;
        }

        GameObject bullet = Instantiate(
            bulletPrefab,
            barrelLocation.position,
            shootRot
        );

        if (!Mathf.Approximately(bulletScaleMultiplier, 1f))
        {
            bullet.transform.localScale *= bulletScaleMultiplier;
        }

        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bulletCol != null)
        {
            foreach (Collider ownerCol in transform.root.GetComponentsInChildren<Collider>())
            {
                if (ownerCol != null)
                {
                    Physics.IgnoreCollision(bulletCol, ownerCol, true);
                }
            }
        }

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = Vector3.zero;
            bulletRb.angularVelocity = Vector3.zero;
            bulletRb.AddForce(shootDir * shotPower, shotForceMode);
        }
    }

    public void CasingRelease()
    {
        if (!isEquipped)
            return;

        if (casingExitLocation == null || casingPrefab == null)
            return;

        GameObject casing = Instantiate(
            casingPrefab,
            casingExitLocation.position,
            casingExitLocation.rotation
        );

        Rigidbody casingRb = casing.GetComponent<Rigidbody>();

        if (casingRb != null)
        {
            casingRb.AddExplosionForce(
                Random.Range(ejectPower * 0.7f, ejectPower),
                casingExitLocation.position
                - casingExitLocation.right * 0.3f
                - casingExitLocation.up * 0.6f,
                1f
            );

            casingRb.AddTorque(
                new Vector3(
                    0,
                    Random.Range(100f, 500f),
                    Random.Range(100f, 1000f)
                ),
                ForceMode.Impulse
            );
        }

        Destroy(casing, destroyTimer);
    }
}