using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Mau")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Bat tu sau khi trung don")]
    [Tooltip("Thoi gian khong nhan sat thuong sau moi lan bi danh")]
    public float invincibleTime = 0.5f;

    [Header("Hoi mau tu dong (0 = tat)")]
    public float regenPerSecond = 0f;
    [Tooltip("Thoi gian cho sau khi bi danh moi bat dau hoi mau")]
    public float regenDelay = 3f;

    [Header("UI (khong bat buoc)")]
    public Slider healthSlider;
    public Image healthFill;
    public Gradient healthColor;

    [Header("Animation (khong bat buoc)")]
    public Animator animator;
    public string hitTrigger = "Hit";
    public string dieTrigger = "Die";

    [Header("Khi chet")]
    [Tooltip("Cac script se bi tat khi chet (vd: PlayerMovement, PlayerAttack)")]
    public MonoBehaviour[] disableOnDeath;

    [Header("Su kien")]
    public UnityEvent<int> onDamaged;   // so mau bi mat
    public UnityEvent<int> onHealed;    // so mau duoc hoi
    public UnityEvent onDeath;

    // Su kien cho code (vd: UI khac, GameManager)
    public event Action<int, int> HealthChanged; // (current, max)

    public int CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }
    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private float lastHitTime = -999f;
    private float regenBuffer;

    private int HitHash;
    private int DieHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        HitHash = Animator.StringToHash(hitTrigger);
        DieHash = Animator.StringToHash(dieTrigger);

        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (IsDead || regenPerSecond <= 0f || currentHealth >= maxHealth)
            return;

        if (Time.time - lastHitTime < regenDelay)
            return;

        // Cong don phan le de hoi mau deu (mau la so nguyen)
        regenBuffer += regenPerSecond * Time.deltaTime;

        if (regenBuffer >= 1f)
        {
            int amount = Mathf.FloorToInt(regenBuffer);
            regenBuffer -= amount;
            Heal(amount);
        }
    }

    // =========================
    // NHAN SAT THUONG
    // =========================

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        if (Time.time - lastHitTime < invincibleTime)
            return;

        lastHitTime = Time.time;
        regenBuffer = 0f;

        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        UpdateUI();
        onDamaged?.Invoke(before - currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (animator != null && HasParameter(HitHash))
            animator.SetTrigger(HitHash);
    }

    // =========================
    // HOI MAU
    // =========================

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0 || currentHealth >= maxHealth)
            return;

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        UpdateUI();
        onHealed?.Invoke(currentHealth - before);
    }

    // =========================
    // CHET & HOI SINH
    // =========================

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        if (animator != null && HasParameter(DieHash))
            animator.SetTrigger(DieHash);

        foreach (MonoBehaviour script in disableOnDeath)
        {
            if (script != null)
                script.enabled = false;
        }

        onDeath?.Invoke();
    }

    public void Respawn(Vector3 position)
    {
        // Tat CharacterController truoc khi doi vi tri, neu khong se bi ghi de
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.position = position;

        if (cc != null) cc.enabled = true;

        IsDead = false;
        currentHealth = maxHealth;
        lastHitTime = -999f;
        regenBuffer = 0f;

        foreach (MonoBehaviour script in disableOnDeath)
        {
            if (script != null)
                script.enabled = true;
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        UpdateUI();
    }

    // =========================
    // UI
    // =========================

    private void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthFill != null && healthColor != null)
            healthFill.color = healthColor.Evaluate(HealthPercent);

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Tranh warning khi Animator khong co trigger Hit / Die
    private bool HasParameter(int hash)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == hash)
                return true;
        }
        return false;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        if (!Application.isPlaying)
            currentHealth = maxHealth;
    }
}