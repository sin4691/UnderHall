using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public enum PlayerState { Idle, Move, Attack, Dash, SpecialAttack, Dead }

[RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerDash))]
public class Player : MonoBehaviour
{
    public PlayerData playerData;
    public PlayerState CurrentState { get; private set; }

    public Rigidbody rb { get; private set; }
    public PlayerMovement movement { get; private set; }
    public PlayerAttack attack { get; private set; }
    public Animator animator { get; private set; }
    public PlayerDash dash { get; private set; }
    public bool IsInvincible { get; private set; } = false;

    private Coroutine invincibilityCoroutine;
    private float currentHealth;
    private Vector2 inputVector;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        dash = GetComponent<PlayerDash>();
        animator = GetComponent<Animator>();

        CurrentState = PlayerState.Idle;
    }
    private void Start()
    {
        if (playerData != null) currentHealth = playerData.maxHealth;
    }

    public void ChangeState(PlayerState newState)
    {
        CurrentState = newState;
    }
    public void GrantInvincibility(float duration)
    {
        if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));
    }
    private IEnumerator InvincibilityRoutine(float duration)
    {
        IsInvincible = true;     
        yield return new WaitForSeconds(duration);
        IsInvincible = false;
    }

    public void TakeDamage(float damage)
    {
        if (IsInvincible || CurrentState == PlayerState.Dead) return;
        if (IsInvincible)
        {
            Debug.Log("회피 성공! (무적 상태라 데미지 무시)");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"플레이어 피격! 남은 체력: {currentHealth}");

        GrantInvincibility(playerData.hitInvincibilityTime);

        if (currentHealth <= 0)
        {
            Die(); 
        }
        else
        {
            GrantInvincibility(playerData.hitInvincibilityTime);

        }
    }

    private void Die()
    {
        if (CurrentState == PlayerState.Dead) return;
        Debug.Log("플레이어 사망!");

        ChangeState(PlayerState.Dead);
        attack.CancelAttack();
        inputVector = Vector2.zero;
        rb.linearVelocity = Vector3.zero;
        if (TryGetComponent<PlayerInput>(out var input))
        {
            input.enabled = false;
        }
        animator.CrossFade("death", 0.1f);
        GetComponent<Collider>().enabled = false;
    }

    public void OnDash(InputValue value)
    {
        if (CurrentState == PlayerState.Dead) return;
        if (value.isPressed)
        {
            dash.ExecuteDash();
        }
    }

    public void OnMove(InputValue value)
    {
        if (CurrentState == PlayerState.Dead) return;
        inputVector = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (CurrentState == PlayerState.Dead) return;
        if (value.isPressed && CurrentState != PlayerState.Dash)
        {
            attack.ExecuteAttack();
        }
    }
    public void OnSpecialAttack(InputValue value)
    {
        if (CurrentState == PlayerState.Dead) return;
        if (value.isPressed)
        {
            if (CurrentState != PlayerState.Dash)
            {
                attack.StartSpecialAttack();
            }
        }
        else
        {
            attack.StopSpecialAttack();
        }
    }

    public Vector2 GetInputVector() => inputVector;
}