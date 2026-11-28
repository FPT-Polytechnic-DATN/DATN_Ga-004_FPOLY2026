using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 15f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Jump & Gravity")]
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("Animation")]
    public Animator animator;
    public string shootTrigger = "Shoot";
    public string attackTrigger = "Attack";
    public string pickerTrigger = "Picker";

    [Header("Pickup Animation")]
    [Range(0.5f, 3f)]
    public float pickedSpeed = 2f;
    public float pickupDelay = 0.3f;

    [Header("Gun Detection")]
    public Transform pickupCheck;
    public float pickupRadius = 1.5f;
    public LayerMask gunLayer = ~0;

    [Header("Gun References")]
    public Transform gunHoldPoint;

    [Header("Melee Bat Settings")]
    public Transform batHoldPoint;
    public float batPickupRadius = 2.5f;
    public LayerMask batLayer = ~0;
    public bool triggerPickupAnimationForBat = true;

    private CharacterController controller;
    private Vector3 velocity;

    private bool isGrounded;
    private bool isRunning;
    private bool isPickingUp;

    private GunPickup nearbyGun;
    private SimpleShoot equippedGun;
    private BatWeapon equippedBat;

    public bool HasEquippedBat => equippedBat != null && equippedBat.IsEquipped;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (batHoldPoint == null)
        {
            batHoldPoint = FindChildRecursive(transform, "batHoldPoint");
            if (batHoldPoint == null)
                batHoldPoint = FindChildRecursive(transform, "BatHoldPoint");
        }

        // Tự động gắn batHoldPoint vào đúng xương bàn tay phải của Humanoid Animator (clip Mixamo)
        if (animator != null && animator.isHuman)
        {
            Transform rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHandBone != null)
            {
                if (batHoldPoint == null)
                {
                    GameObject holdObj = new GameObject("AutoBatHoldPoint");
                    batHoldPoint = holdObj.transform;
                }

                batHoldPoint.SetParent(rightHandBone, worldPositionStays: false);
                batHoldPoint.localPosition = Vector3.zero;
                batHoldPoint.localRotation = Quaternion.identity;
                batHoldPoint.localScale = Vector3.one;
                Debug.Log($"[PlayerMovement] Đã gắn batHoldPoint vào xương tay phải Animator: {rightHandBone.name}");
            }
        }

        if (animator != null &&
            HasAnimatorParameter("PickedSpeed"))
        {
            animator.SetFloat("PickedSpeed", pickedSpeed);
        }
    }

    private void Update()
    {
        CheckGround();
        MovePlayer();
        JumpPlayer();
        ApplyGravity();
        UpdateAnimator();

        // Luôn kiểm tra súng ở gần Player
        FindNearbyGun();

        // Nhấn E để nhặt súng hoặc gậy
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
        }

        // Nhấn chuột trái để vung gậy hoặc bắn súng
        if (Input.GetMouseButtonDown(0))
        {
            TryPrimaryAction();
        }
    }

    // Tìm súng bằng Collider
    private void FindNearbyGun()
    {
        if (isPickingUp || equippedGun != null || equippedBat != null)
            return;

        Vector3 checkPosition = pickupCheck != null
            ? pickupCheck.position
            : transform.position + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(
            checkPosition,
            pickupRadius,
            gunLayer,
            QueryTriggerInteraction.Collide
        );

        GunPickup closestGun = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            GunPickup gun = hit.GetComponentInParent<GunPickup>();

            if (gun == null || gun.IsPickedUp)
                continue;

            Vector3 closestPoint = hit.ClosestPoint(checkPosition);
            float distance = Vector3.Distance(
                checkPosition,
                closestPoint
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGun = gun;
            }
        }

        nearbyGun = closestGun;
    }

    private BatWeapon FindNearbyBat()
    {
        Vector3 checkPosition = pickupCheck != null
            ? pickupCheck.position
            : transform.position + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(
            checkPosition,
            Mathf.Max(pickupRadius, batPickupRadius),
            batLayer,
            QueryTriggerInteraction.Collide
        );

        BatWeapon closestBat = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            BatWeapon bat = hit.GetComponentInParent<BatWeapon>();
            if (bat == null || bat.IsEquipped || !bat.CanPickup(transform))
                continue;

            Vector3 closestPoint = hit.ClosestPoint(checkPosition);
            float distance = Vector3.Distance(checkPosition, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBat = bat;
            }
        }

        if (closestBat == null)
        {
            BatWeapon[] allBats = FindObjectsByType<BatWeapon>(FindObjectsSortMode.None);
            foreach (BatWeapon bat in allBats)
            {
                if (bat == null || bat.IsEquipped || !bat.CanPickup(transform))
                    continue;

                float distance = Vector3.Distance(transform.position, bat.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBat = bat;
                }
            }
        }

        return closestBat;
    }

    private void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    private void MovePlayer()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (cameraTransform == null)
            return;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 direction =
            camForward * vertical +
            camRight * horizontal;

        if (direction.magnitude >= 0.1f)
        {
            direction.Normalize();

            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            isRunning = Input.GetKey(KeyCode.LeftShift);

            float currentSpeed =
                isRunning ? runSpeed : walkSpeed;

            controller.Move(
                direction * currentSpeed * Time.deltaTime
            );

            if (animator != null)
            {
                animator.SetFloat(
                    "Speed",
                    currentSpeed,
                    0.1f,
                    Time.deltaTime
                );
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetFloat(
                    "Speed",
                    0f,
                    0.1f,
                    Time.deltaTime
                );
            }
        }
    }

    private void JumpPlayer()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool("IsGrounded", isGrounded);

        if (HasAnimatorParameter("PickedSpeed"))
        {
            animator.SetFloat("PickedSpeed", pickedSpeed);
        }
    }

    // Giữ lại để tương thích với GunPickup
    public void SetNearbyGun(GunPickup gun)
    {
        if (gun != null && !gun.IsPickedUp)
            nearbyGun = gun;
    }

    public void RemoveNearbyGun(GunPickup gun)
    {
        if (nearbyGun == gun)
            nearbyGun = null;
    }

    private void TryPickupItem()
    {
        if (isPickingUp || equippedGun != null || equippedBat != null)
            return;

        FindNearbyGun();
        if (nearbyGun != null)
        {
            TryPickupGun();
            return;
        }

        BatWeapon nearbyBat = FindNearbyBat();
        if (nearbyBat != null)
        {
            TryPickupBat(nearbyBat);
            return;
        }

        Debug.Log("Không có súng hoặc gậy trong phạm vi nhặt!");
    }

    // Nhấn E nhặt súng
    private void TryPickupGun()
    {
        if (isPickingUp || equippedGun != null || equippedBat != null)
            return;

        FindNearbyGun();

        if (nearbyGun == null)
        {
            Debug.Log("Không có súng trong phạm vi nhặt!");
            return;
        }

        if (gunHoldPoint == null)
        {
            Debug.LogError("Chưa gán GunHoldPoint!");
            return;
        }

        RotateTowardsCamera();

        isPickingUp = true;

        if (animator != null)
        {
            animator.ResetTrigger(shootTrigger);
            animator.ResetTrigger(attackTrigger);
            animator.SetFloat("PickedSpeed", pickedSpeed);
            animator.SetTrigger(pickerTrigger);
        }

        StartCoroutine(PickupRoutine(nearbyGun));
    }

    private IEnumerator PickupRoutine(GunPickup gun)
    {
        yield return new WaitForSeconds(pickupDelay);

        if (gun != null && !gun.IsPickedUp)
        {
            if (gun.PickUp(gunHoldPoint))
            {
                equippedGun = gun.GetShoot();
                nearbyGun = null;

                Debug.Log("Player đã nhặt súng!");
            }
        }

        isPickingUp = false;
    }

    private void TryPickupBat(BatWeapon bat)
    {
        if (bat == null || bat.IsEquipped)
            return;

        if (batHoldPoint == null)
        {
            batHoldPoint = FindChildRecursive(transform, "batHoldPoint");
            if (batHoldPoint == null)
                batHoldPoint = FindChildRecursive(transform, "DRV_DEF-hand.R");
        }

        if (batHoldPoint == null)
        {
            Debug.LogError("Chưa gán hoặc tìm thấy batHoldPoint!");
            return;
        }

        RotateTowardsCamera();
        isPickingUp = true;

        if (animator != null && triggerPickupAnimationForBat && HasAnimatorParameter(pickerTrigger))
        {
            animator.ResetTrigger(shootTrigger);
            animator.ResetTrigger(attackTrigger);
            animator.SetFloat("PickedSpeed", pickedSpeed);
            animator.SetTrigger(pickerTrigger);
        }

        StartCoroutine(PickupBatRoutine(bat));
    }

    private IEnumerator PickupBatRoutine(BatWeapon bat)
    {
        float delay = triggerPickupAnimationForBat ? pickupDelay : 0f;
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (bat != null && !bat.IsEquipped && batHoldPoint != null)
        {
            bat.Equip(transform, batHoldPoint);
            equippedBat = bat;
            Debug.Log($"Player đã nhặt gậy: {bat.name}");
        }

        isPickingUp = false;
    }

    private void TryPrimaryAction()
    {
        if (isPickingUp)
            return;

        if (HasEquippedBat)
        {
            TryBatAttack();
            return;
        }

        if (equippedGun != null)
        {
            TryShoot();
        }
    }

    private void TryBatAttack()
    {
        if (!HasEquippedBat)
            return;

        RotateTowardsCamera();

        if (!equippedBat.TryStartAttack())
            return;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger) && HasAnimatorParameter(attackTrigger))
        {
            animator.ResetTrigger(shootTrigger);
            animator.ResetTrigger(pickerTrigger);
            animator.SetTrigger(attackTrigger);
        }
    }

    // Bắn súng
    private void TryShoot()
    {
        if (equippedGun == null || isPickingUp)
            return;

        RotateTowardsCamera();

        if (animator != null)
        {
            animator.ResetTrigger(pickerTrigger);
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(shootTrigger);
        }

        equippedGun.TriggerFire();
    }

    private void RotateTowardsCamera()
    {
        if (cameraTransform == null)
            return;

        Vector3 direction = cameraTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform found = FindChildRecursive(child, targetName);
            if (found != null)
                return found;
        }
        return null;
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter
            in animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 position = pickupCheck != null
            ? pickupCheck.position
            : transform.position + Vector3.up;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, pickupRadius);
    }
}