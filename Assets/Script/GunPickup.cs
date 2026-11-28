
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GunPickup : MonoBehaviour
{
    [Header("Gun Model")]
    public Transform gunModel;

    [Header("Gun Position In Hand")]
    public Vector3 handPositionOffset = Vector3.zero;
    public Vector3 handRotationOffset = Vector3.zero;

    [Header("Gun Scale In Hand")]
    public Vector3 handScale = new Vector3(0.001f, 0.001f, 0.001f);

    [Header("Pickup Settings")]
    public bool disableCollidersAfterPickup = true;

    public bool IsPickedUp { get; private set; }

    private Collider[] gunColliders;
    private Collider pickupCollider;
    private Rigidbody gunRigidbody;
    private SimpleShoot simpleShoot;

    private void Awake()
    {
        // Lấy các component
        gunColliders = GetComponentsInChildren<Collider>(true);
        pickupCollider = GetComponent<Collider>();
        gunRigidbody = GetComponent<Rigidbody>();

        simpleShoot = GetComponentInChildren<SimpleShoot>(true);

        // Tự tìm model súng
        if (gunModel == null)
        {
            gunModel = transform.Find("M1911 Handgun_Model");
        }

        if (gunModel == null)
        {
            Debug.LogError(
                "GunPickup: Không tìm thấy M1911 Handgun_Model!"
            );
        }

        // Cài đặt Rigidbody
        if (gunRigidbody != null)
        {
            if (!gunRigidbody.isKinematic)
            {
                gunRigidbody.linearVelocity = Vector3.zero;
                gunRigidbody.angularVelocity = Vector3.zero;
            }

            gunRigidbody.isKinematic = true;
            gunRigidbody.useGravity = false;
        }

        // Collider gốc dùng để phát hiện người chơi
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }

        // Súng chưa được nhặt thì chưa thể bắn
        if (simpleShoot != null)
        {
            simpleShoot.SetEquipped(false);
        }
        else
        {
            Debug.LogError(
                "GunPickup: Không tìm thấy SimpleShoot!"
            );
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPickedUp) return;

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            player.SetNearbyGun(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            player.RemoveNearbyGun(this);
        }
    }

    public bool PickUp(Transform holdPoint)
    {
        if (IsPickedUp) return false;

        if (holdPoint == null)
        {
            Debug.LogError(
                "GunPickup: Chưa gán GunHoldPoint!"
            );
            return false;
        }

        if (gunModel == null)
        {
            gunModel = transform.Find("M1911 Handgun_Model");
        }

        if (gunModel == null)
        {
            Debug.LogError(
                "GunPickup: Không tìm thấy model súng!"
            );
            return false;
        }

        if (simpleShoot == null)
        {
            simpleShoot =
                GetComponentInChildren<SimpleShoot>(true);
        }

        if (simpleShoot == null)
        {
            Debug.LogError(
                "GunPickup: Không tìm thấy SimpleShoot!"
            );
            return false;
        }

        IsPickedUp = true;

        // Dừng vật lý
        if (gunRigidbody != null)
        {
            if (!gunRigidbody.isKinematic)
            {
                gunRigidbody.linearVelocity = Vector3.zero;
                gunRigidbody.angularVelocity = Vector3.zero;
            }

            gunRigidbody.isKinematic = true;
            gunRigidbody.useGravity = false;
        }

        // Gắn object súng vào tay nhân vật
        transform.SetParent(holdPoint, false);

        // Chỉnh vị trí và góc xoay
        transform.localPosition = handPositionOffset;
        transform.localRotation =
            Quaternion.Euler(handRotationOffset);

        // Thu nhỏ trực tiếp model súng
        gunModel.localScale = handScale;

        Debug.Log(
            "Đã nhặt súng! Model Scale: " +
            gunModel.localScale
        );

        // Tắt tất cả Collider sau khi nhặt
        if (disableCollidersAfterPickup &&
            gunColliders != null)
        {
            foreach (Collider col in gunColliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        // Cho phép sử dụng súng
        simpleShoot.SetEquipped(true);

        Debug.Log("Nhặt súng thành công!");

        return true;
    }

    public SimpleShoot GetShoot()
    {
        return simpleShoot;
    }
}
