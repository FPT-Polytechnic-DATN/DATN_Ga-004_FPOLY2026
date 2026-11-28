using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float smoothSpeed = 12f;

    [Header("UI & Anchor")]
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.1f, 0f);
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Gradient healthGradient;
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool hideWhenFull = false;

    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 0.25f;
    [SerializeField] private bool debugLogs = true;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    private float targetRatio = 1f;
    private Camera activeCamera;
    private bool hasDied;

    private void Awake()
    {
        currentHealth = maxHealth;
        targetRatio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        hasDied = false;

        if (fillImage == null && healthSlider != null && healthSlider.fillRect != null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
        }

        if (healthSlider == null && fillImage != null)
        {
            healthSlider = fillImage.GetComponentInParent<Slider>();
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.wholeNumbers = false;
            healthSlider.value = targetRatio;
        }

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = targetRatio;
        }

        if (healthCanvas == null && fillImage != null)
        {
            healthCanvas = fillImage.GetComponentInParent<Canvas>();
        }

        if (healthCanvas != null && hideWhenFull)
        {
            healthCanvas.gameObject.SetActive(targetRatio < 1f && currentHealth > 0f);
        }

        ResolveCamera();
        UpdateVisualsImmediate(targetRatio);
    }

    private void LateUpdate()
    {
        float currentVisual = fillImage != null
            ? fillImage.fillAmount
            : (healthSlider != null ? healthSlider.value : targetRatio);

        float nextVisual = Mathf.Lerp(currentVisual, targetRatio, Time.unscaledDeltaTime * smoothSpeed);
        if (Mathf.Abs(nextVisual - targetRatio) < 0.001f)
        {
            nextVisual = targetRatio;
        }

        UpdateVisualsImmediate(nextVisual);

        if (healthCanvas == null || !healthCanvas.gameObject.activeInHierarchy)
            return;

        Vector3 basePos = headAnchor != null ? headAnchor.position : transform.position;
        healthCanvas.transform.position = basePos + localOffset;

        if (!faceCamera)
            return;

        if (activeCamera == null || !activeCamera.isActiveAndEnabled)
        {
            ResolveCamera();
        }

        if (activeCamera != null)
        {
            Vector3 lookDir = healthCanvas.transform.position - activeCamera.transform.position;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                healthCanvas.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    private void UpdateVisualsImmediate(float ratio)
    {
        float clamped = Mathf.Clamp01(ratio);

        if (fillImage != null)
        {
            fillImage.fillAmount = clamped;
        }

        if (healthSlider != null)
        {
            healthSlider.value = clamped;
        }

        if (fillImage != null && healthGradient != null && healthGradient.colorKeys.Length > 0)
        {
            fillImage.color = healthGradient.Evaluate(clamped);
        }
    }

    private void ResolveCamera()
    {
        activeCamera = Camera.main;
        if (activeCamera == null)
        {
            activeCamera = FindFirstObjectByType<Camera>();
        }
    }

    public void SetMaxHealth(float newMaxHealth, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = refill ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
        targetRatio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        UpdateVisualsImmediate(targetRatio);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead || hasDied)
            return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        targetRatio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        if (debugLogs)
        {
            Debug.Log($"[EnemyHealth] {name} -{damage} HP -> {currentHealth}/{maxHealth} (ratio={targetRatio:0.00})");
        }

        if (healthCanvas != null && hideWhenFull)
        {
            healthCanvas.gameObject.SetActive(targetRatio < 1f && currentHealth > 0f);
        }

        if (currentHealth <= 0f)
        {
            OnEnemyDead();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead || hasDied)
            return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        targetRatio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    }

    private void OnEnemyDead()
    {
        if (hasDied)
            return;

        hasDied = true;

        if (debugLogs)
        {
            Debug.Log($"[EnemyHealth] {name} đã hết máu!");
        }

        UpdateVisualsImmediate(0f);

        if (healthCanvas != null)
        {
            healthCanvas.gameObject.SetActive(false);
        }

        NavMeshAgent agent = GetComponentInChildren<NavMeshAgent>();
        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in cols)
        {
            col.enabled = false;
        }

        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in bodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}