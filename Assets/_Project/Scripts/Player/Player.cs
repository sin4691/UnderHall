using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;

public enum PlayerState { Idle, Move, Attack, Dash, SpecialAttack, Dead, Resurrecting }

[RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerDash))]
public class Player : MonoBehaviour
{
    //VFX 위치 지정용
    [Header("VFX")]
    public Transform vfxPoint;
    //// VFXPoint_Body 드래그

    [Header("Camera Zoom Settings")]
    public CinemachineCamera virtualCamera; 
    public float zoomInFOV = 30f;  
    public float zoomDuration = 0.5f;  
    private float originalFOV;
    public float CurrentHealth => currentHealth;

    public PlayerData playerData;
    public PlayerState CurrentState { get; private set; }

    public Rigidbody rb { get; private set; }
    public PlayerMovement movement { get; private set; }
    public PlayerAttack attack { get; private set; }
    public Animator animator { get; private set; }
    public PlayerDash dash { get; private set; }
    public bool IsInvincible { get; private set; } = false;

    private Coroutine invincibilityCoroutine;
    private int remainingResurrections;
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
        if (virtualCamera == null)
        {
            virtualCamera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);
        }
        if (playerData != null)
        {
            currentHealth = playerData.maxHealth;
            remainingResurrections = playerData.maxResurrectionCount;
        }

        if (virtualCamera != null)
        {
            originalFOV = virtualCamera.Lens.FieldOfView;
        }
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
        if (IsInvincible || CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;

        currentHealth -= damage;

        Debug.Log($"플레이어 피격! 남은 체력: {currentHealth}");

        //창우_피격 이펙트
        VFXManager.Instance.PlayPlayerHit(vfxPoint.position, Vector3.up, gameObject);
        //창우_피격 이펙트는 VFXManager에서 구현한 PlayPlayerHit 함수를 호출하여 재생합니다. 이 함수는 피격 위치와 방향, 그리고 플레이어 객체를 인자로 받아서 적절한 피격 이펙트를 생성합니다.

        if (currentHealth <= 0)
        {
            if (remainingResurrections > 0)
            {
                StartCoroutine(ResurrectRoutine());
            }
            else
            {
                Die();
            }
        }
        else
        {
            GrantInvincibility(playerData.hitInvincibilityTime);
        }
    }

    public void Heal(float amount)
    {
        if (CurrentState == PlayerState.Dead) return;
        currentHealth += amount;
        if (currentHealth > playerData.maxHealth) currentHealth = playerData.maxHealth;
        Debug.Log($"[흡혈] 체력 회복! 현재 체력: {currentHealth}");
    }

    private IEnumerator ResurrectRoutine()
    {
        ChangeState(PlayerState.Resurrecting);
        attack.CancelAttack();
        inputVector = Vector2.zero;
        rb.linearVelocity = Vector3.zero;

        animator.Play("Hit");

        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 0.1f;

        //창우_부활 시작 이펙트  쓰러지는 순간 (슬로우모션 진입 직후)
        VFXManager.Instance.PlayPlayerResurrectStart(vfxPoint.position);


        StartCoroutine(CameraZoomRoutine(zoomInFOV, zoomDuration));

        yield return new WaitForSecondsRealtime(4f);

        remainingResurrections--;
        currentHealth = playerData.maxHealth * playerData.resurrectionHealthPercent;

        Time.timeScale = 1f;
        animator.updateMode = AnimatorUpdateMode.Normal;

        StartCoroutine(CameraZoomRoutine(originalFOV, 0.2f));

        //부활 완료 이펙트  일어나는 순간 (타임스케일 복구 직후)
        VFXManager.Instance.PlayPlayerResurrectEnd(vfxPoint.position);

        animator.SetBool("isMoving", false);
        animator.CrossFade("idle", 0.1f);

        ChangeState(PlayerState.Idle);
        GrantInvincibility(playerData.resurrectionInvincibilityTime);
    }

    private IEnumerator CameraZoomRoutine(float targetFOV, float duration)
    {
        if (virtualCamera == null) yield break;

        float startFOV = virtualCamera.Lens.FieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            var lens = virtualCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / duration);
            virtualCamera.Lens = lens;

            yield return null;
        }

        var finalLens = virtualCamera.Lens;
        finalLens.FieldOfView = targetFOV;
        virtualCamera.Lens = finalLens;
    }

    private void Die()
    {
        if (CurrentState == PlayerState.Dead) return;

        //창우_사망 시 이펙트 재생
        VFXManager.Instance.PlayPlayerDeath(vfxPoint.position, gameObject);

        ChangeState(PlayerState.Dead);
        attack.CancelAttack();
        //창우_사망 시 이동과 공격을 즉시 멈추고 입력을 무시하도록 설정, 창현씨가 영상올려주신 한번 부활? 하는 기능을 구현할때는 지워도 될 듯 합니다.


        ChangeState(PlayerState.Dead);
        attack.CancelAttack();
        inputVector = Vector2.zero;
        rb.linearVelocity = Vector3.zero;

        if (TryGetComponent<PlayerInput>(out var input))
        {
            input.enabled = false;
        }

        animator.Play("death");
        GetComponent<Collider>().enabled = false;
    }

    public void OnDash(InputValue value)
    {
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
        if (value.isPressed)
        {
            dash.ExecuteDash();
        }
    }

    public void OnMove(InputValue value)
    {
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
        inputVector = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
        if (value.isPressed && CurrentState != PlayerState.Dash)
        {
            attack.ExecuteAttack();
        }
    }
    public void OnSpecialAttack(InputValue value)
    {
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
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