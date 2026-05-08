using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    private Player player;

    [Header("Combo Settings")]
    private int currentCombo = 0;
    private bool isNextAttackBuffered = false;
    private readonly int maxCombo = 3;
    private Coroutine attackCoroutine;
    private bool isAttackOnCooldown = false;

    [Header("Special (Spin) Settings")]
    private Coroutine specialAttackCoroutine;
    private bool isSpecialAttackOnCooldown = false;
    private bool isSpinning = false;

    private Collider[] hitColliders = new Collider[10];

    private void Awake() => player = GetComponent<Player>();

    // 마우스 좌클릭: 기본 공격
    public void ExecuteAttack()
    {
        if (isAttackOnCooldown || player.CurrentState == PlayerState.Dash || player.CurrentState == PlayerState.Dead) return;

        if (player.CurrentState != PlayerState.Attack)
            attackCoroutine = StartCoroutine(ComboAttackRoutine());
        else if (currentCombo < maxCombo)
            isNextAttackBuffered = true;
    }

    // 마우스 우클릭 누름: 스킬 시작
    public void StartSpecialAttack()
    {
        if (isSpecialAttackOnCooldown || isSpinning || player.CurrentState == PlayerState.Dash || player.CurrentState == PlayerState.Dead) return;

        if (player.CurrentState != PlayerState.Attack && player.CurrentState != PlayerState.SpecialAttack)
            specialAttackCoroutine = StartCoroutine(SpinRoutine());
    }

    // 마우스 우클릭 뗌: 스킬 중지
    public void StopSpecialAttack() => isSpinning = false;

    // 대시 등으로 인한 강제 취소
    public void CancelAttack()
    {
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        if (specialAttackCoroutine != null) StopCoroutine(specialAttackCoroutine);

        isSpinning = false;
        currentCombo = 0;
        isNextAttackBuffered = false;
        isAttackOnCooldown = false;

        if (player.CurrentState == PlayerState.Attack || player.CurrentState == PlayerState.SpecialAttack)
            player.ChangeState(PlayerState.Idle);
    }


    // 기본 공격 콤보 루틴
    private IEnumerator ComboAttackRoutine()
    {
        player.ChangeState(PlayerState.Attack);
        player.animator.SetBool("isMoving", false);
        currentCombo = 1;

        while (currentCombo <= maxCombo)
        {
            isNextAttackBuffered = false;
            LookAtMouse();

            player.animator.CrossFade("attack" + currentCombo, 0.02f);
            yield return new WaitForSeconds(0.05f);

            ExecuteHitDetection(transform.position + transform.forward * (player.playerData.attackRange * 0.5f),
                                player.playerData.attackRange * 0.5f, 1f);

            AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length * 0.5f);

            if (isNextAttackBuffered) currentCombo++;
            else break;
        }

        currentCombo = 0;
        player.animator.CrossFade("idle", 0.15f);
        player.ChangeState(PlayerState.Idle);
        StartCoroutine(AttackCooldownRoutine());
    }

    // 특수 공격 (가렌 E 스타일) 루틴
    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        player.ChangeState(PlayerState.SpecialAttack);
        player.animator.CrossFade("specialAttack", 0.1f);

        float timer = 0f;
        float maxDuration = 5f;
        float tickRate = 0.25f;
        float tickTimer = tickRate;

        while (isSpinning && timer < maxDuration)
        {
            if (player.CurrentState == PlayerState.Dead || player.CurrentState == PlayerState.Resurrecting)
            {
                isSpinning = false;
                break; 
            }

            timer += Time.deltaTime;
            tickTimer += Time.deltaTime;

            if (tickTimer >= tickRate)
            {
                ExecuteHitDetection(transform.position, player.playerData.attackRange, player.playerData.specialAttackMultiplier);
                tickTimer = 0f;
            }
            yield return null;
        }

        isSpinning = false;
        if (player.CurrentState == PlayerState.SpecialAttack)
        {
            player.animator.CrossFade("idle", 0.15f);
            player.ChangeState(PlayerState.Idle);
        }
        StartCoroutine(SpecialCooldownRoutine());
    }

    // 통합 데미지 판정 시스템
    private void ExecuteHitDetection(Vector3 center, float radius, float damageMultiplier)
    {
        center.y += 1f;
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, hitColliders);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitColliders[i];
            if (col.gameObject == player.gameObject) continue;

            var target = col.GetComponentInParent<EnemyBase>();
            if (target != null)
            {
                float finalDamage = player.playerData.damage * damageMultiplier;
                target.TakeDamage(finalDamage);
            }
        }
    }

    private void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (new Plane(Vector3.up, transform.position).Raycast(ray, out float enter))
        {
            Vector3 lookDir = (ray.GetPoint(enter) - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        isAttackOnCooldown = true;
        yield return new WaitForSeconds(player.playerData.attackCooldown);
        isAttackOnCooldown = false;
    }

    private IEnumerator SpecialCooldownRoutine()
    {
        isSpecialAttackOnCooldown = true;
        yield return new WaitForSeconds(player.playerData.specialAttackCooldown);
        isSpecialAttackOnCooldown = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null || player.playerData == null) return;
        Gizmos.color = Color.red;
        Vector3 gizmoPos = transform.position + transform.forward * (player.playerData.attackRange * 0.5f);
        gizmoPos.y += 1f;
        Gizmos.DrawWireSphere(gizmoPos, player.playerData.attackRange * 0.5f);
    }

}